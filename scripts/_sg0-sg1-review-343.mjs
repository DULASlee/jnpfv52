import { login, apiRequest, pick } from './lib/jnpf-auth.mjs';
import { getEvents, getDeliverables, writeEvidence, log } from './lib/phase-sup-api.mjs';
import { runSqlQuery } from './lib/jnpf-db.mjs';

const id = 343;
const session = await login();

const items = await getDeliverables(session, id);
const names = items.map((i) => i.name || i.Name || i.fileName || i.FileName);

async function content(path) {
  const res = await apiRequest(
    'GET',
    `/api/studio/pipeline/execute/${id}/deliverables/content?relativePath=${encodeURIComponent(path)}`,
    { session },
  );
  const body = res?.data?.data ?? res?.data ?? res;
  const text = typeof body === 'string' ? body : body?.content || body?.json || body?.Content || JSON.stringify(body);
  return String(text);
}

const gate = await content('00-gate-report.json');
const merged = await content('00-merged-requirement.md');
const skeleton = await content('01-skeleton.md');

let gatePassed = false;
try {
  const g = JSON.parse(gate.includes('{') ? gate.slice(gate.indexOf('{')) : gate);
  gatePassed = !!(g.passed ?? g.Passed ?? g.ok);
} catch {
  gatePassed = /passed["\s:]*true/i.test(gate);
}

const statusRes = await apiRequest('GET', `/api/studio/skills/design/${id}/status`, { session });
const status = pick(statusRes?.data?.data ?? statusRes?.data ?? statusRes);

const events = await getEvents(session, id);
const types = events.map((e) => e.eventType || e.EventType);
const hasProjectCreated = types.includes('ProjectCreated') || types.some((t) => /ProjectCreated|Gate|Maturity/i.test(t));
const hasSkeleton = types.includes('SkeletonCreated') || names.includes('01-skeleton.md');

const sql = runSqlQuery(`
SET NOCOUNT ON;
SELECT COUNT(*) AS field_cnt FROM ai_entity_field WHERE F_PIPELINE_ID='343' AND F_DeleteMark=0;
`);

const report = {
  pipelineId: id,
  at: new Date().toISOString(),
  sg0: {
    deliverables: names.filter((n) => String(n).startsWith('00')),
    gatePassed,
    mergedLen: merged.length,
    mergedHasLeave: /请假|加班|leave/i.test(merged),
  },
  sg1: {
    hasSkeletonMd: names.includes('01-skeleton.md'),
    skeletonLen: skeleton.length,
    skeletonHasEvents: /businessEvents|业务事件|EV-/i.test(skeleton),
    hasSkeletonIr: hasSkeleton,
  },
  designStatus: {
    canRunDesign: status?.CanRunDesign ?? status?.canRunDesign,
    finalized: status?.AnalysisFinalized ?? status?.analysisFinalized,
    entityFieldCount: status?.EntityFieldCount ?? status?.entityFieldCount,
  },
  sql,
  hasProjectCreatedHint: hasProjectCreated,
};

log(JSON.stringify(report, null, 2));
writeEvidence('sg0-sg1-review-343.json', report);
log('SG0/SG1 review done');
