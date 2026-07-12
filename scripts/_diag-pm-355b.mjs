import { runSqlQuery } from './lib/jnpf-db.mjs';

const cols = await runSqlQuery(`
SELECT c.name, t.name, c.max_length
FROM sys.columns c
JOIN sys.types t ON c.user_type_id=t.user_type_id
WHERE c.object_id=OBJECT_ID('ai_skill_runs')
ORDER BY c.column_id;
`);
console.log('=== ai_skill_runs cols ===\n', cols);

const rows = await runSqlQuery(`
SELECT TOP 3 *
FROM ai_skill_runs
WHERE CAST(F_PIPELINE_ID AS NVARCHAR(50))='355'
ORDER BY 1 DESC;
`);
console.log('\n=== rows ===\n', String(rows).slice(0, 4000));
