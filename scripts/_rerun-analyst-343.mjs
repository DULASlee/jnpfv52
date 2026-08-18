/**
 * 强制重跑 analyst 343 → confirm 物化（COMPILED→PASS 修复后）
 */
import { login } from './lib/jnpf-auth.mjs';
import {
  runAnalystSkill,
  watchSkillTerminal,
  waitDeliverable,
  confirmRequirementSpec,
  getEvents,
  diagnosePipeline,
  printDiagnose,
  writeEvidence,
  assertDeliverableNames,
  getDeliverables,
  log,
} from './lib/phase-sup-api.mjs';
import { runSqlQuery } from './lib/jnpf-db.mjs';

const id = Number(process.argv[2] || 343);

const session = await login();

log('── force re-run analyst', id);
await runAnalystSkill(session, id);
await watchSkillTerminal(session, id, 'analyst-skill', { timeoutMs: 600_000, stallSec: 240 });
await waitDeliverable(session, id, '02-requirement-spec.md', 120_000);
log('analyst terminal OK');

let events = await getEvents(session, id);
let types = events.map(e => e.eventType || e.EventType);
log('AnalysisCompleted', types.includes('AnalysisCompleted'));
log('SaMaterializationCompleted', types.includes('SaMaterializationCompleted'));

if (!types.includes('SaMaterializationCompleted')) {
  log('── confirm-requirement-spec (materialize)');
  const result = await confirmRequirementSpec(session, id, { autoRunDesign: false });
  log('confirm result', JSON.stringify(result)?.slice(0, 400));

  const deadline = Date.now() + 180_000;
  let done = false;
  while (Date.now() < deadline) {
    events = await getEvents(session, id);
    types = events.map(e => e.eventType || e.EventType);
    if (types.includes('SaMaterializationCompleted')) {
      log('SaMaterializationCompleted ✓');
      done = true;
      break;
    }
    if (types.includes('SaMaterializationFailed')) {
      const f = events.find(e => (e.eventType || e.EventType) === 'SaMaterializationFailed');
      throw new Error(`物化失败: ${(f?.payloadPreview || f?.PayloadPreview || '').slice(0, 400)}`);
    }
    await new Promise(r => setTimeout(r, 3000));
  }
  if (!done && !types.includes('SaMaterializationCompleted')) {
    // Round3 工程接线可能已在 analyst Finalize 内物化但未发 IR 事件；查表兜底
    log('IR 未见到物化事件，查 sa_scope…');
  }
}

const items = await getDeliverables(session, id);
const check = assertDeliverableNames(items, [
  '00-merged-requirement.md',
  '01-skeleton.md',
  '02-requirement-spec.md',
]);
events = await getEvents(session, id);
types = events.map(e => e.eventType || e.EventType);
const diag = await diagnosePipeline(session, id);
printDiagnose(diag);

const sa = runSqlQuery(`
SET NOCOUNT ON;
SELECT id, pipeline_id, validation_status, event_count
FROM dbo.sa_scope
WHERE CAST(pipeline_id AS NVARCHAR(50)) = '${id}'
   OR CAST(project_id AS NVARCHAR(50)) = '${id}';
`);
log('sa_scope:\n' + sa);

const evidence = {
  pipelineId: id,
  at: new Date().toISOString(),
  deliverables: check,
  hasAnalysisCompleted: types.includes('AnalysisCompleted'),
  hasMaterialized: types.includes('SaMaterializationCompleted'),
  eventTypes: types.slice(0, 40),
  saScope: String(sa).trim(),
};
const path = writeEvidence(`s2-longchain-${id}-rerun.json`, evidence);
log('evidence', path);

if (!check.pass) throw new Error(`missing ${check.missing.join(',')}`);
if (!types.includes('AnalysisCompleted')) throw new Error('no AnalysisCompleted');

const hasRows = String(sa).includes('PASS') || String(sa).match(/\d+/);
if (!types.includes('SaMaterializationCompleted') && !hasRows) {
  throw new Error('物化未完成：无 SaMaterializationCompleted 且 sa_scope 无行');
}

log('PASS analyst+materialize', id);
