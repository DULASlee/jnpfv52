#!/usr/bin/env node
/**
 * 阶段三 DoD 缺口验收（API 可自动化子集）
 *
 *   node scripts/phase3-dod-verify.mjs
 *
 * 产出：.claude/evidence/phase3-dod-verify.json
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { apiRequest, isJnpfOk, jnpfData, login, pick } from './lib/jnpf-auth.mjs';
import {
  getSkillLlmPolicy,
  resetProjectTokenConsumed,
  setProjectTokenConsumed,
} from './lib/jnpf-db.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..');
const EVIDENCE_DIR = path.join(REPO_ROOT, '.claude', 'evidence');
const REPORT_PATH = path.join(EVIDENCE_DIR, 'phase3-dod-verify.json');

const results = [];
const log = (...args) => console.log('[phase3-dod]', ...args);

function record(id, pass, detail, extra = {}) {
  results.push({ id, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : extra.skip ? 'SKIP' : 'FAIL', id, detail);
}

function skip(id, reason) {
  record(id, true, reason, { skip: true });
}

function jnpfCode(result) {
  if (result?.json && typeof result.json === 'object' && 'code' in result.json)
    return result.json.code;
  return result?.status;
}

async function waitFor(fn, label, timeoutMs = 90_000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const hit = await fn();
    if (hit) return hit;
    await new Promise(r => setTimeout(r, 800));
  }
  throw new Error(`timeout: ${label}`);
}

async function createPipeline(session, name) {
  const res = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
    body: {
      name,
      userRequirement: `${name}：员工请假审批，Phase3 DoD 验收路径。`.padEnd(400, '测'),
    },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`create: ${JSON.stringify(res.json)}`);
  return pick(jnpfData(res), 'pipelineId', 'PipelineId');
}

async function simulate(session, pipelineId, body) {
  const res = await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, { body, session });
  if (!isJnpfOk(res)) throw new Error(`simulate ${body.eventType}: ${JSON.stringify(res.json)}`);
  return res;
}

async function setupIr1Stable(session, pipelineId) {
  await simulate(session, pipelineId, { eventType: 'SkeletonCreated' });
  await simulate(session, pipelineId, {
    eventType: 'EventSpecConfirmed',
    fragmentId: 'eventspec:BE-001',
  });
}

async function setupIr2Clean(session, pipelineId) {
  await simulate(session, pipelineId, { eventType: 'ArchitectureDecisionRecorded' });
  await simulate(session, pipelineId, { eventType: 'DDLStabilized' });
  await simulate(session, pipelineId, { eventType: 'UIDesignStabilized' });
}

async function getEvents(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
  return Array.isArray(res.json) ? res.json : jnpfData(res) || [];
}

async function getSnapshots(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/snapshots`, { session });
  const data = jnpfData(res);
  if (Array.isArray(data)) return data;
  if (Array.isArray(res.json)) return res.json;
  return [];
}

async function waitSkillTerminal(session, pipelineId, skillId, timeoutMs = 60_000) {
  return waitFor(async () => {
    const res = await apiRequest('GET', `/api/studio/skills/${pipelineId}/runs`, { session });
    const list = Array.isArray(res.json) ? res.json : jnpfData(res) || [];
    const run = list.find(r => pick(r, 'skillId', 'SkillId') === skillId);
    const st = pick(run, 'status', 'Status');
    if (st === 'completed' || st === 'failed' || st === 'cancelled') {
      return {
        status: st,
        error: pick(run, 'errorMessage', 'ErrorMessage') || '',
      };
    }
    return null;
  }, `skill ${skillId}`, timeoutMs);
}

/** D10：IR-1 未 stable 时 design/run → 400 */
async function testD10_Ir1Reject(session) {
  const pipelineId = await createPipeline(session, `P3-D10-${Date.now()}`);
  await simulate(session, pipelineId, { eventType: 'SkeletonCreated' });

  const res = await apiRequest('POST', `/api/studio/skills/design/${pipelineId}/run`, {
    body: {},
    session,
  });
  const pass = !isJnpfOk(res) && (res.status === 400 || jnpfCode(res) === 400);
  record('D10', pass, `design/run without IR-1 → status=${res.status} code=${jnpfCode(res)}`, { pipelineId });
  return pass;
}

