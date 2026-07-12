#!/usr/bin/env node
import { login, apiRequest, pick, jnpfData } from './lib/jnpf-auth.mjs';

const ID = 352;
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function getSnapSetId(session, round) {
  const res = await apiRequest('GET', `/api/studio/ir/${ID}/snapshots`, { session });
  const list = jnpfData(res) ?? [];
  const frag = `clarification:requirement-analysis-round${round}:${ID}`;
  const snap = (Array.isArray(list) ? list : []).find((x) => pick(x, 'FragmentId') === frag);
  if (!snap) return null;
  const state = pick(snap, 'StabilityState');
  const payload = pick(snap, 'Payload');
  const setId = payload?.setId || payload?.SetId || null;
  return { setId, state, frag };
}

async function events(session) {
  const res = await apiRequest('GET', `/api/studio/ir/${ID}/events`, { session });
  return jnpfData(res) ?? [];
}

function scores(list) {
  return list
    .filter((e) => pick(e, 'EventType') === 'RequirementSpecPmReviewed')
    .map((e) => {
      const raw = String(pick(e, 'PayloadPreview') || '');
      const m = raw.match(/"score"\s*:\s*(\d+)/);
      return { score: m ? Number(m[1]) : null, preview: raw.slice(0, 280) };
    });
}

function counts(list) {
  const t = {};
  for (const e of list) {
    const k = pick(e, 'EventType') || '?';
    t[k] = (t[k] || 0) + 1;
  }
  return t;
}

async function answerIfNeeded(session, round) {
  const info = await getSnapSetId(session, round);
  if (!info?.setId) return { skipped: true, reason: 'no-snap', info };
  if (info.state === 'stable') return { skipped: true, reason: 'already-stable', info };
  console.log(`[answer] round${round}`, info);
  const ans = await apiRequest('POST', `/api/studio/skills/clarification/${ID}/answer`, {
    session,
    body: { setId: info.setId, skipAll: true, answers: [], skippedQuestionIds: [] },
  });
  console.log('  answer', ans.json?.code, ans.json?.msg, JSON.stringify(ans.json?.data).slice(0, 300));
  return { skipped: false, ok: ans.json?.code === 200, info, ans };
}

async function main() {
  const session = await login();

  for (let i = 0; i < 60; i++) {
    const list = await events(session);
    const sc = scores(list);
    const t = counts(list);
    console.log(
      `t=${i * 10}s ans=${t.ClarificationAnswered || 0} req=${t.ClarificationRequested || 0} skel=${t.SkeletonCreated || 0} pm=${JSON.stringify(sc.map((x) => x.score))}`,
    );
    if (sc.length) {
      console.log('[DONE]', JSON.stringify({ scores: sc, types: t }, null, 2));
      return;
    }

    // 按轮次答 in-progress
    let answered = false;
    for (const round of [1, 2, 3]) {
      const r = await answerIfNeeded(session, round);
      if (r.skipped) continue;
      answered = true;
      if (!r.ok) break;
      const run = await apiRequest('POST', `/api/studio/skills/requirement-analysis/${ID}/run`, {
        session,
        body: {},
      });
      console.log('  run', run.json?.code, JSON.stringify(run.json?.data).slice(0, 400));
      break;
    }

    if (!answered) {
      const run = await apiRequest('POST', `/api/studio/skills/requirement-analysis/${ID}/run`, {
        session,
        body: {},
      });
      console.log('  kick', run.json?.code, JSON.stringify(run.json?.data).slice(0, 400));
    }

    await sleep(10000);
  }
  console.error('timeout');
  process.exit(1);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
