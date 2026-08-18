import { runSqlQuery } from './lib/jnpf-db.mjs';

for (const t of ['ai_entity_field', 'sa_quality_score', 'sa_consistency', 'sa_assumptions']) {
  const cols = runSqlQuery(`
SET NOCOUNT ON;
SELECT c.name FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.${t}')
ORDER BY c.column_id;
`);
  console.log(`\n=== ${t} ===\n${cols}`);
}
