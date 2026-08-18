import { runSqlQuery } from './lib/jnpf-db.mjs';
import fs from 'node:fs';

const rows = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 6
  F_Id,
  ISNULL(F_SkillId,'') AS SkillId,
  ISNULL(F_PIPELINE_ID,'') AS PipelineId,
  ISNULL(F_ACTUAL_MODEL, ISNULL(F_MODEL,'')) AS Model,
  F_PROMPT_TOKENS AS Tin,
  F_COMPLETION_TOKENS AS Tout,
  F_LATENCY_MS AS Ms,
  F_STATUS_CODE AS Http,
  LEN(F_RESPONSE_BODY) AS RespLen,
  LEFT(F_RESPONSE_BODY, 500) AS RespHead,
  F_CREATOR_TIME AS Ts
FROM BASE_AI_CALL_LOG
WHERE F_PIPELINE_ID='355' OR F_SkillId='pm-skill' AND F_CREATOR_TIME >= '2026-07-12 01:20:00'
ORDER BY F_CREATOR_TIME DESC;
`);
console.log(rows);

// dump full responses for analysis
const full = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 3 F_Id, F_COMPLETION_TOKENS, LEN(F_RESPONSE_BODY) AS L, F_RESPONSE_BODY
FROM BASE_AI_CALL_LOG
WHERE F_PIPELINE_ID='355'
ORDER BY F_CREATOR_TIME DESC;
`);
fs.writeFileSync('scripts/_pm355-responses.txt', String(full), 'utf8');
console.log('\nWrote scripts/_pm355-responses.txt len', String(full).length);
