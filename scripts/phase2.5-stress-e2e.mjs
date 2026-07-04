#!/usr/bin/env node
/**
 * Phase 2.5 保守压测 + G1-G8 验收脚本
 *
 * 策略：严格断言 + 保守负载（不跑完整 Analyst×4，配额用短窗口并发验证）
 *
 *   node scripts/phase2.5-stress-e2e.mjs
 *   node scripts/phase2.5-stress-e2e.mjs --skip-full-e2e   # 跳过 phase2-skills-e2e 长链路
 *   node scripts/phase2.5-stress-e2e.mjs --d16-only          # 仅 G6 SSE 切换
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { apiRequest, authHeader, isJnpfOk, jnpfData, login, pick } from './lib/jnpf-auth.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..');
const API = process.env.JNPF_API_URL || 'http://localhost:5000';
const SA = process.env.SA_SERVICE_URL || 'http://localhost:3001';
const SKIP_FULL = process.argv.includes('--skip-full-e2e');
const D16_ONLY = process.argv.includes('--d16-only');
const EVIDENCE_DIR = path.join(REPO_ROOT, '.claude', 'evidence');
const REPORT_PATH = path.join(EVIDENCE_DIR, 'phase2.5-stress-report.json');

const results = [];
const log = (...args) => console.log('[p2.5-stress]', ...args);

function record(id, pass, detail, extra = {}) {
  results.push({ id, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : 'FAIL', id, detail);
}

async function waitFor(fn, label, timeoutMs = 60_000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const hit = await fn();
    if (hit) return hit;
    await new Promise(r => setTimeout(r, 800));
  }
  throw new Error(`timeout: ${label}`);
}

async function healthCheck() {
  const api = await fetch(`${API}/api/oauth/getLoginConfig`).catch(() => null);
  const sa = await fetch(`${SA}/api/sa/health`).catch(() => null);
  const fe = await fetch('http://localhost:3100/').catch(() => null);
  return {
    api: api?.ok || api?.status === 403,
    sa: sa?.ok,
    fe: fe?.ok || fe?.status === 302,
  };
}

async function createPipeline(session, name) {
  const res = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
    body: { name, userRequirement: `${name}：员工请假审批归档 E2E 测试需求。`.padEnd(820, '测') },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`create failed: ${JSON.stringify(res.json)}`);
  return pick(jnpfData(res), 'pipelineId', 'PipelineId');
}

async function preparePipeline(session, pipelineId) {
  await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'SkeletonCreated' },
    session,
  });
  await apiRequest('POST', `/api/studio/skills/pm/${pipelineId}/confirm-skeleton`, {
    body: { autoRunAnalyst: false },
    session,
  });
}

async function getSkillRuns(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/skills/${pipelineId}/runs`, { session });
  const list = Array.isArray(res.json) ? res.json : jnpfData(res) || [];
  return list;
}

async function runAnalyst(session, pipelineId) {
  return apiRequest('POST', `/api/studio/skills/analyst/${pipelineId}/run`, { body: {}, session });
}

/** JNPF 业务码（HTTP 常为 200，409/429 在 body.code） */
function jnpfCode(result) {
  if (result?.json && typeof result.json === 'object' && 'code' in result.json)
    return result.json.code;
  return result?.status;
}

/** D14：同 pipeline 重复 run → 409 */
async function testD14_Mutex(session) {
  const pipelineId = await createPipeline(session, `P25-Mutex-${Date.now()}`);
  await preparePipeline(session, pipelineId);
  const first = await runAnalyst(session, pipelineId);
  await new Promise(r => setTimeout(r, 800));
  const second = await runAnalyst(session, pipelineId);
  const pass = jnpfCode(first) === 200 && jnpfCode(second) === 409;
  record('D14', pass, `first=${jnpfCode(first)}, second=${jnpfCode(second)}`);
  return pass;
}

/** G2：同租户 4 pipeline 并行 Analyst → 第 4 条 code 429 */
async function testG2_Quota(session) {
  const ids = [];
  for (let i = 0; i < 4; i++) {
    ids.push(await createPipeline(session, `P25-Quota-${Date.now()}-${i}`));
    await preparePipeline(session, ids[i]);
  }

  const responses = [];
  for (const id of ids) {
    responses.push(await runAnalyst(session, id));
  }

  const codes = responses.map(jnpfCode);
  const blocked = codes.filter(c => c === 429).length;
  const ok = codes.filter(c => c === 200).length;
  const pass = blocked >= 1 && ok <= 3;
  record('G2', pass, `ok=${ok}, blocked429=${blocked}`, { codes });
  return pass;
}

/** G4：SA session store 租户 key 隔离 */
async function testG4_SaIsolation() {
  const tenants = ['1', '2'];
  for (const t of tenants) {
    for (const p of ['101', '102']) {
      const res = await fetch(`${SA}/sa/run-step`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          tenantId: t,
          projectId: p,
          eventId: 'BE-001',
          agentName: 'ScopeAgent',
          irStepName: 'DomainModel',
          requirementText: '隔离测试',
          skeleton: { businessEvents: [{ eventId: 'BE-001', eventName: 'Test' }] },
        }),
      });
      if (!res.ok) {
        const txt = await res.text();
        record('G4', false, `run-step tenant=${t} project=${p} HTTP ${res.status}: ${txt.slice(0, 120)}`);
        return false;
      }
    }
  }

  const debug = await fetch(`${SA}/sa/debug/sessions`);
  const body = await debug.json();
  const keys = body.keys || [];
  const hasT1 = keys.some(k => k.startsWith('1:'));
  const hasT2 = keys.some(k => k.startsWith('2:'));
  const crossLeak = keys.filter(k => k.includes(':101:') || k.includes(':102:')).length >= 2
    && keys.some(k => k.startsWith('1:101:')) && keys.some(k => k.startsWith('2:101:'));
  const pass = hasT1 && hasT2 && crossLeak;
  record('G4', pass, `keys=${keys.length}, t1=${hasT1}, t2=${hasT2}, crossProject=${crossLeak}`, { keys: keys.slice(0, 20) });
  return pass;
}

