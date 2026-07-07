/**
 * SUP 系列 E2E 共享 API（22 号文档五步推进）
 * 设计原则：每步可单独调用；等待过程有心跳；failed 立即暴露；timeout 带诊断快照。
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { apiRequest, isJnpfOk, jnpfData, pick } from './jnpf-auth.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
export const REPO_ROOT = path.resolve(__dirname, '../..');
export const EVIDENCE_DIR = path.join(REPO_ROOT, '.claude', 'evidence');
export const STATE_FILE = path.join(REPO_ROOT, 'scripts', '.sup-e2e-state.json');

export const log = (...args) => console.log('[sup-e2e]', ...args);
export const warn = (...args) => console.warn('[sup-e2e:warn]', ...args);

const FAILURE_EVENT_TYPES = new Set([
  'SkillFailureRecorded',
  'DeploymentFailed',
  'CodegenFailed',
  'ArchViolationDetected',
]);

export function loadState() {
  try {
    if (fs.existsSync(STATE_FILE)) {
      return JSON.parse(fs.readFileSync(STATE_FILE, 'utf8'));
    }
  } catch {
    /* ignore */
  }
  return {};
}

export function saveState(patch) {
  const next = { ...loadState(), ...patch, updatedAt: new Date().toISOString() };
  fs.mkdirSync(path.dirname(STATE_FILE), { recursive: true });
  fs.writeFileSync(STATE_FILE, JSON.stringify(next, null, 2), 'utf8');
  return next;
}

export function resolvePipelineId(explicit) {
  const n = Number(explicit || loadState().pipelineId || process.env.E2E_PIPELINE_ID || 0);
  if (!n) throw new Error('缺少 pipelineId：先 step create，或 --pipeline-id N / E2E_PIPELINE_ID');
  return n;
}

export async function waitFor(fn, label, timeoutMs = 180_000, intervalMs = 2000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const hit = await fn();
    if (hit) return hit;
    await new Promise(r => setTimeout(r, intervalMs));
  }
  throw new Error(`timeout: ${label} (${Math.round(timeoutMs / 1000)}s)`);
}

export async function createPipeline(session, name, requirement) {
  const text = requirement || `${name}：员工请假审批系统，含请假申请、审批流、部门管理、统计报表。`.padEnd(420, '。');
  const res = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
    body: { name, requirement: text, userRequirement: text },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`create pipeline: ${JSON.stringify(res.json)}`);
  const pipelineId = pick(jnpfData(res), 'pipelineId', 'PipelineId');
  saveState({ pipelineId, name });
  return pipelineId;
}

export async function resolveTenantId(session) {
  if (session.tenantId) return session.tenantId;
  const res = await apiRequest('GET', '/api/oauth/CurrentUser', { session });
  const userInfo = jnpfData(res)?.userInfo ?? jnpfData(res);
  session.tenantId = pick(userInfo, 'tenantId', 'TenantId') || '0';
  return session.tenantId;
}

export async function uploadAnnexFile(session, filePath) {
  const buf = fs.readFileSync(filePath);
  const name = path.basename(filePath);
  const form = new FormData();
  form.append('file', new Blob([buf]), name);
  const tenantId = await resolveTenantId(session);
  const base = (session.apiUrl || 'http://localhost:5000').replace(/\/$/, '');
  const res = await fetch(`${base}/api/file/Uploader/annex`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${session.token}`,
      'jnpf-origin': 'pc',
      'X-Tenant-Id': tenantId,
    },
    body: form,
    signal: AbortSignal.timeout(120_000),
  });
  const json = await res.json().catch(() => ({}));
  if (!res.ok || json.code !== 200) {
    throw new Error(`upload annex: HTTP ${res.status} ${JSON.stringify(json)}`);
  }
  const data = json.data ?? json;
  const url = data.url ?? data.Url;
  if (!url) throw new Error(`upload annex: missing url in ${JSON.stringify(json)}`);
  return { name, url };
}

export async function triggerSaGate(session, pipelineId, { autoRunPm = true, userText, attachments } = {}) {
  const res = await apiRequest('POST', `/api/studio/pipeline/execute/${pipelineId}/sa-gate`, {
    body: {
      userText: userText || '员工请假审批：提交申请、主管审批、HR备案；角色：员工/主管/HR；需统计报表与年假余额。',
      autoRunPm,
      attachments: attachments?.map(a => ({ name: a.name, url: a.url })),
    },
    session,
  });
  if (!isJnpfOk(res) && res.status !== 200) {
    throw new Error(`sa-gate: ${JSON.stringify(res.json)}`);
  }
  return res;
}

export async function getDeliverables(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/pipeline/execute/${pipelineId}/deliverables`, { session });
  const data = jnpfData(res) || res.json;
  return data?.items || data?.Items || [];
}

