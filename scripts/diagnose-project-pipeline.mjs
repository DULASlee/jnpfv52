#!/usr/bin/env node
import { runSqlQuery } from './lib/jnpf-db.mjs';

console.log('=== 3. 同 projectId 下不同 pipelineId 的样本数（修正语法）===');
console.log(runSqlQuery(`
SET NOCOUNT ON;
SELECT F_ProjectId, COUNT(DISTINCT F_PIPELINE_ID) AS distinct_pipelines
FROM ai_ir_events
WHERE F_PIPELINE_ID IS NOT NULL AND F_PIPELINE_ID != ''
GROUP BY F_ProjectId
HAVING COUNT(DISTINCT F_PIPELINE_ID) > 1
ORDER BY distinct_pipelines DESC;
`));

console.log('\n=== 5b. 用户分布（BASE_AI_PIPELINE.F_CREATOR_USER_ID）===');
console.log(runSqlQuery(`
SET NOCOUNT ON;
SELECT F_CREATOR_USER_ID, F_TENANT_ID, COUNT(*) AS cnt
FROM BASE_AI_PIPELINE
WHERE F_DELETE_MARK = 0
GROUP BY F_CREATOR_USER_ID, F_TENANT_ID;
`));

console.log('\n=== 6. 唯一索引 — 看 projectId+pipelineId 是否有约束 ===');
console.log(runSqlQuery(`
SET NOCOUNT ON;
SELECT i.name AS index_name, COL_NAME(ic.object_id, ic.column_id) AS column_name
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id IN (OBJECT_ID('BASE_AI_PIPELINE'), OBJECT_ID('ai_ir_events'), OBJECT_ID('ai_ir_fragment_snapshots'))
  AND i.is_unique = 1;
`));

console.log('\n=== 7. 路径里到底有没有 projectId？看实际文件物理路径 ===');
console.log(runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 5 F_Id AS pipeline_id, F_PROJECT_ID, F_TENANT_ID, F_NAME FROM BASE_AI_PIPELINE WHERE F_DELETE_MARK = 0 ORDER BY CAST(F_Id AS BIGINT) DESC;
`));
