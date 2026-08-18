import { runSqlQuery } from './lib/jnpf-db.mjs';

const tables = runSqlQuery(`
SET NOCOUNT ON;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%call%' OR TABLE_NAME LIKE '%llm%' OR TABLE_NAME LIKE '%AI_%LOG%';
`);
console.log('tables:\n', tables);

const cols = runSqlQuery(`
SET NOCOUNT ON;
SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('ai_call_log','AI_CALL_LOG','BASE_AI_CALL_LOG','ai_llm_call_log')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
`);
console.log('cols:\n', cols);

const rows = runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 12
  F_PROVIDER AS provider,
  F_MODEL AS model,
  F_STATUS_CODE AS statusCode,
  F_SkillId AS skillId,
  F_ProjectId AS projectId,
  F_PIPELINE_ID AS pipelineId,
  F_PROMPT_TOKENS AS tin,
  F_COMPLETION_TOKENS AS tout,
  F_LATENCY_MS AS latency,
  F_FALLBACK AS fallback,
  LEFT(ISNULL(F_FALLBACK_REASON,''),120) AS fallbackReason,
  LEFT(ISNULL(F_RESPONSE_BODY,''),500) AS resp
FROM BASE_AI_CALL_LOG
WHERE F_PIPELINE_ID IN ('341','309')
   OR F_ProjectId IN ('341','309')
   OR F_CREATOR_TIME > DATEADD(MINUTE,-45,SYSUTCDATETIME())
ORDER BY F_CREATOR_TIME DESC;
`);
console.log('recent calls:\n', rows);
