import { runSqlQuery } from './lib/jnpf-db.mjs';
import fs from 'node:fs';

const recent = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 10
  F_Id,
  ISNULL(F_SkillId,'') AS SkillId,
  ISNULL(F_PIPELINE_ID,'') AS Pid,
  ISNULL(F_ACTUAL_MODEL, ISNULL(F_MODEL,'')) AS Model,
  F_PROMPT_TOKENS AS Tin,
  F_COMPLETION_TOKENS AS Tout,
  F_LATENCY_MS AS Ms,
  F_STATUS_CODE AS Http,
  LEN(ISNULL(F_RESPONSE_BODY,'')) AS RespLen,
  F_CREATOR_TIME AS Ts
FROM BASE_AI_CALL_LOG
WHERE F_CREATOR_TIME >= DATEADD(HOUR, -3, GETDATE())
ORDER BY F_CREATOR_TIME DESC;
`);
console.log('=== recent 3h ===\n', recent);

const bySkill = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 10
  F_Id, ISNULL(F_SkillId,'') AS SkillId, ISNULL(F_PIPELINE_ID,'') AS Pid,
  F_PROMPT_TOKENS AS Tin, F_COMPLETION_TOKENS AS Tout,
  LEN(ISNULL(F_RESPONSE_BODY,'')) AS RespLen,
  LEFT(ISNULL(F_RESPONSE_BODY,''), 300) AS Head,
  F_CREATOR_TIME AS Ts
FROM BASE_AI_CALL_LOG
WHERE F_SkillId='pm-skill'
ORDER BY F_CREATOR_TIME DESC;
`);
console.log('\n=== pm-skill ===\n', bySkill);
fs.writeFileSync('scripts/_pm355-responses.txt', String(bySkill), 'utf8');
