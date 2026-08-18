import { login, apiRequest } from './lib/jnpf-auth.mjs';
import { getEvents, getDeliverables, writeEvidence, log } from './lib/phase-sup-api.mjs';
import { runSqlQuery } from './lib/jnpf-db.mjs';

const id = 343;
const session = await login();

const items = await getDeliverables(session, id);
const names = items.map((i) => i.name || i.Name || i.fileName || i.FileName);
log('deliverables', names.join(', '));

const contentRes = await apiRequest(
  'GET',
  `/api/studio/pipeline/execute/${id}/deliverables/content?relativePath=${encodeURIComponent('02-requirement-spec.md')}`,
  { session },
);
const body = contentRes?.data?.data ?? contentRes?.data ?? contentRes;
const text = typeof body === 'string' ? body : body?.content || body?.Content || JSON.stringify(body);
const head = String(text).slice(0, 1200);
log('02 head:\n', head);

const headings = [...String(text).matchAll(/^#{1,3}\s+.+$/gm)].map((m) => m[0]).slice(0, 40);
log('02 headings', headings.join(' | '));

const dddHits = ['限界上下文', '聚合', '领域', 'DDD', '子域', '通用语言'].filter((k) =>
  String(text).includes(k),
);
log('DDD keywords in 02', dddHits.join(', ') || '(none)');

const sql = runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 3 validation_status, event_count FROM sa_scope WHERE CAST(pipeline_id AS NVARCHAR(50))='343' ORDER BY id DESC;
SELECT TOP 3 CAST(fk_in_dict AS INT) fk, CAST(third_normal_form AS INT) n3, validation_status FROM sa_er WHERE CAST(pipeline_id AS NVARCHAR(50))='343';
SELECT TOP 8 F_EntityName AS ent, F_FieldName AS fld, LEFT(ISNULL(F_ProjectionHash,''),12) AS hash
FROM ai_entity_field WHERE F_PIPELINE_ID='343' AND F_DeleteMark=0 ORDER BY F_EntityName, F_FieldName;
SELECT COUNT(*) AS sa_ddd_tables FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'sa_ddd%';
`);
log('sql:\n' + sql);

const events = await getEvents(session, id);
const types = [...new Set(events.map((e) => e.eventType || e.EventType))];
const path = writeEvidence('sg2-deep-343.json', {
  pipelineId: id,
  at: new Date().toISOString(),
  deliverables: names,
  dddKeywordsIn02: dddHits,
  headings02: headings,
  eventTypes: types,
  hasAnalysisCompleted: types.includes('AnalysisCompleted'),
  hasMaterialized: types.includes('SaMaterializationCompleted'),
  sqlSnippet: String(sql).slice(0, 2000),
});
log('evidence', path);
