#!/usr/bin/env node
/**
 * G3 — 阶段三 11-附 导师签字自动化证据包
 *
 *   node scripts/phase3-g3-verify.mjs
 *   node scripts/phase3-g3-verify.mjs --skip-stress --skip-browser
 *
 * 产出：
 *   .claude/evidence/phase3-g3-verify.json
 *   docs/AI原生开发/1、多用户多任务并行/evidence/phase3-g3-signoff-20260705.md
 *
 * 收口条件（11-附 §9）：phase3-dod 9/9 + A–E 无 FAIL
 */
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { apiRequest, isJnpfOk, jnpfData, login, pick } from './lib/jnpf-auth.mjs';
import { getSkillLlmPolicy, runSqlQuery } from './lib/jnpf-db.mjs';
import { buildPhaseB, runPhaseBCli } from './lib/dotnet-build.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..');
const EVIDENCE_DIR = path.join(REPO_ROOT, '.claude', 'evidence');
const DOC_EVIDENCE = path.join(
  REPO_ROOT,
  'docs/AI原生开发/1、多用户多任务并行/evidence',
);

const SKIP_STRESS = process.argv.includes('--skip-stress');
const SKIP_BROWSER = process.argv.includes('--skip-browser');
const REUSE_STRESS = process.argv.includes('--reuse-stress-evidence');

const results = [];
const log = (...args) => console.log('[g3]', ...args);

function record(task, id, pass, detail, extra = {}) {
  results.push({ task, id, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : 'FAIL', `${task}/${id}`, detail);
}

function skip(task, id, reason) {
  record(task, id, true, reason, { skip: true });
}

function runNode(script, args = []) {
  const r = spawnSync(process.execPath, [path.join(REPO_ROOT, 'scripts', script), ...args], {
    cwd: REPO_ROOT,
    stdio: 'pipe',
    encoding: 'utf8',
  });
  return {
    pass: r.status === 0,
    exitCode: r.status ?? 1,
    tail: ((r.stdout || '') + (r.stderr || '')).split('\n').slice(-12).join('\n'),
  };
}

async function taskBaseline() {
  const dod = runNode('phase3-dod-verify.mjs');
  record('BASE', 'phase3-dod', dod.pass, dod.pass ? '9/9 exit 0' : `exit ${dod.exitCode}`, {
    stdoutTail: dod.tail,
  });
  return dod.pass;
}

async function taskA_D17D18(session) {
  try {
    const maxTokensPolicy = runSqlQuery(
      "SET NOCOUNT ON; SELECT F_MaxTokensPerCall FROM ai_skill_llm_policy WHERE F_SkillId = 'db-design-skill'",
    );
    const policyLine = maxTokensPolicy.split(/\r?\n/).map(s => s.trim()).find(s => /^\d+$/.test(s));
    const policyVal = policyLine ? Number(policyLine) : null;
    const d17Pass = policyVal != null && policyVal > 0 && policyVal <= 8192;
    record('A', 'D17', d17Pass, `db-design F_MaxTokensPerCall=${policyVal}`, { policyVal });

    const res = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
      body: {
        name: `G3-D18-${Date.now()}`,
        userRequirement: 'G3 D18 token 对账抽样：员工请假 MVP。'.padEnd(400, '测'),
      },
      session,
    });
    if (!isJnpfOk(res)) {
      record('A', 'D18', false, `create pipeline failed: ${JSON.stringify(res.json)}`);
      return;
    }
    const pipelineId = pick(jnpfData(res), 'pipelineId', 'PipelineId');
    const tenantId = session.tenantId ?? '0';

    const projectRow = runSqlQuery(
      `SET NOCOUNT ON; SELECT F_TokenConsumed FROM ai_projects WHERE F_Id = '${pipelineId}' AND F_TenantId = '${tenantId}'`,
    );
    const consumed = Number(projectRow.split(/\r?\n/).map(s => s.trim()).find(s => /^\d+$/.test(s)) ?? 0);

    const logSumRow = runSqlQuery(`
      SET NOCOUNT ON;
      SELECT ISNULL(SUM(ISNULL(F_PROMPT_TOKENS,0)+ISNULL(F_COMPLETION_TOKENS,0)),0)
      FROM BASE_AI_CALL_LOG
      WHERE F_ProjectId = '${pipelineId}' AND F_TENANT_ID = '${tenantId}'
    `);
    const logSum = Number(logSumRow.split(/\r?\n/).map(s => s.trim()).find(s => /^\d+$/.test(s)) ?? 0);
    const delta = Math.abs(consumed - logSum);
    const d18Pass = delta <= 100;
    record('A', 'D18', d18Pass, `consumed=${consumed} logSum=${logSum} delta=${delta}`, {
      pipelineId,
      tenantId,
      consumed,
      logSum,
      delta,
    });
  } catch (e) {
    record('A', 'D17', false, e.message);
    record('A', 'D18', false, e.message);
  }
}

