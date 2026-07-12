import { login, apiRequest } from './lib/jnpf-auth.mjs';
import {
  getDesignStatus,
  runDesignOrchestrator,
  waitSkillTerminal,
  getDeliverables,
  getEvents,
  writeEvidence,
  log,
} from './lib/phase-sup-api.mjs';

const id = Number(process.env.E2E_PIPELINE_ID || 343);
const session = await login();

const before = await getDesignStatus(session, id);
log('design status before', {
  canRunDesign: before.CanRunDesign ?? before.canRunDesign,
  finalized: before.AnalysisFinalized ?? before.analysisFinalized,
  fields: before.EntityFieldCount ?? before.entityFieldCount,
});

if (!(before.CanRunDesign ?? before.canRunDesign)) {
  throw new Error('canRunDesign=false — 禁止进 SG3');
}

// 仅启动 architect（编排器会连跑 db/ui/system，耗时长；SG3 先验收架构）
const res = await apiRequest('POST', `/api/studio/skills/architect/${id}/run`, {
  body: {},
  session,
});
log('architect run response', JSON.stringify(res.json ?? res).slice(0, 600));

const terminal = await waitSkillTerminal(session, id, 'architect-skill', 600_000);
log('architect terminal', terminal);

const items = await getDeliverables(session, id);
const names = items.map((i) => i.name || i.Name || i.fileName || i.FileName);
const events = await getEvents(session, id);
const types = events.map((e) => e.eventType || e.EventType);
const hasArch = types.includes('ArchitectureDecisionRecorded');
const has03 = names.includes('03-architecture.md');

const evidence = {
  pipelineId: id,
  at: new Date().toISOString(),
  terminal,
  hasArchitectureDecisionRecorded: hasArch,
  has03,
  deliverables: names,
};
writeEvidence('sg3-architect-343.json', evidence);
log('SG3 architect evidence', evidence);

if (!hasArch && !has03) {
  process.exitCode = 1;
  log('SG3 FAIL: 无 ArchitectureDecisionRecorded 且无 03');
} else {
  log('SG3 architect path OK');
}
