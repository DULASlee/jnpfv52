import { login } from './lib/jnpf-auth.mjs';
import { getEvents, diagnosePipeline, printDiagnose, getDeliverables, assertDeliverableNames, writeEvidence, log } from './lib/phase-sup-api.mjs';
import { runSqlQuery } from './lib/jnpf-db.mjs';

const id = 343;
const session = await login();

const deadline = Date.now() + 120_000;
while (Date.now() < deadline) {
  const events = await getEvents(session, id);
  const types = events.map(e => e.eventType || e.EventType);
  if (types.includes('SaMaterializationCompleted')) {
    log('SaMaterializationCompleted ✓');
    break;
  }
  if (types.includes('SaMaterializationFailed')) {
    const f = events.find(e => (e.eventType || e.EventType) === 'SaMaterializationFailed');
    throw new Error('物化失败: ' + (f?.payloadPreview || f?.PayloadPreview || '').slice(0, 400));
  }
  log('waiting materialization…');
  await new Promise(r => setTimeout(r, 3000));
}

const events = await getEvents(session, id);
const types = events.map(e => e.eventType || e.EventType);
const items = await getDeliverables(session, id);
const check = assertDeliverableNames(items, [
  '00-merged-requirement.md',
  '01-skeleton.md',
  '02-requirement-spec.md',
]);
const diag = await diagnosePipeline(session, id);
printDiagnose(diag);

const sa = runSqlQuery(`
SET NOCOUNT ON;
SELECT 'sa_scope' t, COUNT(*) c FROM sa_scope WHERE CAST(pipeline_id AS NVARCHAR(50))='343'
UNION ALL SELECT 'sa_dfd', COUNT(*) FROM sa_dfd WHERE CAST(pipeline_id AS NVARCHAR(50))='343'
UNION ALL SELECT 'sa_er', COUNT(*) FROM sa_er WHERE CAST(pipeline_id AS NVARCHAR(50))='343'
UNION ALL SELECT 'sa_ui', COUNT(*) FROM sa_ui WHERE CAST(pipeline_id AS NVARCHAR(50))='343'
UNION ALL SELECT 'ai_entity_field', COUNT(*) FROM ai_entity_field WHERE F_PIPELINE_ID='343' AND F_DeleteMark=0;
`);
log('counts:\n' + sa);

const evidence = {
  pipelineId: id,
  at: new Date().toISOString(),
  deliverables: check,
  hasAnalysisCompleted: types.includes('AnalysisCompleted'),
  hasMaterialized: types.includes('SaMaterializationCompleted'),
  eventTypes: types.slice(0, 50),
};
const path = writeEvidence('s2-longchain-343-final.json', evidence);
log('evidence', path);
log('PASS?', check.pass && types.includes('AnalysisCompleted'));