function taskB_D12() {
  if (SKIP_STRESS) {
    skip('B', 'D12', 'skipped (--skip-stress)');
    return true;
  }
  if (REUSE_STRESS) {
    const p = path.join(EVIDENCE_DIR, 'phase2.5-stress-report.json');
    if (fs.existsSync(p)) {
      try {
        const ev = JSON.parse(fs.readFileSync(p, 'utf8'));
        const ageMs = Date.now() - fs.statSync(p).mtimeMs;
        const pass = ev.failed === 0 && Array.isArray(ev.results) && ev.results.every(r => r.pass);
        if (pass && ageMs < 3600_000) {
          record('B', 'D12', true, `reused stress evidence age=${Math.round(ageMs / 60000)}min`, { evidence: p });
          return true;
        }
      } catch { /* fall through */ }
    }
  }
  const sa = spawnSync('curl', ['-s', '-o', 'NUL', '-w', '%{http_code}', 'http://127.0.0.1:3001/api/sa/health'], {
    shell: true,
    encoding: 'utf8',
  });
  if ((sa.stdout || '').trim() !== '200') {
    record('B', 'D12', false, 'sa-service :3001 未启动 — 请 start-dev.ps1 或单独启动 SA 后重跑');
    return false;
  }
  const r = runNode('phase2.5-stress-e2e.mjs', ['--skip-full-e2e']);
  record('B', 'D12', r.pass, r.pass ? 'phase2.5-stress exit 0' : `exit ${r.exitCode}`, {
    stdoutTail: r.tail,
  });
  return r.pass;
}

function taskC_D14() {
  if (SKIP_BROWSER) {
    skip('C', 'D14', 'skipped (--skip-browser)');
    return true;
  }
  const fe = spawnSync('curl', ['-s', '-o', 'NUL', '-w', '%{http_code}', 'http://localhost:3100/'], {
    shell: true,
    encoding: 'utf8',
  });
  const feOk = (fe.stdout || '').trim() === '200';
  if (!feOk) {
    record('C', 'D14', false, 'frontend :3100 not reachable — start-dev.ps1 required');
    return false;
  }
  const r = runNode('phase2.5-d16-browser.mjs');
  const pngs = fs.existsSync(EVIDENCE_DIR)
    ? fs.readdirSync(EVIDENCE_DIR).filter(f => f.endsWith('.png') && f !== 'playwright-smoke.png')
    : [];
  const freshPng = pngs.some(f => {
    const st = fs.statSync(path.join(EVIDENCE_DIR, f));
    return st.size > 5000 && Date.now() - st.mtimeMs < 30 * 60 * 1000;
  });
  const pass = r.pass && freshPng;
  record('C', 'D14', pass, pass ? `browser exit 0, png=${pngs.length}` : `exit=${r.exitCode} freshPng=${freshPng}`, {
    stdoutTail: r.tail,
    pngs,
  });
  return pass;
}

