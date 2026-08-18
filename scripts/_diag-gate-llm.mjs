import { runSqlQuery } from './lib/jnpf-db.mjs';
import { login, apiRequest, jnpfData } from './lib/jnpf-auth.mjs';

const recent = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 5 F_Id, F_ACTUAL_MODEL, F_STATUS_CODE, F_PROMPT_TOKENS, F_COMPLETION_TOKENS,
  F_LATENCY_MS, LEFT(ISNULL(F_ERROR_MESSAGE,''),200) AS Err,
  F_CREATOR_TIME
FROM BASE_AI_CALL_LOG
WHERE F_CREATOR_TIME >= DATEADD(MINUTE, -30, GETDATE())
ORDER BY F_CREATOR_TIME DESC;
`);
console.log('=== recent LLM calls ===\n', recent);

const gate = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 3 F_Id, F_SkillId, F_STATUS_CODE, F_LATENCY_MS,
  LEFT(ISNULL(F_ERROR_MESSAGE, ''), 300) AS Err,
  LEFT(ISNULL(F_RESPONSE_BODY,''), 200) AS Head,
  F_CREATOR_TIME
FROM BASE_AI_CALL_LOG
WHERE F_CREATOR_TIME >= DATEADD(HOUR, -2, GETDATE())
  AND (F_SkillId LIKE '%gate%' OR F_ERROR_MESSAGE LIKE '%GATE%' OR F_PROMPT_TOKENS < 500)
ORDER BY F_CREATOR_TIME DESC;
`);
console.log('\n=== gate-ish ===\n', gate);

const pipes = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 3 F_ID, F_NAME, F_CREATOR_TIME FROM BASE_AI_PIPELINE
ORDER BY F_CREATOR_TIME DESC;
`);
console.log('\n=== pipes ===\n', pipes);
