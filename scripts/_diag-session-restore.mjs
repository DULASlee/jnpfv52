import { runSqlQuery } from './lib/jnpf-db.mjs';

const pipelines = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 8
  F_ID AS Id,
  F_NAME AS Name,
  F_CURRENT_STAGE AS Stage,
  ISNULL(F_FROZEN,0) AS Frozen,
  LEN(ISNULL(F_CHECKPOINT,'')) AS CkLen,
  F_CREATOR_TIME AS Created
FROM BASE_AI_PIPELINE
WHERE F_DELETE_MARK IS NULL OR F_DELETE_MARK = 0
ORDER BY ISNULL(F_LAST_MODIFY_TIME, F_CREATOR_TIME) DESC;
`);
console.log('=== recent pipelines ===\n', pipelines);

const named = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 5 F_ID, F_NAME, F_CURRENT_STAGE, ISNULL(F_FROZEN,0) AS Frozen
FROM BASE_AI_PIPELINE
WHERE F_NAME LIKE N'%请为我%' OR F_NAME LIKE N'%更衣柜%' OR F_NAME LIKE N'%智能%';
`);
console.log('\n=== named ===\n', named);
