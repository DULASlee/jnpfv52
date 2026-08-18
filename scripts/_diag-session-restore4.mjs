import { runSqlQuery } from './lib/jnpf-db.mjs';

const att = await runSqlQuery(`
SET NOCOUNT ON;
SELECT F_Id, F_PipelineId, F_FileName, F_ProcessStatus,
  LEN(ISNULL(F_ExtractedText,'')) AS ExtLen, F_FileUrl
FROM inte_assistant_attachment
WHERE F_PipelineId IN ('355','356','354','353')
  AND (F_DeleteMark IS NULL OR F_DeleteMark=0);
`);
console.log('attachments\n', att);

const snapCols = await runSqlQuery(`
SET NOCOUNT ON;
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME='ai_ir_fragment_snapshots' ORDER BY ORDINAL_POSITION;
`);
console.log('\nsnap cols\n', snapCols);

const snaps = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 15 F_FragmentId, F_FragmentType, F_StabilityState, F_Version, F_PIPELINE_ID
FROM ai_ir_fragment_snapshots
WHERE F_PIPELINE_ID IN ('355','356')
ORDER BY F_PIPELINE_ID, F_Version DESC;
`);
console.log('\nsnaps\n', snaps);

const delCols = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%DELIVER%' OR TABLE_NAME LIKE '%GENERATED%' OR TABLE_NAME LIKE '%studio%';
`);
console.log('\ntables\n', delCols);
