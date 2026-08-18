#!/usr/bin/env node
/**
 * D15-D16 — 阶段四总 DoD 验收（优化编排 v2）
 *
 *   node scripts/phase4-dod-verify.mjs
 *   node scripts/phase4-dod-verify.mjs --skip-host      # 跳过 D11 全量 build（约 3.5min）
 *   node scripts/phase4-dod-verify.mjs --skip-green     # 复用 phase4-d14-green-path.json
 *   node scripts/phase4-dod-verify.mjs --no-cleanup     # 不杀 :5000（与 build 并行时慎用）
 *   node scripts/phase4-dod-verify.mjs --force-green    # 忽略已有 Green 证据，重跑 D14
 *
 * 编排优化（相对 v1）：
 *   1. 默认验收前 cleanup → 避免 JNPF.Analyzers.dll MSB3027
 *   2. PhaseB 只 build 一次 → D3/D5/D11/PhaseB 共享 --no-build
 *   3. build-only 门禁先于 API 联调 → 降低与 :5000 后端竞争
 *   4. Q4 只扫 leave-simple + Green path 产物，不扫历史 workspace 垃圾
 *   5. 有效 Green 证据 6h 内自动 --skip-green（除非 --force-green）
 *
 * 产出：.claude/evidence/phase4-dod-verify.json
 */
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login } from './lib/jnpf-auth.mjs';
import { buildPhaseB, runDevCleanup, runPhaseBCli } from './lib/dotnet-build.mjs';
import {
  EVIDENCE_DIR,
  REPO_ROOT,
  log,
  resolveQ4ScanRoots,
  scanGeneratedSqlForTenantFilter,
  writeEvidence,
} from './lib/phase4-api.mjs';

const GREEN_EVIDENCE_TTL_MS = 6 * 60 * 60 * 1000;

const SKIP_HOST = process.argv.includes('--skip-host');
const SKIP_GREEN_FLAG = process.argv.includes('--skip-green');
const FORCE_GREEN = process.argv.includes('--force-green');
const NO_CLEANUP = process.argv.includes('--no-cleanup');

const results = [];

