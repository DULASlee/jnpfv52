import { runSqlQuery } from './lib/jnpf-db.mjs';

const out = runSqlQuery(`
SET NOCOUNT ON;
SELECT TOP 8 F_ProviderCode, F_Model, F_Status, LEFT(F_ErrorMessage,120) AS err, LEFT(F_ResponseBody,300) AS resp
FROM ai_call_log
WHERE F_ProjectId = '301'
ORDER BY F_CreatorTime DESC
`);
console.log(out);
