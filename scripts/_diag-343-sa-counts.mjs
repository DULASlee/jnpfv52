import { runSqlQuery } from './lib/jnpf-db.mjs';

const out = runSqlQuery(`
SET NOCOUNT ON;
SELECT 'sa_scope' AS t, COUNT(*) AS c FROM sa_scope WHERE CAST(pipeline_id AS NVARCHAR(50))='343'
UNION ALL SELECT 'sa_dfd', COUNT(*) FROM sa_dfd WHERE CAST(pipeline_id AS NVARCHAR(50))='343'
UNION ALL SELECT 'sa_er', COUNT(*) FROM sa_er WHERE CAST(pipeline_id AS NVARCHAR(50))='343'
UNION ALL SELECT 'sa_ui', COUNT(*) FROM sa_ui WHERE CAST(pipeline_id AS NVARCHAR(50))='343'
UNION ALL SELECT 'ai_entity_field', COUNT(*) FROM ai_entity_field WHERE F_PIPELINE_ID='343' OR F_ProjectId='343';
SELECT TOP 3 id, validation_status, event_count, pipeline_id FROM sa_scope WHERE CAST(pipeline_id AS NVARCHAR(50))='343';
`);
console.log(out);