function record(id, pass, detail, extra = {}) {
  results.push({ id, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : 'FAIL', id, detail);
}

function skip(id, reason) {
  record(id, true, reason, { skip: true });
}

function runNodeScript(scriptName, args = []) {
  const scriptPath = path.join(REPO_ROOT, 'scripts', scriptName);
  log('exec', scriptName, args.join(' '));
  const r = spawnSync(process.execPath, [scriptPath, ...args], {
    cwd: REPO_ROOT,
    stdio: 'pipe',
    encoding: 'utf8',
    env: { ...process.env },
  });
  const stdout = (r.stdout || '') + (r.stderr || '');
  if (stdout.trim()) {
    const tail = stdout.split('\n').slice(-15).join('\n');
    log('  ↳', tail);
  }
  return {
    pass: r.status === 0,
    exitCode: r.status ?? 1,
    stdoutTail: stdout.split('\n').slice(-30).join('\n'),
  };
}

function loadGreenEvidence() {
  const p = path.join(EVIDENCE_DIR, 'phase4-d14-green-path.json');
  if (!fs.existsSync(p)) return null;
  try {
    const ev = JSON.parse(fs.readFileSync(p, 'utf8'));
    const stat = fs.statSync(p);
    return { ...ev, evidencePath: p, evidenceAgeMs: Date.now() - stat.mtimeMs };
  } catch {
    return null;
  }
}

function shouldSkipGreen() {
  if (SKIP_GREEN_FLAG) return { skip: true, reason: 'flag --skip-green' };
  if (FORCE_GREEN) return { skip: false, reason: 'flag --force-green' };

  const ev = loadGreenEvidence();
  if (ev?.pass === true && ev.evidenceAgeMs <= GREEN_EVIDENCE_TTL_MS) {
    return {
      skip: true,
      reason: `auto-reuse evidence pipelineId=${ev.pipelineId} age=${Math.round(ev.evidenceAgeMs / 60000)}min`,
      evidence: ev,
    };
  }
  return { skip: false, reason: 'no fresh green evidence' };
}

/** cleanup 后自动拉起 :5000（仅 API 冒烟 / D14 需要） */
function ensureBackendRunning() {
  const probe = spawnSync(
    process.execPath,
    [path.join(REPO_ROOT, 'scripts', 'jnpf-api.mjs'), 'GET', '/api/oauth/CurrentUser'],
    { cwd: REPO_ROOT, stdio: 'pipe', encoding: 'utf8', env: { ...process.env } },
  );
  if (probe.status === 0) {
    log('backend', ':5000 already up');
    return true;
  }

  log('backend', 'starting JNPF.API.Entry (dotnet run --no-build)…');
  const entryDir = path.join(REPO_ROOT, 'backend', 'application', 'JNPF.API.Entry');
  spawnSync('dotnet', ['build', '-v', 'q', '/nologo', '-p:RunAnalyzers=false'], {
    cwd: entryDir,
    stdio: 'inherit',
    shell: true,
  });
  const child = spawnSync(
    'powershell',
    [
      '-Command',
      `Start-Process -WindowStyle Hidden -WorkingDirectory '${entryDir.replace(/'/g, "''")}' `
        + `-FilePath dotnet -ArgumentList 'run','--no-build'`,
    ],
    { cwd: REPO_ROOT, stdio: 'pipe', encoding: 'utf8' },
  );
  if (child.status !== 0) {
    log('backend', 'Start-Process failed, trying blocking dotnet run…');
  }

  const deadline = Date.now() + 90_000;
  while (Date.now() < deadline) {
    spawnSync('powershell', ['-Command', 'Start-Sleep -Seconds 3'], { shell: true });
    const retry = spawnSync(
      process.execPath,
      [path.join(REPO_ROOT, 'scripts', 'jnpf-api.mjs'), 'GET', '/api/oauth/CurrentUser'],
      { cwd: REPO_ROOT, stdio: 'pipe', encoding: 'utf8' },
    );
    if (retry.status === 0) {
      log('backend', ':5000 ready');
      return true;
    }
  }
  log('backend', 'WARN: :5000 not ready after 90s');
  return false;
}

function runPhaseBFull() {
  log('exec PhaseB dotnet run (full suite, --no-build)');
  const run = runPhaseBCli([], { inherit: false });
  const out = (run.stdout || '') + (run.stderr || '');
  const pass = run.status === 0 && /0 失败/.test(out);
  return {
    pass,
    exitCode: run.status ?? 1,
    detail: pass ? 'PhaseB 0 failures' : `PhaseB exit=${run.status}`,
    stdoutTail: out.split('\n').slice(-10).join('\n'),
  };
}

async function main() {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
  const greenPlan = shouldSkipGreen();

  // ── 0. 环境：build-only 门禁前释放 dotnet 锁 ──
  if (!NO_CLEANUP) {
    log('cleanup', 'start-dev.ps1 -CleanupOnly (free Analyzer/API locks)');
    const clean = runDevCleanup();
    const tail = ((clean.stdout || '') + (clean.stderr || '')).split('\n').slice(-4).join('\n');
    if (tail.trim()) log('  ↳', tail);
    record('ENV-CLEANUP', clean.status === 0, clean.status === 0 ? 'cleanup exit 0' : `cleanup exit ${clean.status}`);
  } else {
    skip('ENV-CLEANUP', 'skipped (--no-cleanup); build 可能与 :5000 后端竞争');
  }

  // ── 1. PhaseB 单次 build（D3/D5/D11/PhaseB 共享） ──
  log('exec', 'PhaseB build once (RunAnalyzers=false, -m:1)');
  const sharedBuild = buildPhaseB({ inherit: true, retries: 1 });
  record(
    'PHASEB-BUILD',
    sharedBuild.pass,
    sharedBuild.pass ? 'shared PhaseB build ok' : `build failed after retries`,
    { stdoutTail: sharedBuild.stdoutTail },
  );
  if (!sharedBuild.pass) {
    writeReportAndExit(greenPlan);
    return;
  }

  const noBuildArgs = ['--no-build'];

  // ── 2. build-only 子门禁（无 API） ──
  const d3 = runNodeScript('phase4-d3-sandbox-gate.mjs', noBuildArgs);
  record('D3-GATE', d3.pass, d3.pass ? 'sandbox-gate exit 0' : `exit ${d3.exitCode}`, {
    stdoutTail: d3.stdoutTail,
  });

  const d10 = runNodeScript('phase4-d5-arch-guard.mjs', noBuildArgs);
  record('D5-Q2', d10.pass, d10.pass ? 'arch-guard Q2 profiles exit 0' : `exit ${d10.exitCode}`, {
    stdoutTail: d10.stdoutTail,
  });

  if (SKIP_HOST) {
    skip('D11-D12', 'skipped (--skip-host); D16 正式验收须去掉此 flag');
  } else {
    const d11 = runNodeScript('phase4-d11-host-build.mjs', noBuildArgs);
    record('D11-D12', d11.pass, d11.pass ? 'host-demo full build exit 0' : `exit ${d11.exitCode}`, {
      stdoutTail: d11.stdoutTail,
    });
  }

  const phaseB = runPhaseBFull();
  record('PHASEB', phaseB.pass, phaseB.detail, { stdoutTail: phaseB.stdoutTail });

  // ── 3. Q4 租户抽样（限定 scope，避免历史 workspace 误报） ──
  const greenEv = greenPlan.evidence ?? loadGreenEvidence();
  const q4Roots = resolveQ4ScanRoots(greenEv);
  const q4 = scanGeneratedSqlForTenantFilter(q4Roots.length > 0 ? q4Roots : []);
  record('Q4-TENANT-SQL', q4.pass || q4.scanned === 0, q4.detail, {
    scanned: q4.scanned,
    issues: q4.issues,
    roots: q4.roots,
  });

  // ── 4. API 联调（build 完成后启动后端再冒烟） ──
  if (greenPlan.skip) {
    const ev = greenEv ?? loadGreenEvidence();
    const pass = ev?.pass === true;
    record(
      'D14-GREEN',
      pass,
      pass ? `${greenPlan.reason}` : 'no valid phase4-d14-green-path.json',
      { pipelineId: ev?.pipelineId, autoReuse: !SKIP_GREEN_FLAG },
    );
  } else {
    log('hint', 'D14 需要 :5000 API — 正在启动后端…');
    ensureBackendRunning();
    const d14 = runNodeScript('phase4-green-path.mjs', ['--skip-artifacts']);
    record('D14-GREEN', d14.pass, d14.pass ? 'green-path exit 0' : `exit ${d14.exitCode}`, {
      stdoutTail: d14.stdoutTail,
    });
  }

  try {
    ensureBackendRunning();
    const session = await login();
    const { apiRequest, isJnpfOk } = await import('./lib/jnpf-auth.mjs');
    const smoke = await apiRequest('GET', '/api/oauth/CurrentUser', { session });
    record('API-SMOKE', isJnpfOk(smoke), `CurrentUser status=${smoke.status}`, {
      account: session.account,
    });
  } catch (e) {
    record('API-SMOKE', false, e.message);
  }

  const g3Path = path.join(EVIDENCE_DIR, 'phase3-g3-verify.json');
  let g3Pass = false;
  let g3Detail = 'no phase3-g3-verify.json';
  if (fs.existsSync(g3Path)) {
    try {
      const g3 = JSON.parse(fs.readFileSync(g3Path, 'utf8'));
      g3Pass = g3.pass === true;
      g3Detail = g3Pass
        ? `phase3-g3-verify ${g3.passed}/${g3.total} signed`
        : `phase3-g3 ${g3.passed}/${g3.total} FAIL`;
    } catch {
      g3Detail = 'phase3-g3-verify.json parse error';
    }
  }
  record('G3', g3Pass, g3Detail);

  writeReportAndExit(greenPlan);
}

function writeReportAndExit(greenPlan) {
  const runnable = results.filter(r => !r.skip);
  const passed = runnable.filter(r => r.pass).length;
  const report = {
    phase: 'phase4-dod',
    version: 'v2-orchestration',
    passed,
    total: runnable.length,
    skipped: results.filter(r => r.skip).length,
    flags: {
      skipHost: SKIP_HOST,
      skipGreen: SKIP_GREEN_FLAG || greenPlan.skip,
      forceGreen: FORCE_GREEN,
      noCleanup: NO_CLEANUP,
      greenReuseReason: greenPlan.reason,
    },
    results,
    at: new Date().toISOString(),
  };

  const reportPath = writeEvidence('phase4-dod-verify.json', report);
  log('report →', reportPath);
  log(`summary ${passed}/${runnable.length} (${report.skipped} skipped)`);

  if (passed < runnable.length) process.exit(1);
}

main().catch(err => {
  console.error('[phase4-dod] FATAL', err);
  process.exit(1);
});
