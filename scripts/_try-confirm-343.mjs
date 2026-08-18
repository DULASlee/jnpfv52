import { login, apiRequest, jnpfData, pick } from './lib/jnpf-auth.mjs';
import { getEvents } from './lib/phase-sup-api.mjs';

const session = await login();
const events = await getEvents(session, 343);
const fail = events.find(e => (e.eventType || e.EventType) === 'SkillFailureRecorded'
  && String(e.payloadPreview || e.PayloadPreview || '').includes('F_TenantId'));
console.log(JSON.stringify(fail, null, 2)?.slice(0, 2500));

// Try confirm-requirement-spec directly — materialize already done
const res = await apiRequest('POST', '/api/studio/skills/analyst/343/confirm-requirement-spec', {
  body: { autoRunDesign: false },
  session,
});
console.log('\nconfirm status', res.status, JSON.stringify(res.json)?.slice(0, 800));
