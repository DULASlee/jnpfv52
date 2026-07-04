#!/usr/bin/env node
/**
 * D15-D16 — 阶段四总 DoD 验收
 *
 *   node scripts/phase4-dod-verify.mjs
 *   node scripts/phase4-dod-verify.mjs --skip-host      # 跳过 D11 全量 build（约 3.5min）
 *   node scripts/phase4-dod-verify.mjs --skip-green     # 跳过 D14 HTTP 联调（需已有 evidence）
 *
 * 产出：.claude/evidence/phase4-dod-verify.json
 *
 * D16 收口：本脚本 exit 0 + D14 Green path + （默认）宿主全量 build pass
 */
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login } from './lib/jnpf-auth.mjs';
import {
  EVIDENCE_DIR,
  REPO_ROOT,
  log,
  scanGeneratedSqlForTenantFilter,
  writeEvidence,
} from './lib/phase4-api.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SKIP_HOST = process.argv.includes('--skip-host');
const SKIP_GREEN = process.argv.includes('--skip-green');

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

function runPhaseB() {
  const phaseBDir = path.join(REPO_ROOT, 'backend', 'tests', 'JNPF.Tests.PhaseB');
  log('exec PhaseB dotnet run');
  const build = spawnSync('dotnet', ['build', '-v', 'q'], {
    cwd: phaseBDir,
    stdio: 'inherit',
    shell: true,
  });
  if (build.status !== 0) {
    return { pass: false, exitCode: build.status ?? 1, detail: 'PhaseB build failed' };
  }
  const run = spawnSync('dotnet', ['run', '--no-build'], {
    cwd: phaseBDir,
    stdio: 'pipe',
    encoding: 'utf8',
    shell: true,
  });
  const out = (run.stdout || '') + (run.stderr || '');
  const pass = run.status === 0 && /0 失败/.test(out);
  return {
    pass,
    exitCode: run.status ?? 1,
    detail: pass ? 'PhaseB 0 failures' : `PhaseB exit=${run.status}`,
    stdoutTail: out.split('\n').slice(-10).join('\n'),
  };
}

function loadGreenEvidence() {
  const p = path.join(EVIDENCE_DIR, 'phase4-d14-green-path.json');
  if (!fs.existsSync(p)) return null;
  try {
    return JSON.parse(fs.readFileSync(p, 'utf8'));
  } catch {
    return null;
  }
}

async function main() {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });

  // ── 子脚本门禁 ──
  const d3 = runNodeScript('phase4-d3-sandbox-gate.mjs');
  record('D3-GATE', d3.pass, d3.pass ? 'sandbox-gate exit 0' : `exit ${d3.exitCode}`, {
    stdoutTail: d3.stdoutTail,
  });

  const d10 = runNodeScript('phase4-d5-arch-guard.mjs');
  record('D5-Q2', d10.pass, d10.pass ? 'arch-guard Q2 profiles exit 0' : `exit ${d10.exitCode}`, {
    stdoutTail: d10.stdoutTail,
  });

  if (SKIP_HOST) {
    skip('D11-D12', 'skipped (--skip-host); D16 正式验收须去掉此 flag');
  } else {
    const d11 = runNodeScript('phase4-d11-host-build.mjs');
    record('D11-D12', d11.pass, d11.pass ? 'host-demo full build exit 0' : `exit ${d11.exitCode}`, {
      stdoutTail: d11.stdoutTail,
    });
  }

  // ── D14 Green path ──
  if (SKIP_GREEN) {
    const ev = loadGreenEvidence();
    const pass = ev?.pass === true;
    record(
      'D14-GREEN',
      pass,
      pass ? `reused evidence pipelineId=${ev.pipelineId}` : 'no valid phase4-d14-green-path.json',
      { pipelineId: ev?.pipelineId },
    );
  } else {
    const d14 = runNodeScript('phase4-green-path.mjs', ['--skip-artifacts']);
    record('D14-GREEN', d14.pass, d14.pass ? 'green-path exit 0' : `exit ${d14.exitCode}`, {
      stdoutTail: d14.stdoutTail,
    });
  }

  // ── PhaseB 全量回归 ──
  const phaseB = runPhaseB();
  record('PHASEB', phaseB.pass, phaseB.detail, { stdoutTail: phaseB.stdoutTail });

  // ── Q4 租户 SQL 抽样 ──
  const q4 = scanGeneratedSqlForTenantFilter();
  record('Q4-TENANT-SQL', q4.pass, q4.detail, { scanned: q4.scanned, issues: q4.issues });

  // ── API 冒烟 ──
  try {
    const session = await login();
    const { apiRequest, isJnpfOk } = await import('./lib/jnpf-auth.mjs');
    const smoke = await apiRequest('GET', '/api/oauth/CurrentUser', { session });
    record('API-SMOKE', isJnpfOk(smoke), `CurrentUser status=${smoke.status}`, {
      account: session.account,
    });
  } catch (e) {
    record('API-SMOKE', false, e.message);
  }

  // ── G3 提醒（不阻塞脚本 exit，由导师签字） ──
  skip('G3', '阶段三 11-附 导师签字 — 手工门禁，阻塞 D16 正式归档');

  const runnable = results.filter(r => !r.skip);
  const passed = runnable.filter(r => r.pass).length;
  const report = {
    phase: 'phase4-dod',
    passed,
    total: runnable.length,
    skipped: results.filter(r => r.skip).length,
    flags: { skipHost: SKIP_HOST, skipGreen: SKIP_GREEN },
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
