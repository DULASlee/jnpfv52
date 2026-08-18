import { runSqlQuery } from './lib/jnpf-db.mjs';

// Find LLM call logs around the PM failure window
const cols = await runSqlQuery(`
SELECT TOP 1 TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%AI%CALL%' OR TABLE_NAME LIKE '%LLM%' OR TABLE_NAME LIKE '%ai_call%'
ORDER BY TABLE_NAME;
`);
console.log('tables hint', cols);

const tables = await runSqlQuery(`
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%CALL%' OR TABLE_NAME LIKE '%LLM%' OR TABLE_NAME LIKE '%PROMPT%' OR TABLE_NAME LIKE '%ai_%'
ORDER BY TABLE_NAME;
`);
console.log('\n=== tables ===\n', tables);
