import { runSqlQuery } from './lib/jnpf-db.mjs';

const rows = runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 3
  F_COMPLETION_TOKENS AS tout,
  F_LATENCY_MS AS latency,
  RIGHT(ISNULL(F_RESPONSE_BODY,''), 400) AS respTail
FROM BASE_AI_CALL_LOG
WHERE F_PIPELINE_ID = '341' OR F_ProjectId = '341'
ORDER BY F_CREATOR_TIME DESC;
`);
console.log(rows);
