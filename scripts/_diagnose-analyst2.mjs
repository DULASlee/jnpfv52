import { login, apiRequest, jnpfData, pick } from './lib/jnpf-auth.mjs';

const s = await login();
const ev = jnpfData(await apiRequest('GET', '/api/studio/ir/301/events', { session: s })) || [];
const types = {};
for (const e of ev) {
  const t = pick(e, 'eventType', 'EventType') || '?';
  types[t] = (types[t] || 0) + 1;
}
console.log('all IR types:', types);

const sa = ev.filter(e => (pick(e, 'eventType', 'EventType') || '').includes('SA'));
const last = sa.slice(-1)[0];
console.log('last SA event:', JSON.stringify({
  step: pick(last, 'saStepName', 'SaStepName'),
  frag: pick(last, 'fragmentId', 'FragmentId'),
  skill: pick(last, 'skillId', 'SkillId'),
}, null, 2));

// download skeleton json from deliverable
const sk = await apiRequest('GET', '/api/studio/pipeline/execute/301/deliverables/content?relativePath=01-skeleton.md', { session: s });
const body = sk.text || JSON.stringify(sk.json);
const beMatch = body.match(/businessEvents[\s\S]{0,2000}/);
console.log('skeleton snippet:', (beMatch ? beMatch[0] : body.slice(0, 400)).slice(0, 600));
