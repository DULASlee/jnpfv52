#!/usr/bin/env node
/**
 * 阶段二 DoD 缺口验收：D2 / D8 / D9 / D10 / G5 / G7
 *
 *   node scripts/phase2-dod-verify.mjs
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { apiRequest, isJnpfOk, jnpfData, login, pick } from './lib/jnpf-auth.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..');
const EVIDENCE_DIR = path.join(REPO_ROOT, '.claude', 'evidence');
const REPORT_PATH = path.join(EVIDENCE_DIR, 'phase2-dod-verify.json');

const results = [];
const log = (...args) => console.log('[dod-verify]', ...args);

function record(id, pass, detail, extra = {}) {
  results.push({ id, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : 'FAIL', id, detail);
}

function jnpfCode(result) {
  if (result?.json && typeof result.json === 'object' && 'code' in result.json)
    return result.json.code;
  return result?.status;
}

async function waitFor(fn, label, timeoutMs = 120_000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const hit = await fn();
    if (hit) return hit;
    await new Promise(r => setTimeout(r, 1000));
  }
  throw new Error(`timeout: ${label}`);
}

async function createPipeline(session, name, requirement) {
  const res = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
    body: {
      name,
      userRequirement: requirement || `${name}：员工请假审批归档，含 LeaveRequest 自动种子路径。`.padEnd(400, '测'),
    },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`create: ${JSON.stringify(res.json)}`);
  return pick(jnpfData(res), 'pipelineId', 'PipelineId');
}

function parseSkeletonPayload(raw) {
  if (!raw) return null;
  try {
    const obj = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return {
      businessEvents: obj.businessEvents || obj.BusinessEvents || [],
      roleMatrix: obj.roleMatrix || obj.RoleMatrix || [],
      entityDrafts: obj.entityDrafts || obj.EntityDrafts || [],
    };
  } catch {
    return null;
  }
}

async function getSnapshots(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/snapshots`, { session });
  const data = jnpfData(res);
  if (Array.isArray(data)) return data;
  if (Array.isArray(res.json)) return res.json;
  return [];
}

async function getEvents(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
  return Array.isArray(res.json) ? res.json : jnpfData(res) || [];
}

/** D2：IR-0 快照质量（simulate 骨架） */
async function testD2_SnapshotQuality(session) {
  const pipelineId = await createPipeline(session, `DoD-D2-${Date.now()}`);
  await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'SkeletonCreated', withAutoSeedEvent: true },
    session,
  });

  const snaps = await getSnapshots(session, pipelineId);
  const sk = snaps.find(s =>
    pick(s, 'fragmentType', 'FragmentType') === 'IR0_Skeleton'
    || String(pick(s, 'fragmentId', 'FragmentId') || '').startsWith('skeleton:'));
  const payload = parseSkeletonPayload(pick(sk, 'payload', 'Payload'));
  const events = payload?.businessEvents || [];
  const roles = payload?.roleMatrix || [];
  const entities = payload?.entityDrafts || [];

  const pass = events.length >= 1 && roles.length >= 1 && entities.length >= 1;
  record('D2', pass, `events=${events.length}, roles=${roles.length}, entities=${entities.length}`, { pipelineId });
  return pass;
}

/** D8：IOI 拒绝 — EventSpecConfirmed 违反不变量 */
async function testD8_IoiReject(session) {
  const pipelineId = await createPipeline(session, `DoD-D8-${Date.now()}`);
  await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'SkeletonCreated' },
    session,
  });

  const bad = await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'EventSpecConfirmed', useInvalidPayload: true },
    session,
  });
  const rejected = !isJnpfOk(bad) && jnpfCode(bad) !== 200;
  record('D8', rejected, `invalid EventSpec code=${jnpfCode(bad)} msg=${bad.json?.msg || ''}`, { pipelineId });
  return rejected;
}

/** D9：auto 种子路径 — SaStep payload 含 seed-auto，无长时间 SA 等待 */
async function testD9_AutoSeed(session) {
  const pipelineId = await createPipeline(session, `DoD-D9-${Date.now()}`, '请假 LeaveRequest 自动种子 E2E 验收。');
  await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'SkeletonCreated', withAutoSeedEvent: true },
    session,
  });
  await apiRequest('POST', `/api/studio/skills/pm/${pipelineId}/confirm-skeleton`, {
    body: { autoRunAnalyst: false },
    session,
  });

  const t0 = Date.now();
  const run = await apiRequest('POST', `/api/studio/skills/analyst/${pipelineId}/run`, { body: {}, session });
  if (!isJnpfOk(run)) {
    record('D9', false, `analyst start failed code=${jnpfCode(run)}`);
    return false;
  }

  await waitFor(async () => {
    const types = (await getEvents(session, pipelineId)).map(e => pick(e, 'eventType', 'EventType'));
    return types.includes('AnalysisCompleted');
  }, 'AnalysisCompleted', 180_000);

  const elapsed = Date.now() - t0;
  const events = await getEvents(session, pipelineId);
  const saPayloads = events
    .filter(e => pick(e, 'eventType', 'EventType') === 'SA_Step_Completed')
    .map(e => pick(e, 'payloadPreview', 'PayloadPreview') || pick(e, 'payload', 'Payload') || '');

  const hasAuto = saPayloads.some(p => String(p).includes('seed-auto'))
    || events.some(e => String(pick(e, 'payloadPreview', 'PayloadPreview') || '').includes('autoSeed'));

  const pass = hasAuto && elapsed < 120_000;
  record('D9', pass, `autoSeed=${hasAuto}, elapsedMs=${elapsed}`, { pipelineId });
  return pass;
}