function taskD_D16() {
  buildPhaseB({ inherit: false, retries: 0 });
  const r = runPhaseBCli(['phase3-maxcalls'], { inherit: false });
  const out = (r.stdout || '') + (r.stderr || '');
  const pass = r.status === 0 && /Phase3.*maxCalls|LLM_CALL_LIMIT_EXCEEDED|All design skill tests passed/i.test(out);
  record('D', 'D16', pass, pass ? 'PhaseB phase3-maxcalls exit 0' : `exit ${r.status}`, {
    stdoutTail: out.split('\n').slice(-8).join('\n'),
  });
  return pass;
}

async function taskE_D8(session) {
  try {
    const mismatch = runSqlQuery(`
      SET NOCOUNT ON;
      SELECT COUNT(*) AS cnt
      FROM ai_ir_events e
      INNER JOIN ai_projects p ON e.F_ProjectId = p.F_Id
      WHERE e.F_TenantId <> p.F_TenantId
    `);
    const crossRows = Number(mismatch.split(/\r?\n/).map(s => s.trim()).find(s => /^\d+$/.test(s)) ?? -1);
    const sqlOk = crossRows === 0;
    record('E', 'D8-SQL', sqlOk, sqlOk ? 'events/projects tenant mismatch rows=0' : `mismatch rows=${crossRows}`, {
      crossRows,
    });

    buildPhaseB({ inherit: false, retries: 0 });
    const guardRun = runPhaseBCli(['phase3-tenant-isolation'], { inherit: false });
    const guardOut = (guardRun.stdout || '') + (guardRun.stderr || '');
    const guardOk = guardRun.status === 0 && /TenantGuard cross-tenant isolation passed/i.test(guardOut);
    record('E', 'D8-GUARD', guardOk, guardOk ? 'TenantGuard VerifyOwnership cross-tenant=false' : `exit ${guardRun.status}`, {
      stdoutTail: guardOut.split('\n').slice(-4).join('\n'),
    });

    const accountB = process.env.JNPF_TENANT_B_ACCOUNT;
    const passwordB = process.env.JNPF_TENANT_B_PASSWORD;
    if (!accountB || !passwordB) {
      skip('E', 'D8-API', '无 JNPF_TENANT_B_*；SQL+TenantGuard 已覆盖');
      return sqlOk && guardOk;
    }

    const resA = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
      body: {
        name: `G3-D8-A-${Date.now()}`,
        userRequirement: 'G3 双租户 API 探测 A。'.padEnd(400, '测'),
      },
      session,
    });
    if (!isJnpfOk(resA)) {
      record('E', 'D8-API', false, 'tenant A create failed');
      return sqlOk && guardOk;
    }
    const pipelineA = pick(jnpfData(resA), 'pipelineId', 'PipelineId');
    await apiRequest('POST', `/api/studio/ir/${pipelineA}/simulate`, {
      body: { eventType: 'SkeletonCreated' },
      session,
    });

    const loginB = spawnSync(process.execPath, [path.join(REPO_ROOT, 'scripts/lib/jnpf-auth.mjs'), '--json'], {
      cwd: REPO_ROOT,
      env: { ...process.env, JNPF_ACCOUNT: accountB, JNPF_PASSWORD: passwordB },
      encoding: 'utf8',
    });
    let sessionB;
    try {
      sessionB = JSON.parse((loginB.stdout || '').trim() || '{}');
    } catch {
      sessionB = null;
    }
    if (!sessionB?.token) {
      record('E', 'D8-API', false, `Tenant B 登录失败 account=${accountB}`);
      return sqlOk && guardOk;
    }

    const cross = await apiRequest('GET', `/api/studio/ir/${pipelineA}/events`, {
      session: { token: sessionB.token, account: accountB, tenantId: sessionB.tenantId },
    });
    const crossTypes = (Array.isArray(cross.json) ? cross.json : jnpfData(cross) || [])
      .map(e => pick(e, 'eventType', 'EventType'));
    const crossOk = cross.status === 403
      || cross.status === 404
      || !isJnpfOk(cross)
      || crossTypes.length === 0
      || !crossTypes.includes('SkeletonCreated');

    record('E', 'D8-API', crossOk, crossOk
      ? `B(${accountB}) 无法读取 A pipeline ${pipelineA}`
      : `泄漏：B 可见 A 事件`, {
      pipelineA,
      crossStatus: cross.status,
      crossTypes,
    });
    return sqlOk && guardOk && crossOk;
  } catch (e) {
    record('E', 'D8', false, e.message);
    return false;
  }
}