/** D1：IR-1 stable 后 design/run 可启动 */
async function testD1_DesignRunStart(session) {
  const pipelineId = await createPipeline(session, `P3-D1-${Date.now()}`);
  await setupIr1Stable(session, pipelineId);

  const run = await apiRequest('POST', `/api/studio/skills/design/${pipelineId}/run`, {
    body: {},
    session,
  });
  const statusRes = await apiRequest('GET', `/api/studio/skills/design/${pipelineId}/status`, { session });
  const status = jnpfData(statusRes) || statusRes.json?.data || statusRes.json;
  const ir1Stable = pick(status, 'ir1Stable', 'Ir1Stable') === true;
  const pass = isJnpfOk(run) && ir1Stable;
  record('D1', pass, `design/run ok=${isJnpfOk(run)}, ir1Stable=${ir1Stable}`, { pipelineId });
  return pass;
}

/** D6：三 IR-2 片段 stable 后 system-design → SystemDesignLocked */
async function testD6_SystemDesignLocked(session) {
  const pipelineId = await createPipeline(session, `P3-D6-${Date.now()}`);
  await setupIr1Stable(session, pipelineId);
  await setupIr2Clean(session, pipelineId);

  await apiRequest('POST', `/api/studio/skills/system-design/${pipelineId}/run`, { body: {}, session });
  const terminal = await waitSkillTerminal(session, pipelineId, 'system-design-skill');

  const types = (await getEvents(session, pipelineId)).map(e => pick(e, 'eventType', 'EventType'));
  const locked = types.includes('SystemDesignLocked');
  const pass = terminal.status === 'completed' && locked;
  record('D6', pass, `run=${terminal.status}, SystemDesignLocked=${locked}`, { pipelineId });
  return pass;
}

/** D7：Dev 注入分层违规 DDL → ConstraintViolationReported + critical */
async function testD7_ConstraintViolation(session) {
  const pipelineId = await createPipeline(session, `P3-D7-${Date.now()}`);
  await setupIr1Stable(session, pipelineId);
  await simulate(session, pipelineId, { eventType: 'ArchitectureDecisionRecorded' });
  await simulate(session, pipelineId, {
    eventType: 'DDLStabilized',
    injectLayerViolation: true,
  });
  await simulate(session, pipelineId, { eventType: 'UIDesignStabilized' });

  const check = await apiRequest('POST', `/api/studio/ir/${pipelineId}/constraints/check`, {
    body: { persist: true },
    session,
  });
  const data = jnpfData(check) || check.json?.data || check.json;
  const critical = pick(data, 'criticalCount', 'CriticalCount') ?? 0;
  const passed = pick(data, 'passed', 'Passed');
  const violations = pick(data, 'violations', 'Violations') || [];
  const hasC001 = violations.some(v => pick(v, 'ruleId', 'RuleId') === 'C-001');

  const types = (await getEvents(session, pipelineId)).map(e => pick(e, 'eventType', 'EventType'));
  const reported = types.includes('ConstraintViolationReported');

  const pass = critical >= 1 && passed === false && hasC001 && reported;
  record('D7', pass, `critical=${critical}, C-001=${hasC001}, event=${reported}`, { pipelineId });
  return pass;
}

/** D13：缺 UI 片段时 system-design 拒绝锁定 */
async function testD13_SystemDesignIncomplete(session) {
  const pipelineId = await createPipeline(session, `P3-D13-${Date.now()}`);
  await setupIr1Stable(session, pipelineId);
  await simulate(session, pipelineId, { eventType: 'ArchitectureDecisionRecorded' });
  await simulate(session, pipelineId, { eventType: 'DDLStabilized' });

  await apiRequest('POST', `/api/studio/skills/system-design/${pipelineId}/run`, { body: {}, session });
  const terminal = await waitSkillTerminal(session, pipelineId, 'system-design-skill');

  const types = (await getEvents(session, pipelineId)).map(e => pick(e, 'eventType', 'EventType'));
  const locked = types.includes('SystemDesignLocked');
  const rejected = terminal.status === 'failed'
    && String(terminal.error).includes('FormPageIR');

  const pass = rejected && !locked;
  record('D13', pass, `run=${terminal.status}, locked=${locked}, err=${terminal.error}`, { pipelineId });
  return pass;
}

/** API：LLM budget 查询结构 */
async function testApi_LlmBudget(session) {
  const pipelineId = await createPipeline(session, `P3-Budget-${Date.now()}`);
  await setupIr1Stable(session, pipelineId);

  const res = await apiRequest('GET', `/api/studio/llm/budget/${pipelineId}`, { session });
  const data = jnpfData(res) || res.json?.data || res.json;
  const budget = pick(data, 'tokenBudget', 'TokenBudget');
  const consumed = pick(data, 'tokenConsumed', 'TokenConsumed');
  const canRun = pick(data, 'canRunDesign', 'CanRunDesign');

  const pass = isJnpfOk(res) && jnpfCode(res) === 200 && typeof budget === 'number'
    && typeof consumed === 'number' && typeof canRun === 'boolean';
  record('API-BUDGET', pass, `budget=${budget}, consumed=${consumed}, canRun=${canRun}`, { pipelineId });
  return pass;
}