export async function getEvents(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
  return Array.isArray(res.json) ? res.json : jnpfData(res) || [];
}

export async function getSnapshots(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/snapshots`, { session });
  const data = jnpfData(res);
  if (Array.isArray(data)) return data;
  if (Array.isArray(res.json)) return res.json;
  return [];
}

export async function getSkillRuns(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/skills/${pipelineId}/runs`, { session });
  return Array.isArray(res.json) ? res.json : jnpfData(res) || [];
}

export async function getLatestSkillRun(session, pipelineId, skillId) {
  const list = await getSkillRuns(session, pipelineId);
  const runs = list
    .filter(r => pick(r, 'skillId', 'SkillId') === skillId)
    .sort((a, b) => (pick(b, 'startedAt', 'StartedAt') || 0) - (pick(a, 'startedAt', 'StartedAt') || 0));
  return runs[0] || null;
}

/** analyst/pm 运行期间 IR 是否有进展 */
export async function countSkillIrProgress(session, pipelineId, skillId) {
  const events = await getEvents(session, pipelineId);
  const progressTypes = skillId === 'analyst-skill'
    ? ['SA_Step_Completed', 'EventSpecConfirmed', 'AnalysisCompleted', 'SaNineViewCompiled']
    : skillId === 'pm-skill'
      ? ['SkeletonCreated', 'FragmentStabilized']
      : skillId === 'architect-skill'
        ? ['ArchitectureDecisionRecorded']
        : skillId === 'db-design-skill'
          ? ['DDLStabilized']
          : skillId === 'ui-design-skill'
            ? ['UIDesignStabilized']
            : skillId === 'system-design-skill'
              ? ['SystemDesignLocked']
              : skillId === 'deploy-skill'
                ? ['DeploymentVerified', 'DeploymentFailed']
                : [];
  return events.filter(e => progressTypes.includes(e.eventType || e.EventType)).length;
}

/** 采集当前 pipeline 运行时快照（timeout / 人工 diagnose 用） */
export async function diagnosePipeline(session, pipelineId) {
  const [runs, events, deliverables, snapshots] = await Promise.all([
    getSkillRuns(session, pipelineId),
    getEvents(session, pipelineId),
    getDeliverables(session, pipelineId),
    getSnapshots(session, pipelineId),
  ]);

  const recentEvents = events.slice(0, 20).map(e => ({
    type: e.eventType || e.EventType,
    skillId: e.skillId || e.SkillId,
    saStep: e.saStepName || e.SaStepName,
    at: e.createdAt || e.CreatedAt,
    preview: (e.payloadPreview || e.PayloadPreview || '').slice(0, 120),
  }));

  const failEvents = events.filter(e => FAILURE_EVENT_TYPES.has(e.eventType || e.EventType));

  return {
    pipelineId,
    at: new Date().toISOString(),
    saServiceUp: await probeSaService(),
    skillRuns: runs.map(r => ({
      skillId: pick(r, 'skillId', 'SkillId'),
      status: pick(r, 'status', 'Status'),
      error: pick(r, 'errorMessage', 'ErrorMessage') || '',
      startedAt: pick(r, 'startedAt', 'StartedAt'),
      completedAt: pick(r, 'completedAt', 'CompletedAt'),
    })),
    deliverableFiles: deliverables.map(d => d.fileName || d.FileName),
    eventSpecCount: snapshots.filter(s =>
      (s.fragmentType || s.FragmentType) === 'IR1_EventSpec'
      || (s.fragmentId || s.FragmentId || '').startsWith('eventspec:')).length,
    skeletonStable: snapshots.some(s =>
      ((s.fragmentType || s.FragmentType) === 'IR0_Skeleton'
        || (s.fragmentId || s.FragmentId || '').startsWith('skeleton:'))
      && ['stable', 'locked'].includes((s.stabilityState || s.StabilityState || '').toLowerCase())),
    recentEvents,
    failureEvents: failEvents.slice(0, 5).map(e => ({
      type: e.eventType || e.EventType,
      preview: (e.payloadPreview || e.PayloadPreview || '').slice(0, 200),
    })),
    hasAnalysisCompleted: events.some(e => (e.eventType || e.EventType) === 'AnalysisCompleted'),
  };
}

