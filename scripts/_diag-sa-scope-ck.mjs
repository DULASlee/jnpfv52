import { runSqlQuery } from './lib/jnpf-db.mjs';

const ck = runSqlQuery(`
SET NOCOUNT ON;
SELECT name, definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('dbo.sa_scope');
`);
console.log('CHECK constraints:\n', ck);

const sample = runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 5 validation_status, COUNT(*) cnt
FROM dbo.sa_scope
GROUP BY validation_status;
`);
console.log('existing statuses:\n', sample);

const events = runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 15 F_EVENT_TYPE AS t, LEFT(ISNULL(F_PAYLOAD,''),200) AS p
FROM ai_ir_events
WHERE F_PIPELINE_ID = '343'
ORDER BY F_CREATOR_TIME DESC;
`);
console.log('IR 343:\n', events);
