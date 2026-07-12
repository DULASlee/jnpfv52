#!/usr/bin/env node
/**
 * 冒烟：验证 PM Refine / Analyst 语义分析写回骨架（非仅 Compile）
 * 用法: node scripts/smoke-pm-analyst-refine.mjs
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login, apiRequest, pick, jnpfData } from './lib/jnpf-auth.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const EVIDENCE = path.join(__dirname, '../.claude/evidence/smoke-pm-analyst-refine.json');

function unwrap(res) {
  const body = res.json;
  const code = body?.code ?? body?.Code;
  if (code != null && code !== 200 && code !== 0) {
    throw new Error(`API fail ${res.status}: ${JSON.stringify(body).slice(0, 600)}`);
  }
  return jnpfData(body) ?? body?.data ?? body?.Data ?? body;
}

async function listEvents(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
  const data = jnpfData(res) ?? res.json?.data ?? res.json;
  return Array.isArray(data) ? data : (data?.items || data?.Items || []);
}

function skeletons(events) {
  return events
    .filter((e) => pick(e, 'eventType', 'EventType') === 'SkeletonCreated')
    .map((e) => ({
      skillId: pick(e, 'skillId', 'SkillId'),
      fragmentId: pick(e, 'fragmentId', 'FragmentId'),
      at: pick(e, 'createdAt', 'CreatedAt'),
      preview: String(pick(e, 'payloadPreview', 'PayloadPreview') || '').slice(0, 120),
    }));
}

function pmReviews(events) {
  return events
    .filter((e) => pick(e, 'eventType', 'EventType') === 'RequirementSpecPmReviewed')
    .map((e) => {
      const raw = String(pick(e, 'payloadPreview', 'PayloadPreview') || '');
      let score = null;
      try {
        score = JSON.parse(raw)?.score ?? null;
      } catch {
        const m = raw.match(/"score"\s*:\s*(\d+)/);
        if (m) score = Number(m[1]);
      }
      return { score, skillId: pick(e, 'skillId', 'SkillId'), raw: raw.slice(0, 240) };
    });
}

async function sleep(ms) {
  await new Promise((r) => setTimeout(r, ms));
}

async function runOrchestrator(session, pipelineId, body = {}) {
  const res = await apiRequest('POST', `/api/studio/skills/requirement-analysis/${pipelineId}/run`, {
    session,
    body,
    timeoutMs: 600_000,
  });
  return unwrap(res);
}

async function waitFor(session, pipelineId, predicate, label, timeoutMs = 600_000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const events = await listEvents(session, pipelineId);
    const hit = predicate(events);
    if (hit) return { events, hit };
    await sleep(4000);
  }
  throw new Error(`timeout waiting ${label} on pipeline ${pipelineId}`);
}

function latestClarificationRequested(events) {
  const list = events.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationRequested');
  return list.at(-1) || null;
}

function extractSetIdFromPreview(evt) {
  const raw = String(pick(evt, 'payloadPreview', 'PayloadPreview', 'payload', 'Payload') || '');
  const m = raw.match(/"setId"\s*:\s*"([^"]+)"/i) || raw.match(/"SetId"\s*:\s*"([^"]+)"/);
  return m?.[1] || null;
}

async function main() {
  const session = await login();
  const report = { at: new Date().toISOString(), steps: {} };

  const baseEvents = await listEvents(session, 343);
  report.steps.baseline343 = {
    pmReviews: pmReviews(baseEvents),
    note: '历史：首次 score=40；后续 ForceRefinalize 曾出现 JSON/空响应 → 0',
  };
  console.log('[baseline343]', JSON.stringify(report.steps.baseline343.pmReviews));

  const name = `smoke-refine-${Date.now()}`;
  const requirement =
    '做一套员工请假审批系统：支持年假/病假/事假，部门经理与人事两级审批，允许人事代提，支持撤回与驳回，站内信通知。';
  const createRes = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
    session,
    body: { name, requirement, userRequirement: requirement },
  });
  const created = unwrap(createRes);
  const pipelineId = Number(created?.pipelineId ?? created?.PipelineId ?? created?.id ?? created?.Id);
  if (!pipelineId) throw new Error('create pipeline failed: ' + JSON.stringify(created).slice(0, 400));
  report.pipelineId = pipelineId;
  console.log('[create]', pipelineId, name);

  await runOrchestrator(session, pipelineId);
  console.log('[round1] waiting ClarificationRequested…');
  const { events: eventsAsk, hit: clarEvt } = await waitFor(
    session,
    pipelineId,
    (ev) => latestClarificationRequested(ev),
    'ClarificationRequested',
  );
  const setId = extractSetIdFromPreview(clarEvt);
  report.steps.round1ask = {
    skeletons: skeletons(eventsAsk),
    setId,
    pmSkillSkeletonCount: skeletons(eventsAsk).filter((s) => s.skillId === 'pm-skill').length,
  };
  console.log('[round1ask]', JSON.stringify(report.steps.round1ask));
  if (!setId) {
    fs.mkdirSync(path.dirname(EVIDENCE), { recursive: true });
    fs.writeFileSync(EVIDENCE, JSON.stringify(report, null, 2));
    throw new Error('cannot extract setId from ClarificationRequested preview');
  }

  // skipAll 仍会写 ClarificationAnswered 并触发 PM Refine（验证完善主体接线）
  const ansRes = await apiRequest('POST', `/api/studio/skills/clarification/${pipelineId}/answer`, {
    session,
    body: { setId, skipAll: true, answers: [], skippedQuestionIds: [] },
    timeoutMs: 120_000,
  });
  report.steps.answer = unwrap(ansRes);
  console.log('[answer-skipAll]', JSON.stringify(report.steps.answer).slice(0, 280));

  const skBefore = skeletons(eventsAsk).filter((s) => s.skillId === 'pm-skill').length;
  await runOrchestrator(session, pipelineId);
  console.log('[afterAnswer] waiting refine / round2…');
  const { events: eventsRefine } = await waitFor(
    session,
    pipelineId,
    (ev) => {
      const pmSk = skeletons(ev).filter((s) => s.skillId === 'pm-skill').length;
      const analystSk = skeletons(ev).filter((s) => s.skillId === 'analyst-skill').length;
      const answered = ev.some((e) => pick(e, 'eventType', 'EventType') === 'ClarificationAnswered');
      const clarN = ev.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationRequested').length;
      return answered && (pmSk > skBefore || analystSk >= 1 || clarN >= 2);
    },
    'PM-Refine-or-Round2',
    600_000,
  );

  const sk = skeletons(eventsRefine);
  report.steps.afterPmRefine = {
    skeletons: sk,
    pmSkillSkeletonCount: sk.filter((s) => s.skillId === 'pm-skill').length,
    analystSkeletonCount: sk.filter((s) => s.skillId === 'analyst-skill').length,
    clarAnswered: eventsRefine.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationAnswered').length,
    clarRequested: eventsRefine.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationRequested').length,
  };
  console.log('[afterPmRefine]', JSON.stringify(report.steps.afterPmRefine, null, 2));

  // Round2：再 skipAll，推进 Analyst semantic
  if ((report.steps.afterPmRefine.clarRequested || 0) >= 2) {
    const clar2 = latestClarificationRequested(eventsRefine);
    const setId2 = extractSetIdFromPreview(clar2);
    if (setId2) {
      await apiRequest('POST', `/api/studio/skills/clarification/${pipelineId}/answer`, {
        session,
        body: { setId: setId2, skipAll: true, answers: [], skippedQuestionIds: [] },
        timeoutMs: 120_000,
      });
      await runOrchestrator(session, pipelineId);
      console.log('[round2] waiting analyst-skill SkeletonCreated…');
      const { events: eventsR2 } = await waitFor(
        session,
        pipelineId,
        (ev) => skeletons(ev).some((s) => s.skillId === 'analyst-skill')
          || skeletons(ev).filter((s) => s.skillId === 'pm-skill').length
            > report.steps.afterPmRefine.pmSkillSkeletonCount,
        'Analyst-or-extra-PM-Refine',
        600_000,
      );
      report.steps.afterRound2 = {
        skeletons: skeletons(eventsR2),
        analystSkeletonCount: skeletons(eventsR2).filter((s) => s.skillId === 'analyst-skill').length,
        pmSkillSkeletonCount: skeletons(eventsR2).filter((s) => s.skillId === 'pm-skill').length,
      };
      console.log('[afterRound2]', JSON.stringify(report.steps.afterRound2, null, 2));
    }
  }

  report.verdict = {
    pmRefineLikely: (report.steps.afterPmRefine?.pmSkillSkeletonCount || 0) > skBefore,
    analystSemanticSeen: (report.steps.afterRound2?.analystSkeletonCount || 0) >= 1,
    pipelineId,
    skBefore,
    note: 'pm SkeletonCreated 条数增加 ⇒ Refine 写回；analyst-skill SkeletonCreated ⇒ Round2 语义分析',
  };

  fs.mkdirSync(path.dirname(EVIDENCE), { recursive: true });
  fs.writeFileSync(EVIDENCE, JSON.stringify(report, null, 2));
  console.log('[evidence]', EVIDENCE);
  console.log('[verdict]', JSON.stringify(report.verdict));

  if (!report.verdict.pmRefineLikely && !report.verdict.analystSemanticSeen) process.exit(1);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