export function printDiagnose(diag) {
  log('── diagnose ──');
  log('pipelineId:', diag.pipelineId, '| sa-service:', diag.saServiceUp ? 'UP' : 'DOWN');
  log('deliverables:', diag.deliverableFiles.join(', ') || '(none)');
  for (const r of diag.skillRuns) {
    log(`  run ${r.skillId}: ${r.status}${r.error ? ` — ${r.error}` : ''}`);
  }
  if (diag.failureEvents.length) {
    warn('failure IR events:');
    for (const f of diag.failureEvents) warn(`  ${f.type}: ${f.preview}`);
  }
  log('recent IR:', diag.recentEvents.slice(0, 8).map(e => e.type).join(' → ') || '(none)');
  log('AnalysisCompleted:', diag.hasAnalysisCompleted ? 'yes' : 'no');
}

/** 等待 Skill 终态：failed 立即抛；无 IR 进展则快速判定挂死（非傻等 15 分钟） */
export async function watchSkillTerminal(session, pipelineId, skillId, options = {}) {
  const {
    timeoutMs = skillId === 'analyst-skill' ? 600_000 : 300_000,
    intervalMs = 3000,
    heartbeatSec = 15,
    /** running 超过此秒数仍无 IR 进展 → 判定挂死（真实链路 PM~1min，analyst~3-8min） */
    stallSec = skillId === 'analyst-skill' ? 180 : 120,
    label = skillId,
  } = options;

  const start = Date.now();
  let lastHeartbeat = 0;
  let lastStatus = '';
  const baselineProgress = await countSkillIrProgress(session, pipelineId, skillId);
  let lastProgress = baselineProgress;
  let lastProgressAt = start;

  while (Date.now() - start < timeoutMs) {
    const run = await getLatestSkillRun(session, pipelineId, skillId);
    const st = run ? pick(run, 'status', 'Status') : 'no-run';
    const err = run ? pick(run, 'errorMessage', 'ErrorMessage') || '' : '';

    if (st === 'completed') {
      log(`${label} completed in ${Math.round((Date.now() - start) / 1000)}s`);
      return { status: st, error: err };
    }
    if (st === 'failed' || st === 'cancelled') {
      const diag = await diagnosePipeline(session, pipelineId);
      printDiagnose(diag);
      throw new Error(`${label} ${st}: ${err || '(no errorMessage on run record)'}`);
    }

    const progress = await countSkillIrProgress(session, pipelineId, skillId);
    if (progress > lastProgress) {
      log(`${label} IR 进展：${progress} 条（本轮 +${progress - baselineProgress}）`);
      lastProgress = progress;
      lastProgressAt = Date.now();
    }

    const elapsed = Math.round((Date.now() - start) / 1000);
    const stallElapsed = Math.round((Date.now() - lastProgressAt) / 1000);

    if (st === 'running' && stallElapsed >= stallSec && progress <= baselineProgress) {
      const diag = await diagnosePipeline(session, pipelineId);
      printDiagnose(diag);
      const evidencePath = writeEvidence(`diagnose-stall-${pipelineId}-${skillId}.json`, diag);
      throw new Error(
        `${label} 疑似挂死：${stallElapsed}s 内无 IR 进展（正常 PM~60s / analyst 单事件~2-5min）。` +
        ` 后端可能在轮询 sa-service（最长 30min）。快照 → ${evidencePath}`,
      );
    }

    if (st !== lastStatus) {
      log(`${label} status → ${st} (${elapsed}s)`);
      lastStatus = st;
    } else if (Date.now() - lastHeartbeat >= heartbeatSec * 1000) {
      log(`${label} ${st} … ${elapsed}s（IR +${progress - baselineProgress}，${stallElapsed}s 无新进展）`);
      lastHeartbeat = Date.now();
    }

    await new Promise(r => setTimeout(r, intervalMs));
  }

  const diag = await diagnosePipeline(session, pipelineId);
  printDiagnose(diag);
  const evidencePath = writeEvidence(`diagnose-pipeline-${pipelineId}.json`, diag);
  throw new Error(`timeout: ${label} after ${Math.round(timeoutMs / 1000)}s — snapshot → ${evidencePath}`);
}

