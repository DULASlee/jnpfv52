import { runSqlFile, runSqlQuery } from './lib/jnpf-db.mjs';

const files = [
  'backend/modularity/inteAssistant/Migrations/20260705_SA_pipeline_work_mode.sql',
  'backend/modularity/inteAssistant/Migrations/20260705_SA_deliverable.sql',
];

for (const f of files) {
  console.log('Applying', f);
  runSqlFile(f);
  console.log('OK', f);
}

const cols = runSqlQuery(
  "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND (name LIKE 'F_%MODE%' OR name LIKE 'F_TARGET%' OR name LIKE 'F_SOURCE%')",
);
console.log('WorkMode columns:', cols);

const tbl = runSqlQuery("SELECT name FROM sys.tables WHERE name = 'inte_assistant_deliverable'");
console.log('Deliverable table:', tbl);
