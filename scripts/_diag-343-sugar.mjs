import { login, apiRequest, jnpfData, pick } from './lib/jnpf-auth.mjs';
import { getEvents } from './lib/phase-sup-api.mjs';
import { runSqlQuery } from './lib/jnpf-db.mjs';

const session = await login();
const events = await getEvents(session, 343);
const fail = events.filter(e => (e.eventType || e.EventType) === 'SkillFailureRecorded');
for (const f of fail.slice(0, 3)) {
  console.log('---');
  console.log((f.payloadPreview || f.PayloadPreview || JSON.stringify(f)).slice(0, 1500));
}

// also try get run detail if any
const runs = await apiRequest('GET', '/api/studio/skills/343/runs', { session });
const list = jnpfData(runs) || [];
const latest = list.find(r => pick(r, 'skillId', 'SkillId') === 'analyst-skill');
console.log('\nlatest analyst:', JSON.stringify(latest, null, 2)?.slice(0, 1000));

const sa = runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 5 id, pipeline_id, validation_status, event_count FROM dbo.sa_scope
WHERE CAST(pipeline_id AS NVARCHAR(50))='343' OR CAST(project_id AS NVARCHAR(50))='343';
SELECT TOP 3 name FROM sys.tables WHERE name LIKE 'ai_entity%' OR name LIKE 'sa_%';
`);
console.log('\nDB:\n', sa);