export async function waitSkillTerminal(session, pipelineId, skillId, timeoutMs = 600_000) {
  return watchSkillTerminal(session, pipelineId, skillId, { timeoutMs, label: skillId });
}

export async function waitDeliverable(session, pipelineId, fileName, timeoutMs = 300_000) {
  const start = Date.now();
  let lastLog = 0;

  return waitFor(async () => {
    // 若相关 skill 已 failed，不要傻等文件
    const skillMap = {
      '00-merged-requirement.md': null,
      '01-skeleton.md': 'pm-skill',
      '02-requirement-spec.md': 'analyst-skill',
      '03-architecture.md': 'architect-skill',
      '04-system-design.md': 'system-design-skill',
      '05-ddl.sql': 'db-design-skill',
      '06-formpage-ir.json': 'ui-design-skill',
    };
    const boundSkill = skillMap[fileName];
    if (boundSkill) {
      const run = await getLatestSkillRun(session, pipelineId, boundSkill);
      const st = run ? pick(run, 'status', 'Status') : '';
      if (st === 'failed' || st === 'cancelled') {
        throw new Error(`等 ${fileName} 时 ${boundSkill} 已 ${st}: ${pick(run, 'errorMessage', 'ErrorMessage') || ''}`);
      }
    }

    const items = await getDeliverables(session, pipelineId);
    const hit = items.find(i => (i.fileName || i.FileName) === fileName);
    if (hit) return true;

    if (fileName === '00-merged-requirement.md') {
      const report = items.find(i => (i.fileName || i.FileName) === '00-gate-report.json');
      if (report) {
        try {
          const res = await apiRequest('GET', `/api/studio/pipeline/execute/${pipelineId}/deliverables/content?relativePath=${encodeURIComponent('00-gate-report.json')}`, { session });
          const body = typeof res.json === 'string' ? JSON.parse(res.json) : res.json;
          const data = body?.data ?? body;
          if (data && data.passed === false) {
            const reason = data.semanticFitness?.missing?.[0]?.howToFix
              || data.semanticFitness?.nextStepGuidance
              || data.warnings?.[0]
              || '门控未通过（Fail-Closed）';
            throw new Error(`门控失败 — ${reason}`);
          }
        } catch (e) {
          if (e.message.startsWith('门控失败')) throw e;
        }
      }
    }

    const elapsed = Math.round((Date.now() - start) / 1000);
    if (Date.now() - lastLog >= 15000) {
      log(`waiting ${fileName} … ${elapsed}s (have: ${items.map(i => i.fileName || i.FileName).join(', ') || 'none'})`);
      lastLog = Date.now();
    }
    return null;
  }, `deliverable ${fileName}`, timeoutMs, 2000);
}

export function assertDeliverableNames(items, expectedNames) {
  const names = new Set(items.map(i => i.fileName || i.FileName));
  const missing = expectedNames.filter(n => !names.has(n));
  return { pass: missing.length === 0, missing, names: [...names] };
}

export function writeEvidence(fileName, payload) {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
  const p = path.join(EVIDENCE_DIR, fileName);
  fs.writeFileSync(p, JSON.stringify(payload, null, 2), 'utf8');
  return p;
}