/** IR-2 快照 Tab 数据：三片段投影 */
async function testIr2_Snapshots(session) {
  const pipelineId = await createPipeline(session, `P3-IR2-${Date.now()}`);
  await setupIr1Stable(session, pipelineId);
  await setupIr2Clean(session, pipelineId);

  const snaps = await getSnapshots(session, pipelineId);
  const types = snaps.map(s => pick(s, 'fragmentType', 'FragmentType'));
  const hasArch = types.includes('IR2_Architecture');
  const hasDdl = types.includes('IR2_DDL');
  const hasUi = types.includes('IR2_FormPageIR');

  const pass = hasArch && hasDdl && hasUi;
  record('IR2-SNAPSHOTS', pass, `arch=${hasArch}, ddl=${hasDdl}, ui=${hasUi}`, { pipelineId });
  return pass;
}

/** D15：TokenConsumed ≥ 95% budget → design/run 429 LLM_BUDGET_EXHAUSTED */
async function testD15_BudgetExhausted(session) {
  const pipelineId = await createPipeline(session, `P3-D15-${Date.now()}`);
  await setupIr1Stable(session, pipelineId);
  setProjectTokenConsumed(pipelineId, 0.96);

  const res = await apiRequest('POST', `/api/studio/skills/design/${pipelineId}/run`, {
    body: {},
    session,
  });
  const inner = res.json?.data ?? jnpfData(res);
  const exhaustedCode = pick(inner, 'code', 'Code');
  const pass = jnpfCode(res) === 429 && exhaustedCode === 'LLM_BUDGET_EXHAUSTED';

  resetProjectTokenConsumed(pipelineId);
  record('D15', pass, `code=${jnpfCode(res)}, data.code=${exhaustedCode}`, { pipelineId });
  return pass;
}

/** D19：analyst-skill 策略 MaxLlmCalls=0（零直连 LLM Gateway） */
async function testD19_AnalystZeroDirectLlm() {
  const maxCalls = getSkillLlmPolicy('analyst-skill');
  const pass = maxCalls === 0;
  record('D19', pass, `ai_skill_llm_policy analyst MaxLlmCalls=${maxCalls}`);
  return pass;
}

async function main() {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
  const session = await login();
  log('logged in as', session.account);

  await testD10_Ir1Reject(session);
  await testD1_DesignRunStart(session);
  await testD6_SystemDesignLocked(session);
  await testD7_ConstraintViolation(session);
  await testD13_SystemDesignIncomplete(session);
  await testApi_LlmBudget(session);
  await testIr2_Snapshots(session);
  await testD15_BudgetExhausted(session);
  await testD19_AnalystZeroDirectLlm();

  // 需 DB 种子 / 浏览器 / 双租户 — 标记 SKIP
  skip('D8', '双租户隔离需第二租户账号，手工验收');
  skip('D9', '手工低代码平台零影响，需人工回归');
  skip('D11', 'Serilog 结构化日志需查服务端日志文件');
  skip('D12', '同租户 4 pipeline 并行需 phase2.5-stress-e2e');
  skip('D14', '前端内存泄漏需 Playwright phase2.5-d16-browser');
  skip('D16', 'Skill maxCalls 需 architect ToT 第 4 次调用场景');
  skip('D17', 'maxTokensPerCall 需查 BASE_AI_CALL_LOG');
  skip('D18', 'token 累加需跑完 design 后 SQL 对账');
  skip('D20', '见阶段三文档 §7 扩展项');

  const runnable = results.filter(r => !r.skip);
  const passed = runnable.filter(r => r.pass).length;
  const report = {
    phase: 'phase3',
    passed,
    total: runnable.length,
    skipped: results.filter(r => r.skip).length,
    results,
    at: new Date().toISOString(),
  };
  fs.writeFileSync(REPORT_PATH, JSON.stringify(report, null, 2));
  log('report →', REPORT_PATH);
  log(`summary ${passed}/${runnable.length} (${report.skipped} skipped)`);

  if (passed < runnable.length) process.exit(1);
}

main().catch(err => {
  console.error('[phase3-dod] FATAL', err);
  process.exit(1);
});
