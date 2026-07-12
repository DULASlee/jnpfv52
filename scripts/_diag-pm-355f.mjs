import { runSqlQuery } from './lib/jnpf-db.mjs';
import fs from 'node:fs';

const rows = await runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 4
  CAST(F_Id AS VARCHAR(30)) AS Id,
  F_PROMPT_TOKENS AS Tin,
  F_COMPLETION_TOKENS AS Tout,
  LEN(ISNULL(F_RESPONSE_BODY,'')) AS RespLen,
  RIGHT(ISNULL(F_RESPONSE_BODY,''), 200) AS Tail,
  LEFT(ISNULL(F_RESPONSE_BODY,''), 120) AS Head
FROM BASE_AI_CALL_LOG
WHERE F_CREATOR_TIME >= '2026-07-12 09:28:00'
  AND F_CREATOR_TIME < '2026-07-12 09:30:00'
ORDER BY F_CREATOR_TIME ASC;
`);
fs.writeFileSync('scripts/_pm355-tails.txt', String(rows), 'utf8');
console.log(rows);
