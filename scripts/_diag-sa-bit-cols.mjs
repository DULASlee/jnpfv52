import { runSqlQuery } from './lib/jnpf-db.mjs';

const tables = [
  'sa_scope', 'sa_dfd', 'sa_business_process', 'sa_data_dictionary',
  'sa_er', 'sa_state_machine', 'sa_pspec', 'sa_decision_table', 'sa_ui',
];

for (const t of tables) {
  const cols = runSqlQuery(`
SET NOCOUNT ON;
SELECT c.name AS col, ty.name AS typ, c.max_length, c.is_nullable
FROM sys.columns c
JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.${t}')
  AND (ty.name = 'bit' OR c.name LIKE '%status%' OR c.name LIKE '%check%'
       OR c.name LIKE '%pass%' OR c.name LIKE '%dict%' OR c.name LIKE '%form%'
       OR c.name LIKE '%mapping%' OR c.name LIKE '%column%')
ORDER BY c.column_id;
`);
  console.log(`\n=== ${t} ===\n${cols}`);
}