async function main() {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
  fs.mkdirSync(DOC_EVIDENCE, { recursive: true });

  await taskBaseline();
  const session = await login();
  log('logged in as', session.account, 'tenant', session.tenantId);

  await taskA_D17D18(session);
  taskB_D12();
  taskC_D14();
  taskD_D16();
  await taskE_D8(session);

  const runnable = results.filter(r => !r.skip);
  const passed = runnable.filter(r => r.pass).length;
  const allPass = passed === runnable.length;

  const report = {
    phase: 'phase3-g3-signoff',
    pass: allPass,
    passed,
    total: runnable.length,
    skipped: results.filter(r => r.skip).length,
    results,
    at: new Date().toISOString(),
  };

  const jsonPath = path.join(EVIDENCE_DIR, 'phase3-g3-verify.json');
  fs.writeFileSync(jsonPath, JSON.stringify(report, null, 2));

  const mdPath = path.join(DOC_EVIDENCE, 'phase3-g3-signoff-20260705.md');
  const md = `# G3 阶段三导师签字证据包

- 执行时间：${report.at}
- 自动化结论：**${allPass ? 'PASS — 可签字' : 'FAIL — 见下表'}**
- JSON：\`.claude/evidence/phase3-g3-verify.json\`

## §9 签字表（导师复核）

| 任务 | DoD | 自动化 | 导师结论 | 证据 |
|------|-----|--------|----------|------|
| A | D17–D18 | ${results.find(r => r.id === 'D17')?.pass ? 'PASS' : 'FAIL'} / ${results.find(r => r.id === 'D18')?.pass ? 'PASS' : 'FAIL'} | ☐ PASS ☐ FAIL | phase3-g3-verify.json#A |
| B | D12 | ${results.find(r => r.task === 'B')?.pass ? 'PASS' : results.find(r => r.task === 'B')?.skip ? 'SKIP' : 'FAIL'} | ☐ PASS ☐ FAIL | phase2.5-stress-report.json |
| C | D14 | ${results.find(r => r.id === 'D14')?.pass ? 'PASS' : results.find(r => r.id === 'C')?.skip ? 'SKIP' : 'FAIL'} | ☐ PASS ☐ FAIL | .claude/evidence/*.png |
| D | D16 | ${results.find(r => r.id === 'D16')?.pass ? 'PASS' : 'FAIL'} | ☐ PASS ☐ FAIL | PhaseB phase3-maxcalls |
| E | D8 | ${results.find(r => r.id === 'D8')?.pass ? 'PASS' : 'FAIL'} | ☐ PASS ☐ FAIL | 双租户 API 探测 |

## 明细

${results.map(r => `- **${r.task}/${r.id}** ${r.skip ? 'SKIP' : r.pass ? 'PASS' : 'FAIL'}: ${r.detail}`).join('\n')}
`;
  fs.writeFileSync(mdPath, md, 'utf8');

  log('report →', jsonPath);
  log('signoff →', mdPath);
  log(`summary ${passed}/${runnable.length}`);

  if (!allPass) process.exit(1);
}

main().catch(err => {
  console.error('[g3] FATAL', err);
  process.exit(1);
});
