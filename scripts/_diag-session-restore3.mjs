import { runSqlQuery } from './lib/jnpf-db.mjs';

const cols = await runSqlQuery(`
SET NOCOUNT ON;
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME='inte_assistant_attachment' ORDER BY ORDINAL_POSITION;
`);
console.log('att cols', cols);

const att = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 20 * FROM inte_assistant_attachment WHERE CAST(F_PIPELINE_ID AS NVARCHAR(50)) IN ('355','356','354','353');
`);
console.log('\natt rows', String(att).slice(0, 2000));

const msgHeads = await runSqlQuery(`
SET NOCOUNT ON;
SELECT F_ID, F_ROLE, F_STAGE, LEN(ISNULL(F_CONTENT,'')) AS Len,
  LEFT(ISNULL(F_CONTENT,''), 120) AS Head
FROM BASE_AI_PIPELINE_MESSAGE
WHERE F_PIPELINE_ID='355'
ORDER BY F_CREATOR_TIME, F_SEQUENCE;
`);
console.log('\n355 messages', msgHeads);

const snaps = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 10 F_FragmentId, F_FragmentType, F_StabilityState, F_Version
FROM ai_ir_fragment_snapshots
WHERE F_PIPELINE_ID='355' OR F_PipelineId='355';
`);
console.log('\n355 snaps', snaps);
