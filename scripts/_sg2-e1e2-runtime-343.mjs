/**
 * SG2-E1/E2 运行时重验：重跑 analyst Finalize → 抽检 02 表头 + 待确认节
 */
import { login, apiRequest } from './lib/jnpf-auth.mjs';
import { runAnalystSkill, watchSkillTerminal, waitDeliverable, writeEvidence, log } from './lib/phase-sup-api.mjs';

const id = Number(process.argv[2] || 343);
const session = await login();

log('── analyst Finalize re-run', id);
await runAnalystSkill(session, id);
const terminal = await watchSkillTerminal(session, id, 'analyst-skill', {
  timeoutMs: 600_000,
  stallSec: 240,
});
log('terminal', terminal);
if (terminal.status === 'failed') {
  throw new Error(`analyst failed: ${terminal.error || JSON.stringify(terminal)}`);
}

await waitDeliverable(session, id, '02-requirement-spec.md', 120_000);

const contentRes = await apiRequest(
  'GET',
  `/api/studio/pipeline/execute/${id}/deliverables/content?relativePath=${encodeURIComponent('02-requirement-spec.md')}`,
  { session },
);
const raw = contentRes?.json ?? contentRes?.data ?? contentRes;
const text = String(
  typeof raw === 'string'
    ? raw
    : raw?.data ?? raw?.content ?? raw?.Content ?? raw?.json ?? '',
);

const nameMatch = text.match(/\|\s*项目名称\s*\|\s*([^|]+)\s*\|/);
const summaryMatch = text.match(/\|\s*需求概要\s*\|\s*([^|]+)\s*\|/);
const projectName = (nameMatch?.[1] || '').trim();
const summary = (summaryMatch?.[1] || '').trim();

const checks = {
  terminalStatus: terminal.status,
  docLen: text.length,
  projectName,
  summary: summary.slice(0, 100),
  noDashName: projectName !== '—' && projectName !== '-',
  noDashSummary: summary !== '—' && summary !== '-',
  notGenericBusinessOnly: projectName !== '业务',
  hasPendingSection: text.includes('待确认事项'),
  hasLeaveHint:
    /请假|加班|Leave|OT/i.test(projectName + summary + text.slice(0, 1500)),
};

checks.pass =
  checks.noDashName &&
  checks.noDashSummary &&
  checks.hasPendingSection &&
  checks.docLen > 500 &&
  (checks.notGenericBusinessOnly || checks.hasLeaveHint);

const path = writeEvidence('sg2-e1e2-runtime-343.json', {
  pipelineId: id,
  at: new Date().toISOString(),
  checks,
  coverHead: text.slice(0, 900),
});

log('checks', JSON.stringify(checks, null, 2));
log('evidence', path);
log('cover:\n' + text.slice(0, 700));

if (!checks.pass) process.exitCode = 1;
else log('SG2-E1/E2 runtime PASS');
