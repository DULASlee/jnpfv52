#!/usr/bin/env node
/**
 * 驱动 pipeline 352 需求分析三轮（skipAll 推进）并报告 PM 终评分。
 * 用法: node scripts/smoke-352-req-score.mjs
 */
import { login, apiRequest, pick, jnpfData } from './lib/jnpf-auth.mjs';

const PIPELINE_ID = Number(process.env.E2E_PIPELINE_ID || 352);

function unwrap(res) {
  const body = res.json;
  const code = body?.code ?? body?.Code;
  if (code != null && code !== 200 && code !== 0) {
    throw new Error(`API fail ${res.status}: ${JSON.stringify(body).slice(0, 500)}`);
  }
  return jnpfData(body) ?? body?.data ?? body?.Data ?? body;
}

async function listEvents(session) {
  const res = await apiRequest('GET', `/api/studio/ir/${PIPELINE_ID}/events`, { session });
  const data = jnpfData(res) ?? res.json?.data ?? res.json;
  return Array.isArray(data) ? data : (data?.items || data?.Items || []);
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

function extractSetId(evt) {
  const raw = String(pick(evt, 'payloadPreview', 'PayloadPreview') || '');
  const m = raw.match(/"setId"\s*:\s*"([^"]+)"/i);
  return m?.[1] || null;
}

function pmReviews(events) {
  return events
    .filter((e) => pick(e, 'eventType', 'EventType') === 'RequirementSpecPmReviewed')
    .map((e) => {
      const raw = String(pick(e, 'payloadPreview', 'PayloadPreview') || '');
      const m = raw.match(/"score"\s*:\s*(\d+)/);
      return {
        score: m ? Number(m[1]) : null,
        skillId: pick(e, 'skillId', 'SkillId'),
        at: pick(e, 'createdAt', 'CreatedAt'),
        raw: raw.slice(0, 280),
      };
    });
}

function typeCounts(events) {
  const types = {};
  for (const e of events) {
    const t = pick(e, 'eventType', 'EventType') || '?';
    types[t] = (types[t] || 0) + 1;
  }
  return types;
}

async function waitFor(session, predicate, label, timeoutMs = 600_000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const events = await listEvents(session);
    const hit = predicate(events);
    if (hit) return { events, hit };
    await sleep(4000);
  }
  throw new Error(`timeout waiting ${label}`);
}

async function runOrch(session, body = {}) {
  const res = await apiRequest('POST', `/api/studio/skills/requirement-analysis/${PIPELINE_ID}/run`, {
    session,
    body,
    timeoutMs: 120_000,
  });
  return unwrap(res);
}

async function answerSkipAll(session, setId) {
  const res = await apiRequest('POST', `/api/studio/skills/clarification/${PIPELINE_ID}/answer`, {
    session,
    body: { setId, skipAll: true, answers: [], skippedQuestionIds: [] },
    timeoutMs: 120_000,
  });
  return unwrap(res);
}

async function main() {
  const session = await login();
  let events = await listEvents(session);
  console.log('[baseline]', JSON.stringify({
    eventCount: events.length,
    types: typeCounts(events),
    pm: pmReviews(events),
  }, null, 2));

  const hasFinalized = events.some((e) => {
    if (pick(e, 'eventType', 'EventType') !== 'AnalysisCompleted') return false;
    const raw = String(pick(e, 'payloadPreview', 'PayloadPreview') || '');
    return raw.includes('"finalized":true') || raw.includes('"finalized": true');
  });
  const clarAnswered = events.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationAnswered').length;
  const clarRequested = events.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationRequested').length;

  // 若已 Finalize，强制再 Finalize+Review 看最新分；否则推进三轮
  if (hasFinalized && clarAnswered >= 3) {
    console.log('[path] already 3 rounds + finalized → ForceRefinalize + Review');
    await runOrch(session, { forceRefinalize: true });
    const { events: after } = await waitFor(
      session,
      (ev) => pmReviews(ev).length > pmReviews(events).length
        || (pmReviews(ev).at(-1)?.at !== pmReviews(events).at(-1)?.at),
      'new-PmReviewed',
      600_000,
    );
    console.log('[result]', JSON.stringify({
      types: typeCounts(after),
      pm: pmReviews(after),
      latestScore: pmReviews(after).at(-1)?.score ?? null,
    }, null, 2));
    return;
  }

  console.log('[path] drive rounds skipAll clarReq=', clarRequested, 'clarAns=', clarAnswered);

  for (let i = 0; i < 6; i++) {
    events = await listEvents(session);
    const pm = pmReviews(events);
    if (pm.length > 0 && events.some((e) => {
      if (pick(e, 'eventType', 'EventType') !== 'AnalysisCompleted') return false;
      return String(pick(e, 'payloadPreview', 'PayloadPreview') || '').includes('finalized":true');
    })) {
      console.log('[done] finalized + pm score');
      break;
    }

    const pending = [...events].reverse().find((e) => {
      if (pick(e, 'eventType', 'EventType') !== 'ClarificationRequested') return false;
      const frag = pick(e, 'fragmentId', 'FragmentId') || '';
      // 找尚未 Answered 的 requirement-analysis round fragment：看同 fragment 最新是否 Answered
      const answered = events.some(
        (a) => pick(a, 'eventType', 'EventType') === 'ClarificationAnswered'
          && pick(a, 'fragmentId', 'FragmentId') === frag,
      );
      return !answered;
    });

    if (pending) {
      const setId = extractSetId(pending);
      if (!setId) throw new Error('cannot extract setId from pending clarification');
      console.log('[answer] setId=', setId, 'frag=', pick(pending, 'fragmentId', 'FragmentId'));
      await answerSkipAll(session, setId);
    }

    console.log('[run] orchestrator…');
    await runOrch(session);
    await waitFor(
      session,
      (ev) => {
        const req = ev.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationRequested').length;
        const ans = ev.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationAnswered').length;
        const pmNow = pmReviews(ev);
        const finalized = ev.some((e) => {
          if (pick(e, 'eventType', 'EventType') !== 'AnalysisCompleted') return false;
          return String(pick(e, 'payloadPreview', 'PayloadPreview') || '').includes('finalized":true');
        });
        // 推进：新出题 / 新终评 / 已 finalize
        return finalized || pmNow.length > pm.length || req > clarRequested || ans > clarAnswered
          || (pending && ans > events.filter((e) => pick(e, 'eventType', 'EventType') === 'ClarificationAnswered').length);
      },
      `round-progress-${i}`,
      600_000,
    );
    events = await listEvents(session);
    console.log('[progress]', JSON.stringify({
      types: typeCounts(events),
      pm: pmReviews(events),
    }));
  }

  events = await listEvents(session);
  const reviews = pmReviews(events);
  console.log('[final]', JSON.stringify({
    pipelineId: PIPELINE_ID,
    types: typeCounts(events),
    pmReviews: reviews,
    latestScore: reviews.at(-1)?.score ?? null,
    latestRaw: reviews.at(-1)?.raw ?? null,
  }, null, 2));
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
