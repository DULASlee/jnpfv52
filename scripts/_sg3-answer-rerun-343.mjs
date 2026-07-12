import { login, apiRequest, pick } from './lib/jnpf-auth.mjs';
import {
  waitSkillTerminal,
  getEvents,
  getDeliverables,
  writeEvidence,
  log,
} from './lib/phase-sup-api.mjs';

const id = 343;
const session = await login();
const events = await getEvents(session, id);

const clar = events.find((e) => {
  const t = e.eventType || e.EventType || '';
  const fid = String(e.fragmentId || e.FragmentId || '');
  return t === 'ClarificationRequested' && fid.includes('architecture');
});

if (!clar) {
  log('no ClarificationRequested architecture — listing clarification events');
  for (const e of events.filter((x) => /Clarification/i.test(x.eventType || x.EventType || ''))) {
    log(' ', e.eventType || e.EventType, e.fragmentId || e.FragmentId);
  }
  throw new Error('no architecture ClarificationRequested');
}

const raw = String(clar.payloadPreview || clar.PayloadPreview || '');
const setIdMatch = raw.match(/"setId"\s*:\s*"([^"]+)"/);
const setId = setIdMatch?.[1] || pick(
  (() => {
    try {
      return JSON.parse(raw);
    } catch {
      return {};
    }
  })(),
  'setId',
  'SetId',
);
log('fragment', clar.fragmentId || clar.FragmentId, 'setId', setId);
if (!setId) throw new Error('setId missing in payload');

const ans = await apiRequest('POST', `/api/studio/skills/clarification/${id}/answer`, {
  session,
  body: { setId, skipAll: true },
});
log('answer', JSON.stringify(ans.json || ans).slice(0, 600));

const next = pick(ans.json?.data || ans.json || {}, 'nextAction', 'NextAction');
log('nextAction', next);

const run = await apiRequest('POST', `/api/studio/skills/architect/${id}/run`, {
  session,
  body: {},
});
log('rerun', JSON.stringify(run.json || run).slice(0, 500));

const terminal = await waitSkillTerminal(session, id, 'architect-skill', 600_000);
log('terminal', terminal);

const items = await getDeliverables(session, id);
const names = items.map((i) => i.name || i.Name || i.fileName || i.FileName);
const ev2 = await getEvents(session, id);
const types = ev2.map((e) => e.eventType || e.EventType);

const evidence = {
  pipelineId: id,
  at: new Date().toISOString(),
  setId,
  nextAction: next,
  terminal,
  hasArchitectureDecisionRecorded: types.includes('ArchitectureDecisionRecorded'),
  hasClarificationAnswered: types.includes('ClarificationAnswered'),
  has03: names.includes('03-architecture.md'),
  deliverables: names,
  recentEvents: types.slice(0, 12),
};
writeEvidence('sg3-architect-343-after-clarification.json', evidence);
log('evidence', evidence);

if (!evidence.hasArchitectureDecisionRecorded && !evidence.has03) {
  process.exitCode = 1;
  log('SG3 FAIL');
} else {
  log('SG3 architect OK');
}