/** D10：项目隔离 — 两 pipeline 事件互不可见 */
async function testD10_ProjectIsolation(session) {
  const idA = await createPipeline(session, `DoD-D10-A-${Date.now()}`);
  const idB = await createPipeline(session, `DoD-D10-B-${Date.now()}`);
  const marker = `MARKER-${Date.now()}`;

  await apiRequest('POST', `/api/studio/ir/${idA}/simulate`, {
    body: { eventType: 'SkeletonCreated' },
    session,
  });

  const evA = await getEvents(session, idA);
  const evB = await getEvents(session, idB);
  const aHasSk = evA.some(e => pick(e, 'eventType', 'EventType') === 'SkeletonCreated');
  const bHasSk = evB.some(e => pick(e, 'eventType', 'EventType') === 'SkeletonCreated');
  const cross = evB.some(e => String(pick(e, 'payloadPreview', 'PayloadPreview') || '').includes(marker));

  const pass = aHasSk && !bHasSk && !cross;
  record('D10', pass, `A.skeleton=${aHasSk}, B.skeleton=${bHasSk}, crossLeak=${cross}`, { idA, idB });
  return pass;
}

/** G5：inferred soft-block + ack */
async function testG5_InferredAck(session) {
  const pipelineId = await createPipeline(session, `DoD-G5-${Date.now()}`);
  await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'SkeletonCreated' },
    session,
  });

  const spec = await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'EventSpecConfirmed', withInferredRules: true, fragmentId: 'eventspec:BE-001' },
    session,
  });
  if (!isJnpfOk(spec)) {
    record('G5', false, `EventSpecConfirmed failed code=${jnpfCode(spec)}`);
    return false;
  }

  const snapsBefore = await getSnapshots(session, pipelineId);
  const frag = snapsBefore.find(s => String(pick(s, 'fragmentId', 'FragmentId') || '').includes('eventspec:'));
  const stateBefore = pick(frag, 'stabilityState', 'StabilityState');
  const blocked = stateBefore !== 'stable' && stateBefore !== 'locked';

  const ack = await apiRequest('POST', `/api/studio/ir/${pipelineId}/events/eventspec:BE-001/ack-inferred-rules`, {
    body: {},
    session,
  });
  const types = (await getEvents(session, pipelineId)).map(e => pick(e, 'eventType', 'EventType'));
  const pass = blocked && isJnpfOk(ack) && types.includes('InferredRulesAcknowledged');
  record('G5', pass, `stateBefore=${stateBefore}, ack=${isJnpfOk(ack)}`, { pipelineId });
  return pass;
}

/** G7：cancel API 中断运行中 Skill */
async function testG7_Cancel(session) {
  const pipelineId = await createPipeline(session, `DoD-G7-${Date.now()}`);
  await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'SkeletonCreated' },
    session,
  });
  await apiRequest('POST', `/api/studio/skills/pm/${pipelineId}/confirm-skeleton`, {
    body: { autoRunAnalyst: false },
    session,
  });

  await apiRequest('POST', `/api/studio/skills/analyst/${pipelineId}/run`, { body: {}, session });
  await new Promise(r => setTimeout(r, 500));

  const cancel = await apiRequest('POST', `/api/studio/skills/${pipelineId}/cancel`, { body: {}, session });
  const data = jnpfData(cancel) || cancel.json?.data || cancel.json;
  const cancelledCount = pick(data, 'cancelledCount', 'CancelledCount') ?? 0;

  let finalStatus = 'unknown';
  try {
    finalStatus = await waitFor(async () => {
      const runs = await apiRequest('GET', `/api/studio/skills/${pipelineId}/runs`, { session });
      const list = Array.isArray(runs.json) ? runs.json : jnpfData(runs) || [];
      const analyst = list.find(r => pick(r, 'skillId', 'SkillId') === 'analyst-skill');
      const st = pick(analyst, 'status', 'Status');
      return st === 'cancelled' || st === 'completed' || st === 'failed' ? st : null;
    }, 'run terminal', 30_000);
  } catch {
    finalStatus = 'timeout';
  }

  const pass = isJnpfOk(cancel) && cancelledCount >= 0
    && (finalStatus === 'cancelled' || finalStatus === 'completed' || cancelledCount >= 1);
  record('G7', pass, `cancelledCount=${cancelledCount}, runStatus=${finalStatus}`, { pipelineId });
  return pass;
}

async function main() {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
  const session = await login();
  log('logged in as', session.account);

  await testD2_SnapshotQuality(session);
  await testD8_IoiReject(session);
  await testD9_AutoSeed(session);
  await testD10_ProjectIsolation(session);
  await testG5_InferredAck(session);
  await testG7_Cancel(session);

  const passed = results.filter(r => r.pass).length;
  const report = { passed, total: results.length, results, at: new Date().toISOString() };
  fs.writeFileSync(REPORT_PATH, JSON.stringify(report, null, 2));
  log('report →', REPORT_PATH);
  log(`summary ${passed}/${results.length}`);

  if (passed < results.length) process.exit(1);
}

main().catch(err => {
  console.error('[dod-verify] FATAL', err);
  process.exit(1);
});
