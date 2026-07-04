/**
 * 阶段四 Green path / DoD 共享 API 辅助
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { apiRequest, isJnpfOk, jnpfData, pick } from './jnpf-auth.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
export const REPO_ROOT = path.resolve(__dirname, '../..');
export const EVIDENCE_DIR = path.join(REPO_ROOT, '.claude', 'evidence');

export const IR3_FRAGMENT_TYPES = ['IR3_GeneratedCode', 'IR3_ArchReport', 'IR3_TestSuite'];
export const GREEN_SUCCESS_EVENTS = ['CodeGeneratedStablePromoted', 'TestSuiteGenerated'];
export const GREEN_FAIL_EVENTS = ['CodegenFailed'];

export const log = (...args) => console.log('[phase4]', ...args);

export async function waitFor(fn, label, timeoutMs = 120_000, intervalMs = 1500) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const hit = await fn();
    if (hit) return hit;
    await new Promise(r => setTimeout(r, intervalMs));
  }
  throw new Error(`timeout: ${label}`);
}

export async function createPipeline(session, name, requirement) {
  const res = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
    body: {
      name,
      userRequirement:
        requirement
        || `${name}：员工请假审批 leave-simple Green path，含 LeaveRequest 单表 MVP。`.padEnd(400, '测'),
    },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(`create pipeline: ${JSON.stringify(res.json)}`);
  return pick(jnpfData(res), 'pipelineId', 'PipelineId');
}

export async function simulate(session, pipelineId, body) {
  const res = await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, { body, session });
  if (!isJnpfOk(res)) throw new Error(`simulate ${body.eventType}: ${JSON.stringify(res.json)}`);
  return res;
}

export async function setupIr1Stable(session, pipelineId) {
  await simulate(session, pipelineId, { eventType: 'SkeletonCreated' });
  await simulate(session, pipelineId, {
    eventType: 'EventSpecConfirmed',
    fragmentId: 'eventspec:BE-001',
  });
}

export async function setupIr2Clean(session, pipelineId) {
  await simulate(session, pipelineId, { eventType: 'ArchitectureDecisionRecorded' });
  await simulate(session, pipelineId, { eventType: 'DDLStabilized' });
  await simulate(session, pipelineId, { eventType: 'UIDesignStabilized' });
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

export async function getDiagnostics(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/diagnostics`, { session });
  return jnpfData(res) || res.json?.data || res.json || {};
}

export async function waitSkillTerminal(session, pipelineId, skillId, timeoutMs = 120_000) {
  return waitFor(async () => {
    const res = await apiRequest('GET', `/api/studio/skills/${pipelineId}/runs`, { session });
    const list = Array.isArray(res.json) ? res.json : jnpfData(res) || [];
    const run = list.find(r => pick(r, 'skillId', 'SkillId') === skillId);
    const st = pick(run, 'status', 'Status');
    if (st === 'completed' || st === 'failed' || st === 'cancelled') {
      return {
        status: st,
        error: pick(run, 'errorMessage', 'ErrorMessage') || '',
      };
    }
    return null;
  }, `skill ${skillId}`, timeoutMs);
}

/** IR-2 locked 前置：simulate IR-1 + IR-2 三片段 → system-design-skill */
export async function setupIr2Locked(session, pipelineId) {
  await setupIr1Stable(session, pipelineId);
  await setupIr2Clean(session, pipelineId);

  const run = await apiRequest('POST', `/api/studio/skills/system-design/${pipelineId}/run`, {
    body: {},
    session,
  });
  if (!isJnpfOk(run)) {
    throw new Error(`system-design run: ${JSON.stringify(run.json)}`);
  }

  const terminal = await waitSkillTerminal(session, pipelineId, 'system-design-skill');
  const types = (await getEvents(session, pipelineId)).map(e => pick(e, 'eventType', 'EventType'));
  const locked = types.includes('SystemDesignLocked');
  if (terminal.status !== 'completed' || !locked) {
    throw new Error(
      `IR-2 lock failed: run=${terminal.status}, SystemDesignLocked=${locked}, err=${terminal.error}`,
    );
  }

  const snaps = await getSnapshots(session, pipelineId);
  const system = snaps.find(s => pick(s, 'fragmentType', 'FragmentType') === 'IR2_SystemDesign');
  const state = pick(system, 'stabilityState', 'StabilityState');
  if (state !== 'locked') {
    throw new Error(`IR2_SystemDesign stability=${state}, expected locked`);
  }

  return { locked: true, systemDesignState: state };
}

export async function runDeveloperOrchestrator(session, pipelineId) {
  const res = await apiRequest('POST', `/api/studio/skills/developer/${pipelineId}/run`, {
    body: {},
    session,
  });
  if (res.status === 404) {
    throw new Error(
      'developer API 404 — 后端未加载 DeveloperSkillsApiService，请执行 start-dev.ps1 重启 :5000',
    );
  }
  if (!isJnpfOk(res)) {
    const msg = res.json?.msg ?? res.json?.message ?? JSON.stringify(res.json) ?? res.status;
    throw new Error(`developer run HTTP ${res.status}: ${msg}`);
  }
  return pick(jnpfData(res), 'runId', 'RunId');
}