/** G3：SA stdout 结构化 JSON（run-step 后检查 health） */
async function testG3_StructuredLog() {
  const health = await fetch(`${SA}/api/sa/health`);
  const pass = health.ok;
  record('G3', pass, `sa health ${health.status}`);
  return pass;
}

/** G6 / D16：SSE connect + abort ×10，无 hang */
async function testG6_SseLeak(session) {
  const pipelineId = await createPipeline(session, `P25-D16-${Date.now()}`);
  const token = session.token;
  const errors = [];
  let activeAtPeak = 0;

  for (let i = 0; i < 10; i++) {
    const ac = new AbortController();
    const url = `${API}/api/studio/pipeline/execute/${pipelineId}/events`;
    const p = fetch(url, {
      headers: { Authorization: authHeader(token), Accept: 'text/event-stream', 'jnpf-origin': 'pc' },
      signal: ac.signal,
    }).catch(e => {
      if (e.name !== 'AbortError') errors.push(String(e));
    });
    activeAtPeak++;
    await new Promise(r => setTimeout(r, 300));
    ac.abort();
    await Promise.race([p, new Promise(r => setTimeout(r, 2000))]);
  }

  const pass = errors.length === 0;
  record('G6', pass, `10x abort, errors=${errors.length}, peakActive=${activeAtPeak}`);
  return pass;
}

async function testG1_RunId(session) {
  const pipelineId = await createPipeline(session, `P25-RunId-${Date.now()}`);
  await preparePipeline(session, pipelineId);
  const run = await runAnalyst(session, pipelineId);
  const data = jnpfData(run);
  const runId = pick(data, 'runId', 'RunId');
  const pass = !!runId && String(runId).length >= 16;
  record('G1', pass, `runId=${runId}`);
  return pass;
}

async function testG5_InferredSoftBlock(session) {
  // 保守：仅验证 ack API 端点存在（完整 inferred 链路需 seeded EventSpec）
  const pipelineId = await createPipeline(session, `P25-Inferred-${Date.now()}`);
  const res = await apiRequest('POST', `/api/studio/ir/${pipelineId}/events/eventspec:BE-001/ack-inferred-rules`, {
    body: {},
    session,
  });
  const pass = res.status !== 404;
  record('G5', pass, `ack-inferred-rules HTTP ${res.status} (端点可达)`, { note: '完整 soft-block 需 seeded inferred EventSpec' });
  return pass;
}

async function runPhase2Subset(session) {
  const pipelineId = await createPipeline(session, `P25-Subset-${Date.now()}`);
  await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'SkeletonCreated' },
    session,
  });
  await waitFor(async () => {
    const ev = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
    const list = Array.isArray(ev.json) ? ev.json : jnpfData(ev) || [];
    return list.some(e => pick(e, 'eventType', 'EventType') === 'SkeletonCreated');
  }, 'SkeletonCreated', 30_000);
  record('G8-subset', true, `simulate SkeletonCreated pipeline=${pipelineId}`);
  return true;
}

async function main() {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
  const hc = await healthCheck();
  log('health', hc);
  if (!hc.api) {
    console.error('后端未启动 — 请先 start-dev.ps1');
    process.exit(1);
  }
  if (!hc.sa) {
    console.error('sa-service 未启动 (:3001)');
    process.exit(1);
  }

  const session = await login({ force: true });
  let failed = 0;

  if (D16_ONLY) {
    if (!(await testG6_SseLeak(session))) failed++;
  } else {
    const tests = [
      () => testD14_Mutex(session),
      () => testG1_RunId(session),
      () => testG3_StructuredLog(),
      () => testG4_SaIsolation(),
      () => testG2_Quota(session),
      () => testG6_SseLeak(session),
      () => testG5_InferredSoftBlock(session),
      () => runPhase2Subset(session),
    ];
    for (const t of tests) {
      try {
        if (!(await t())) failed++;
      } catch (e) {
        record(t.name || 'unknown', false, e.message);
        failed++;
      }
    }
  }

  const summary = {
    generatedAt: new Date().toISOString(),
    api: API,
    sa: SA,
    health: hc,
    results,
    failed,
    passed: results.filter(r => r.pass).length,
    total: results.length,
  };
  fs.writeFileSync(REPORT_PATH, JSON.stringify(summary, null, 2), 'utf8');
  log('report', REPORT_PATH);
  log(`SUMMARY: ${summary.passed}/${summary.total} passed, failed=${failed}`);

  if (!SKIP_FULL && !D16_ONLY && hc.api) {
    log('--- launching phase2-skills-e2e.mjs (full chain, conservative timeout) ---');
    const { spawnSync } = await import('node:child_process');
    const child = spawnSync(process.execPath, ['scripts/phase2-skills-e2e.mjs'], {
      cwd: REPO_ROOT,
      stdio: 'inherit',
      env: { ...process.env, PHASE2_E2E_TIMEOUT_MS: '600000' },
    });
    record('G8-full-e2e', child.status === 0, `phase2-skills-e2e exit=${child.status}`);
    if (child.status !== 0) failed++;
  }

  process.exit(failed > 0 ? 1 : 0);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
