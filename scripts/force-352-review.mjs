#!/usr/bin/env node
import { login, apiRequest, pick, jnpfData } from './lib/jnpf-auth.mjs';

const ID = 352;
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

const s = await login();
console.log('ForceRefinalize...');
const run = await apiRequest('POST', `/api/studio/skills/requirement-analysis/${ID}/run`, {
  session: s,
  body: { forceRefinalize: true },
});
console.log(run.json?.code, JSON.stringify(run.json?.data).slice(0, 400));

for (let i = 0; i < 36; i++) {
  await sleep(8000);
  const res = await apiRequest('GET', `/api/studio/ir/${ID}/events`, { session: s });
  const list = jnpfData(res) ?? [];
  const pm = list.filter((e) => pick(e, 'EventType') === 'RequirementSpecPmReviewed');
  const scores = pm.map((e) => {
    const raw = String(pick(e, 'PayloadPreview') || '');
    const m = raw.match(/"score"\s*:\s*(\d+)/);
    return { score: m ? Number(m[1]) : null, preview: raw.slice(0, 220) };
  });
  const skel = list.filter((e) => pick(e, 'EventType') === 'SkeletonCreated').length;
  console.log(`t=${(i + 1) * 8}s pmCount=${pm.length} scores=${JSON.stringify(scores.map((x) => x.score))} skel=${skel}`);
  if (pm.length >= 2 || (pm.length >= 1 && scores.at(-1)?.score !== 0 && i > 2)) {
    console.log('[DONE]', JSON.stringify(scores, null, 2));
    break;
  }
}