export async function runPmSkill(session, pipelineId) {
  const res = await apiRequest('POST', `/api/studio/skills/pm/${pipelineId}/run`, { body: {}, session });
  if (!isJnpfOk(res)) throw new Error(`pm run: ${JSON.stringify(res.json)}`);
  return res;
}

export async function confirmSkeleton(session, pipelineId, autoRunAnalyst = false) {
  const res = await apiRequest('POST', `/api/studio/skills/pm/${pipelineId}/confirm-skeleton`, {
    body: { autoRunAnalyst },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`confirm skeleton: ${JSON.stringify(res.json)}`);
  return res;
}

export async function runAnalystSkill(session, pipelineId) {
  const res = await apiRequest('POST', `/api/studio/skills/analyst/${pipelineId}/run`, { body: {}, session });
  if (!isJnpfOk(res)) throw new Error(`analyst run: ${JSON.stringify(res.json)}`);
  return res;
}

/** 用户确认《需求分析说明书》→ 物化 Job + 可选触发 architect */
export async function confirmRequirementSpec(session, pipelineId, { autoRunDesign = false } = {}) {
  const res = await apiRequest('POST', `/api/studio/skills/analyst/${pipelineId}/confirm-requirement-spec`, {
    body: { autoRunDesign },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`confirm-requirement-spec: ${JSON.stringify(res.json)}`);
  return jnpfData(res) || res.json;
}

/** 流水线阶段确认（传 pipelineId）→ StageConfirmSkillTrigger 调度下一步 Skill */
export async function confirmStage(session, pipelineId, { approved = true, comment = 'E2E stage confirm' } = {}) {
  const res = await apiRequest('POST', `/api/studio/pipeline/execute/stage/${pipelineId}/confirm`, {
    body: { approved, comment },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`stage confirm: ${JSON.stringify(res.json)}`);
  return jnpfData(res) || res.json;
}

/** 设计四 Skill 编排（AnalysisCompleted 后一键跑 architect + db/ui + system-design） */
export async function runDesignOrchestrator(session, pipelineId, { providerCode } = {}) {
  const res = await apiRequest('POST', `/api/studio/skills/design/${pipelineId}/run`, {
    body: providerCode ? { providerCode } : {},
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`design orchestrator run: ${JSON.stringify(res.json)}`);
  return pick(jnpfData(res), 'runId', 'RunId');
}

export async function getDesignStatus(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/skills/design/${pipelineId}/status`, { session });
  return jnpfData(res) || res.json?.data || res.json || {};
}

/** 从 IR 重建 deliverables（Skill 已跑但落盘失败时补建） */
export async function rebuildDeliverables(session, pipelineId, stages) {
  const qs = stages?.length ? `?stages=${stages.join(',')}` : '';
  const res = await apiRequest('POST', `/api/studio/pipeline/execute/${pipelineId}/deliverables/rebuild${qs}`, {
    body: {},
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`deliverables rebuild: ${JSON.stringify(res.json)}`);
  return jnpfData(res) || res.json;
}

export async function runDeploySkill(session, pipelineId) {
  const res = await apiRequest('POST', `/api/studio/skills/deploy/${pipelineId}/run`, { body: {}, session });
  if (res.status === 404) {
    throw new Error('deploy API 404 — 请 dotnet build + 重启 :5000');
  }
  if (!isJnpfOk(res)) throw new Error(`deploy run: ${JSON.stringify(res.json)}`);
  return pick(jnpfData(res), 'runId', 'RunId');
}

export async function probeSaService() {
  try {
    const res = await fetch('http://127.0.0.1:3001/sa/run-step', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: '{}',
      signal: AbortSignal.timeout(3000),
    });
    return res.status >= 400 && res.status < 600;
  } catch {
    return false;
  }
}

export async function probeEnv() {
  let apiOk = false;
  try {
    const res = await apiRequest('GET', '/api/oauth/CurrentUser');
    apiOk = isJnpfOk(res);
  } catch {
    apiOk = false;
  }
  const saUp = await probeSaService();
  return { apiOk, saUp, apiUrl: process.env.JNPF_API_URL || 'http://localhost:5000', saUrl: 'http://127.0.0.1:3001' };
}