/** 启动前探测 developer/status 是否注册 */
export async function probeDeveloperApi(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/skills/developer/${pipelineId}/status`, { session });
  if (res.status === 404) {
    throw new Error(
      'developer/status 404 — 请 dotnet build + start-dev.ps1 重启后端后再跑 Green path',
    );
  }
  return res;
}

export async function getDeveloperStatus(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/skills/developer/${pipelineId}/status`, { session });
  return jnpfData(res) || res.json?.data || res.json || {};
}

export async function waitDeveloperGreen(session, pipelineId, timeoutMs) {
  let lastTypes = [];
  return waitFor(async () => {
    const types = (await getEvents(session, pipelineId)).map(
      e => pick(e, 'eventType', 'EventType'),
    );
    lastTypes = types;

    if (types.some(t => GREEN_FAIL_EVENTS.includes(t))) {
      return { ok: false, reason: 'CodegenFailed', types };
    }

    const hasPromote = types.includes('CodeGeneratedStablePromoted');
    const hasTestSuite = types.includes('TestSuiteGenerated');
    if (hasPromote && hasTestSuite) {
      return { ok: true, types };
    }
    return null;
  }, 'developer green (promote + TestSuite)', timeoutMs, 2000).catch(err => {
    err.lastEventTypes = lastTypes;
    throw err;
  });
}

export function parseSnapshotPayload(raw) {
  if (raw == null) return null;
  try {
    return typeof raw === 'string' ? JSON.parse(raw) : raw;
  } catch {
    return null;
  }
}

export function findSnapshot(snapshots, fragmentType) {
  return snapshots.find(s => pick(s, 'fragmentType', 'FragmentType') === fragmentType);
}

export function resolveGeneratedBackendRoot(tenantId, projectId) {
  return path.join(REPO_ROOT, 'workspace', 'generated', String(tenantId), String(projectId), 'backend');
}

export function assertGeneratedArtifacts(backendRoot) {
  const missing = [];
  const entityDir = path.join(backendRoot, 'Entitys');
  const serviceDir = path.join(backendRoot, 'Services');
  const ifaceDir = path.join(backendRoot, 'Interfaces');

  if (!fs.existsSync(backendRoot)) {
    return { pass: false, detail: `backend root missing: ${backendRoot}`, missing: ['backend/'] };
  }

  const entityFiles = fs.existsSync(entityDir)
    ? fs.readdirSync(entityDir).filter(f => f.endsWith('Entity.cs'))
    : [];
  const serviceFiles = fs.existsSync(serviceDir)
    ? fs.readdirSync(serviceDir).filter(f => f.endsWith('Service.cs') && !f.endsWith('.custom.cs'))
    : [];
  const ifaceFiles = fs.existsSync(ifaceDir)
    ? fs.readdirSync(ifaceDir).filter(f => f.startsWith('I') && f.endsWith('Service.cs'))
    : [];

  if (entityFiles.length < 1) missing.push('Entitys/*Entity.cs');
  if (serviceFiles.length < 1) missing.push('Services/*Service.cs');
  if (ifaceFiles.length < 1) missing.push('Interfaces/I*Service.cs');

  return {
    pass: missing.length === 0,
    detail: `entity=${entityFiles.length}, service=${serviceFiles.length}, iface=${ifaceFiles.length}`,
    entityFiles,
    serviceFiles,
    ifaceFiles,
    backendRoot,
    missing,
  };
}

/** Q4 — 生成 Service.cs 须含租户过滤痕迹（AG-002/AG-003 抽样） */
export function scanGeneratedSqlForTenantFilter(generatedRoot = path.join(REPO_ROOT, 'workspace', 'generated')) {
  if (!fs.existsSync(generatedRoot)) {
    return { pass: true, scanned: 0, issues: [], detail: 'no workspace/generated yet' };
  }

  const issues = [];
  let scanned = 0;

  function walk(dir) {
    for (const name of fs.readdirSync(dir)) {
      const full = path.join(dir, name);
      const st = fs.statSync(full);
      if (st.isDirectory()) {
        walk(full);
        continue;
      }
      if (!name.endsWith('Service.cs') || name.endsWith('.custom.cs')) continue;
      scanned++;
      const content = fs.readFileSync(full, 'utf8');
      const hasTenantFilter =
        content.includes('ITenantFilter')
        || /F_TenantId|F_Tenant_Id|@TenantId|TenantId\s*[=)]/.test(content);
      if (!hasTenantFilter) {
        issues.push({ file: path.relative(REPO_ROOT, full), reason: 'missing tenant filter marker' });
      }
    }
  }

  walk(generatedRoot);
  return {
    pass: issues.length === 0,
    scanned,
    issues,
    detail: issues.length === 0
      ? `${scanned} Service.cs scanned, all contain tenant markers`
      : `${issues.length} issue(s) in ${scanned} files`,
  };
}

export function writeEvidence(filename, data) {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
  const p = path.join(EVIDENCE_DIR, filename);
  fs.writeFileSync(p, JSON.stringify(data, null, 2), 'utf8');
  return p;
}
