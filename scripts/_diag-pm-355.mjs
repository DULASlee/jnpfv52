import { login, apiRequest, jnpfData, pick } from './lib/jnpf-auth.mjs';
import { runSqlQuery } from './lib/jnpf-db.mjs';

await login();
const pid = 355;

const runsRaw = jnpfData(await apiRequest('GET', `/api/studio/skills/${pid}/runs`)) || [];
const list = Array.isArray(runsRaw) ? runsRaw : (runsRaw.items || runsRaw.Items || []);
console.log('=== skill runs ===', list.length);
for (const r of list.slice(0, 10)) {
  console.log(JSON.stringify({
    skillId: pick(r, 'skillId', 'SkillId'),
    status: pick(r, 'status', 'Status'),
    error: String(pick(r, 'errorMessage', 'ErrorMessage', 'message', 'Message', 'error', 'Error') || '').slice(0, 300),
    runId: pick(r, 'runId', 'RunId', 'id', 'Id'),
  }));
}

const att = await runSqlQuery(`
SELECT TOP 5 F_Id, F_FileName, F_ProcessStatus, LEN(F_ExtractedText) AS extLen, F_ProcessError
FROM inte_assistant_attachment
WHERE F_PipelineId='355'
ORDER BY F_CreatorTime DESC;
`);
console.log('\n=== attachments ===\n', att);

const pm = await runSqlQuery(`
SELECT TOP 5 F_ID, F_SKILL_ID, F_STATUS, F_ERROR_MESSAGE, F_STARTED_AT, F_FINISHED_AT
FROM ai_skill_runs
WHERE F_PIPELINE_ID='355' OR F_PIPELINE_ID=355
ORDER BY F_STARTED_AT DESC;
`);
console.log('\n=== ai_skill_runs ===\n', pm);
