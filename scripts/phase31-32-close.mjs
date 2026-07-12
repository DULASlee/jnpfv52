#!/usr/bin/env node
/**
 * 31/32 业务闭环验收：ForceRefinalize 回填 + fork Amend + GoldenSet 信号采集
 * 用法: node scripts/phase31-32-close.mjs [sourcePipelineId=343]
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login, apiRequest, jnpfData } from './lib/jnpf-auth.mjs';
import { getDeliverables, writeEvidence, log } from './lib/phase-sup-api.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const sourceId = Number(process.argv[2] || process.env.E2E_PIPELINE_ID || 343);
const CTA =
  '请你确认需求分析说明书，如果同意，推进到下一工作阶段，如果不满意，请在输入框继续提出你的问题和要求。';

function unwrap(res) {
  const body = res.json;
  // JNPF：HTTP 200 + code 200/0 均视为成功；兼容 data 直接返回
  const code = body?.code ?? body?.Code;
  if (code != null && code !== 200 && code !== 0) {
    throw new Error(`API fail ${res.status}: ${JSON.stringify(body).slice(0, 500)}`);
  }
  return jnpfData(body) ?? body?.data ?? body?.Data ?? body;
}

async function getSpecMarkdown(session, pipelineId, relativePath = '02-requirement-spec.md') {
  const pathOnly = String(relativePath).replace(/^deliverables\//, '');
  const res = await apiRequest(
    'GET',
    `/api/studio/pipeline/execute/${pipelineId}/deliverables/content?relativePath=${encodeURIComponent(pathOnly)}`,
    { session },
  );
  if (typeof res.json === 'string' && res.json.includes('需求分析')) return res.json;
  const data = unwrap(res);
  if (typeof data === 'string') return data;
  return data?.content || data?.Content || data?.text || data?.Text || '';
}

async function listEvents(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
  const data = jnpfData(res) ?? res.json?.data ?? res.json;
  return Array.isArray(data) ? data : (data?.items || data?.Items || []);
}

function eventTypes(events) {
  return events.map(e => e.eventType || e.EventType);
}

async function waitForEvent(session, pipelineId, type, timeoutMs = 600_000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const events = await listEvents(session, pipelineId);
    if (eventTypes(events).includes(type)) return events;
    await new Promise(r => setTimeout(r, 4000));
  }
  throw new Error(`timeout waiting ${type} on pipeline ${pipelineId}`);
}

async function main() {
  const session = await login();
  const report = {
    at: new Date().toISOString(),
    sourcePipelineId: sourceId,
    steps: {},
  };

  // ── 1) 343 ForceRefinalize：回填 PmReviewed + 重生 02（含 CTA/附录）──
  log('step1 ForceRefinalize', sourceId);
  const existingEvents = await listEvents(session, sourceId);
  const alreadyReviewed = eventTypes(existingEvents).includes('RequirementSpecPmReviewed');
  let mdProbe = '';
  try {
    mdProbe = await getSpecMarkdown(session, sourceId, '02-requirement-spec.md');
  } catch { /* ignore */ }
  const alreadyHasCta = mdProbe.includes(CTA);

  if (!(alreadyReviewed && alreadyHasCta)) {
    const runRes = await apiRequest('POST', `/api/studio/skills/requirement-analysis/${sourceId}/run`, {
      session,
      body: { forceRefinalize: true },
    });
    unwrap(runRes);
    await waitForEvent(session, sourceId, 'RequirementSpecPmReviewed', 600_000);
  } else {
    log('step1 skip', 'PmReviewed+CTA already present');
  }

  const afterRefinalize = await listEvents(session, sourceId);
  const types1 = eventTypes(afterRefinalize);
  const pmEvents = afterRefinalize.filter(e => (e.eventType || e.EventType) === 'RequirementSpecPmReviewed');
  let latestPm = null;
  try {
    const raw = pmEvents.at(-1)?.payloadPreview || pmEvents.at(-1)?.PayloadPreview || '{}';
    latestPm = JSON.parse(raw);
  } catch { /* ignore */ }

  const items = await getDeliverables(session, sourceId);
  const spec = items.find(d => (d.fileName || d.FileName || d.name || '').includes('02-requirement-spec'));
  const rel = spec?.relativePath || spec?.RelativePath || '02-requirement-spec.md';
  const md = await getSpecMarkdown(session, sourceId, rel);
  report.steps.sourceAfterRefinalize = {
    hasPmReviewed: types1.includes('RequirementSpecPmReviewed'),
    pmScore: latestPm?.score ?? null,
    pmGaps: latestPm?.gaps ?? [],
    hasCta: md.includes(CTA),
    hasOutOfScope: md.includes('非目标') || md.includes('Out of Scope'),
    hasAcceptance: md.includes('验收要点'),
    hasAppendixE: md.includes('附录 E'),
    docChars: md.length,
  };
  if (!report.steps.sourceAfterRefinalize.hasPmReviewed) throw new Error('343 缺少 RequirementSpecPmReviewed');
  if (!report.steps.sourceAfterRefinalize.hasCta) throw new Error('343 的 02 缺少固定 CTA');

  // ── 2) fork → Amend Propose/Apply（未 StageConfirmed，可硬需求）──
  log('step2 fork', sourceId);
  const forkRes = await apiRequest('POST', `/api/studio/pipeline/execute/${sourceId}/fork`, {
    session,
    body: { name: `31-32-close-fork-${Date.now()}`, workMode: 'enhancement' },
  });
  const fork = unwrap(forkRes);
  const forkId = Number(fork.pipelineId ?? fork.PipelineId);
  if (!forkId) throw new Error('fork 未返回 pipelineId');
  report.forkPipelineId = forkId;

  const hardReq =
    '请增加「代他人请假」能力：员工可为直属下属提交请假单；审批流增加部门经理与 HR 两级；' +
    '数据表需记录被代理人与代理人；驳回后允许修改再提交。';

  log('step3 amend propose', forkId);
  const proposeRes = await apiRequest('POST', `/api/studio/skills/requirement-analysis/${forkId}/amend/propose`, {
    session,
    body: { userMessage: hardReq },
  });
  const proposal = unwrap(proposeRes);
  const understanding = proposal.understanding || proposal.Understanding || {};
  const features = understanding.features || understanding.Features || [];
  const flows = understanding.flows || understanding.Flows || [];
  const entities = understanding.entitiesOrTables || understanding.EntitiesOrTables || [];
  report.steps.amendPropose = {
    proposalId: proposal.proposalId || proposal.ProposalId,
    featuresCount: features.length,
    flowsCount: flows.length,
    entitiesCount: entities.length,
    patchesCount: (understanding.patches || understanding.Patches || []).length,
    features,
    flows,
    entities,
  };
  if (!report.steps.amendPropose.proposalId) throw new Error('AmendPropose 无 proposalId');
  if (features.length + flows.length + entities.length === 0) {
    throw new Error('Amend 回显缺少功能/流程/实体 — 不符合企业可用');
  }

  log('step4 amend apply', forkId);
  const applyRes = await apiRequest('POST', `/api/studio/skills/requirement-analysis/${forkId}/amend/apply`, {
    session,
    body: {
      proposalId: report.steps.amendPropose.proposalId,
      understanding,
      userMessage: hardReq,
    },
  });
  const applied = unwrap(applyRes);
  report.steps.amendApply = applied;

  const afterApply = await waitForEvent(session, forkId, 'RequirementSpecPmReviewed', 600_000);
  const forkTypes = eventTypes(afterApply);
  const forkItems = await getDeliverables(session, forkId);
  const forkSpec = forkItems.find(d => (d.fileName || d.FileName || d.name || '').includes('02-requirement-spec'));
  const forkRel = forkSpec?.relativePath || forkSpec?.RelativePath || '02-requirement-spec.md';
  let forkMd = '';
  try {
    forkMd = await getSpecMarkdown(session, forkId, forkRel);
  } catch (e) {
    forkMd = String(e?.message || e);
  }
  report.steps.forkAfterApply = {
    hasAmendmentProposed: forkTypes.includes('RequirementAmendmentProposed'),
    hasAmendmentApplied: forkTypes.includes('RequirementAmendmentApplied'),
    hasPmReviewed: forkTypes.includes('RequirementSpecPmReviewed'),
    hasAnalysisCompleted: forkTypes.includes('AnalysisCompleted'),
    hasCta: forkMd.includes(CTA),
    docChars: forkMd.length,
  };
  if (!report.steps.forkAfterApply.hasAmendmentApplied) throw new Error('fork 缺少 RequirementAmendmentApplied');
  if (!report.steps.forkAfterApply.hasPmReviewed) throw new Error('fork Apply 后缺少 PM 终评');

  // ── 3) 进化信号采集 ──
  log('step5 collect-req-signals');
  const collectRes = await apiRequest('POST', '/api/studio/skill-memory/collect-req-signals?sinceDays=30', {
    session,
    body: {},
  });
  report.steps.collectReqSignals = unwrap(collectRes);

  writeEvidence('phase31-32-close.json', report);
  const out = path.join(__dirname, '../.claude/evidence/phase31-32-close.json');
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, JSON.stringify(report, null, 2));
  log('PASS', JSON.stringify(report.steps, null, 2));
  console.log(JSON.stringify({ ok: true, forkPipelineId: forkId, evidence: out }, null, 2));
}

main().catch(err => {
  console.error('[phase31-32-close] FAIL', err);
  process.exitCode = 1;
});
