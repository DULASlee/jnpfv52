import { login, apiRequest, jnpfData, pick } from './lib/jnpf-auth.mjs';
import {
  getEvents,
  getDeliverables,
  getSkillRuns,
  diagnosePipeline,
  printDiagnose,
  confirmRequirementSpec,
} from './lib/phase-sup-api.mjs';
import { runSqlQuery } from './lib/jnpf-db.mjs';

const session = await login();
const id = 343;

const diag = await diagnosePipeline(session, id);
printDiagnose(diag);

const events = await getEvents(session, id);
const types = events.map(e => e.eventType || e.EventType);
console.log('\nkey events:', {
  AnalysisCompleted: types.includes('AnalysisCompleted'),
  SaMaterializationCompleted: types.includes('SaMaterializationCompleted'),
  SaMaterializationFailed: types.includes('SaMaterializationFailed'),
  ClarificationRequested: types.filter(t => t === 'ClarificationRequested').length,
  ClarificationAnswered: types.filter(t => t === 'ClarificationAnswered').length,
  SkillFailureRecorded: types.filter(t => t === 'SkillFailureRecorded').length,
});

const clar = events.find(e => (e.eventType || e.EventType) === 'ClarificationRequested');
if (clar) {
  console.log('\nClarificationRequested preview:', (clar.payloadPreview || clar.PayloadPreview || '').slice(0, 500));
}

const fail = events.find(e => (e.eventType || e.EventType) === 'SkillFailureRecorded');
if (fail) {
  console.log('\nSkillFailure preview:', (fail.payloadPreview || fail.PayloadPreview || '').slice(0, 400));
}

const runs = await getSkillRuns(session, id);
for (const r of runs.filter(x => pick(x, 'skillId', 'SkillId') === 'analyst-skill')) {
  console.log('analyst run:', {
    status: pick(r, 'status', 'Status'),
    error: pick(r, 'errorMessage', 'ErrorMessage'),
    started: pick(r, 'startedAt', 'StartedAt'),
    completed: pick(r, 'completedAt', 'CompletedAt'),
  });
}

const sa = runSqlQuery(`
SET NOCOUNT ON;
SELECT id, pipeline_id, validation_status, event_count, created_by
FROM dbo.sa_scope
WHERE CAST(pipeline_id AS NVARCHAR(50)) = '343' OR CAST(project_id AS NVARCHAR(50)) = '343';
`);
console.log('\nsa_scope 343:\n', sa);
