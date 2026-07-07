/**
 * Studio E2E 分步运行器 — 状态持久化、单步执行、断点续跑、poll-once 探针
 *
 * 状态文件：scripts/.e2e-state.json
 * 锁文件：  scripts/.e2e-lock.json（防并发踩同一 pipeline）
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { apiRequest, login, isJnpfOk, jnpfData, pick, loadCachedSession } from './jnpf-auth.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
export const STATE_FILE = path.join(__dirname, '..', '.e2e-state.json');
export const LOCK_FILE = path.join(__dirname, '..', '.e2e-lock.json');

const DEFAULT_REQUIREMENT =
  process.env.E2E_REQUIREMENT ||
  '请假系统 E2E：员工提交请假、主管审批、HR归档；角色：员工/主管/HR。';

/** 模块级 session 缓存，减少重复读盘；401/600 后须 invalidateSessionCache() */
let _cachedSession = null;
let _backendOkAt = 0;
const BACKEND_TTL_MS = 30_000;

/** 401/600 重登后调用，避免后续请求仍用旧 token */
export function invalidateSessionCache() {
  _cachedSession = null;
}

/** 将磁盘上最新 token 合并进传入的 session 对象（长时 E2E 轮询用） */
export function syncSessionFromDisk(session) {
  const fresh = loadCachedSession();
  if (fresh?.token && session) {
    session.token = fresh.token;
    session.expiresAt = fresh.expiresAt;
    session.apiUrl = fresh.apiUrl;
    _cachedSession = fresh;
  }
  return fresh || session;
}

function isProcessAlive(pid) {
  if (!pid || pid === process.pid) return true;
  try {
    process.kill(pid, 0);
    return true;
  } catch (e) {
    return e.code === 'EPERM';
  }
}

/** 轮询专用：短 TTL 缓存 collectStatus，避免 wait 循环内重复打 API */
export function createStatusPoller(session, pipelineId, { ttlMs = 800, includeDeliverables = false } = {}) {
  let cache = null;
  let cacheAt = 0;
  return async function pollStatus(force = false) {
    const now = Date.now();
    if (!force && cache && now - cacheAt < ttlMs) return cache;
    cache = await collectStatus(pipelineId, session, { includeDeliverables });
    cacheAt = now;
    return cache;
  };
}

export function parseE2eArgs(argv = process.argv.slice(2)) {
  const args = {
    command: 'help',
    step: null,
    fromStep: null,
    pipelineId: process.env.E2E_PIPELINE_ID ? Number(process.env.E2E_PIPELINE_ID) : null,
    pollOnce: false,
    timeoutMs: null,
    autoAnalyst: false,
    useGate: true,
    useSimulate: false,
    requirement: DEFAULT_REQUIREMENT,
    json: false,
    skipIfDone: false,
    noLock: false,
    verbose: false,
    retries: Number(process.env.E2E_API_RETRIES || 3),
  };

  const rest = [];
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--step' && argv[i + 1]) { args.command = 'step'; args.step = argv[++i]; }
    else if (a === '--from' && argv[i + 1]) { args.command = 'from'; args.fromStep = argv[++i]; }
    else if (a === '--pipeline-id' && argv[i + 1]) { args.pipelineId = Number(argv[++i]); }
    else if (a === '--poll-once') args.pollOnce = true;
    else if (a === '--timeout' && argv[i + 1]) { args.timeoutMs = Number(argv[++i]); }
    else if (a === '--auto-analyst') args.autoAnalyst = true;
    else if (a === '--no-gate') args.useGate = false;
    else if (a === '--simulate') args.useSimulate = true;
    else if (a === '--requirement' && argv[i + 1]) { args.requirement = argv[++i]; }
    else if (a === '--json') args.json = true;
    else if (a === '--skip-if-done') args.skipIfDone = true;
    else if (a === '--verbose') args.verbose = true;
    else if (a === '--no-lock') args.noLock = true;
    else if (a === '--retries' && argv[i + 1]) { args.retries = Number(argv[++i]); }
    else if (a === 'step' && argv[i + 1] && !argv[i + 1].startsWith('-')) {
      args.command = 'step';
      args.step = argv[++i];
    }
    else if (a === 'from' && argv[i + 1] && !argv[i + 1].startsWith('-')) {
      args.command = 'from';
      args.fromStep = argv[++i];
    }
    else if (a === 'status') args.command = 'status';
    else if (a === 'init') args.command = 'init';
    else if (a === 'all') args.command = 'all';
    else if (a === 'quick') args.command = 'quick';
    else if (a === 'help' || a === '-h' || a === '--help') args.command = 'help';
    else rest.push(a);
  }

  if (rest.includes('--full')) args.autoAnalyst = true;
  if (rest.includes('--headed')) args.headed = true;
  return args;
}

export function loadState() {
  try {
    if (!fs.existsSync(STATE_FILE)) return null;
    return JSON.parse(fs.readFileSync(STATE_FILE, 'utf8'));
  } catch {
    return null;
  }
}

export function saveState(partial) {
  const prev = loadState() || {};
  const next = {
    ...prev,
    ...partial,
    updatedAt: new Date().toISOString(),
  };
  fs.mkdirSync(path.dirname(STATE_FILE), { recursive: true });
  fs.writeFileSync(STATE_FILE, JSON.stringify(next, null, 2), 'utf8');
  return next;
}

export function createLogger(prefix) {
  return (...args) => console.log(`[${prefix}]`, ...args);
}

function sleep(ms) {
  return new Promise(r => setTimeout(r, ms));
}

function isTransientError(err, result) {
  if (err?.name === 'AbortError' || err?.name === 'TimeoutError') return true;
  if (err?.code === 'ECONNRESET' || err?.code === 'ECONNREFUSED') return true;
  const status = result?.status;
  return status === 502 || status === 503 || status === 504 || status === 429;
}

function formatApiError(label, result) {
  const snippet = typeof result?.json === 'object'
    ? JSON.stringify(result.json).slice(0, 400)
    : String(result?.text || '').slice(0, 400);
  return `${label} HTTP ${result?.status} body=${snippet}`;
}

/** E2E 专用 API：超时 + 指数退避重试 */
export async function apiE2e(method, urlPath, opts = {}) {
  const {
    session,
    body,
    retries = 3,
    timeoutMs = 45_000,
    label = `${method} ${urlPath}`,
  } = opts;

  let lastErr;
  for (let attempt = 1; attempt <= retries; attempt++) {
    try {
      const result = await apiRequest(method, urlPath, { session, body, timeoutMs });
      if (result.status === 401 || result.status === 600) {
        invalidateSessionCache();
        syncSessionFromDisk(session);
      }
      if (!result.ok && isTransientError(null, result) && attempt < retries) {
        const wait = Math.min(1000 * 2 ** (attempt - 1), 8000);
        await sleep(wait);
        continue;
      }
      return result;
    } catch (err) {
      lastErr = err;
      if (isTransientError(err) && attempt < retries) {
        const wait = Math.min(1000 * 2 ** (attempt - 1), 8000);
        await sleep(wait);
        continue;
      }
      throw new Error(`${label} 失败(尝试 ${attempt}/${retries}): ${err.message}`);
    }
  }
  throw lastErr || new Error(`${label} 失败 after ${retries} retries`);
}

export function acquireLock(pipelineId, stepName, { noLock = false } = {}) {
  if (noLock) return () => {};
  if (fs.existsSync(LOCK_FILE)) {
    try {
      const lock = JSON.parse(fs.readFileSync(LOCK_FILE, 'utf8'));
      const age = Date.now() - new Date(lock.since).getTime();
      const stale = age >= 2 * 60 * 60 * 1000 || !isProcessAlive(lock.pid);
      if (!stale && lock.pid !== process.pid) {
        throw new Error(
          `E2E 锁占用中: pipeline=${lock.pipelineId} step=${lock.step} pid=${lock.pid} — 等待或 --no-lock`);
      }
      if (stale) {
        try { fs.unlinkSync(LOCK_FILE); } catch { /* ignore */ }
      }
    } catch (e) {
      if (e.message.includes('E2E 锁占用')) throw e;
    }
  }
  fs.writeFileSync(LOCK_FILE, JSON.stringify({
    pipelineId,
    step: stepName,
    pid: process.pid,
    since: new Date().toISOString(),
  }), 'utf8');
  const release = () => {
    try { fs.unlinkSync(LOCK_FILE); } catch { /* ignore */ }
  };
  return release;
}

/** Ctrl+C 时释放锁，避免残留阻塞后续分步 */
let _activeLockRelease = null;
function setupLockSignalHandlers() {
  if (setupLockSignalHandlers._done) return;
  setupLockSignalHandlers._done = true;
  const onSignal = () => {
    if (_activeLockRelease) {
      try { _activeLockRelease(); } catch { /* ignore */ }
      _activeLockRelease = null;
    }
    process.exit(130);
  };
  process.on('SIGINT', onSignal);
  process.on('SIGTERM', onSignal);
}
setupLockSignalHandlers();

export function withActiveLock(release) {
  _activeLockRelease = release;
  return () => {
    release();
    if (_activeLockRelease === release) _activeLockRelease = null;
  };
}

/** 带心跳 + 自适应间隔 + 轮询内错误容忍 */
export async function waitFor(options) {
  const {
    fn,
    label,
    timeoutMs = 120_000,
    intervalMs = 2_000,
    pollOnce = false,
    onPoll,
    log = createLogger('e2e'),
    maxPollErrors = 5,
  } = options;

  const deadline = Date.now() + timeoutMs;
  let lastHeartbeat = 0;
  let pollErrors = 0;
  let idlePolls = 0;
  let currentInterval = Math.min(intervalMs, 1500);

  while (true) {
    let result;
    try {
      result = await fn();
      pollErrors = 0;
    } catch (err) {
      pollErrors++;
      if (pollErrors >= maxPollErrors) throw err;
      log(`WARN ${label} poll error (${pollErrors}/${maxPollErrors}):`, err.message);
      result = false;
    }

    if (result === true || (result && result !== false)) {
      return result;
    }

    const now = Date.now();
    if (onPoll) {
      try { await onPoll(result); } catch { /* non-fatal */ }
    }

    if (pollOnce) {
      log(`probe ${label}: 未就绪（poll-once 模式，不阻塞）`);
      return null;
    }

    if (now >= deadline) {
      throw new Error(`timeout: ${label} (${Math.round(timeoutMs / 1000)}s) — 用 status 或 --poll-once 排查`);
    }

    if (now - lastHeartbeat >= 15_000) {
      lastHeartbeat = now;
      const left = Math.max(0, Math.ceil((deadline - now) / 1000));
      log(`waiting ${label} … 剩余 ${left}s (interval ${currentInterval}ms)`);
    }

    idlePolls++;
    // 长时间无进展 → 降低轮询频率，减轻 backend 压力
    if (idlePolls > 10) currentInterval = Math.min(5000, currentInterval + 500);
    else if (idlePolls > 3) currentInterval = Math.min(3000, currentInterval + 250);

    await sleep(currentInterval);
  }
}

export async function ensureBackend(log = createLogger('e2e')) {
  if (Date.now() - _backendOkAt < BACKEND_TTL_MS) return;
  const url = process.env.JNPF_API_URL || 'http://localhost:5000';
  for (let i = 0; i < 3; i++) {
    try {
      const res = await fetch(`${url}/api/oauth/getLoginConfig`, {
        signal: AbortSignal.timeout(8000),
      });
      if (res.ok || res.status === 403) {
        _backendOkAt = Date.now();
        log('backend ok', url);
        return;
      }
    } catch { /* retry */ }
    await sleep(1000 * (i + 1));
  }
  throw new Error('后端未启动 — 请先 powershell -File start-dev.ps1');
}

export async function getSession(force = false) {
  if (force) invalidateSessionCache();
  if (!force && _cachedSession?.token) return _cachedSession;
  _cachedSession = await login(force ? { force: true } : {});
  return _cachedSession;
}

export async function getEventTypes(pipelineId, session) {
  const ev = await apiE2e('GET', `/api/studio/ir/${pipelineId}/events`, { session, timeoutMs: 30_000 });
  const list = Array.isArray(ev.json) ? ev.json : jnpfData(ev) || [];
  return list.map(e => pick(e, 'eventType', 'EventType'));
}

export async function getSkillRuns(pipelineId, session) {
  const res = await apiE2e('GET', `/api/studio/skills/${pipelineId}/runs`, { session, timeoutMs: 20_000 });
  return Array.isArray(res.json) ? res.json : jnpfData(res) || [];
}

export async function listDeliverables(pipelineId, session) {
  const res = await apiE2e('GET', `/api/studio/pipeline/execute/${pipelineId}/deliverables`, {
    session,
    timeoutMs: 20_000,
    retries: 2,
  });
  if (!isJnpfOk(res)) return { ok: false, items: [], raw: res, status: res.status };
  const data = jnpfData(res);
  const items = data?.items ?? (Array.isArray(data) ? data : []);
  return { ok: true, items, raw: res, status: res.status };
}

export function hasDeliverable(items, fileName) {
  return items.some(d =>
    pick(d, 'fileName', 'FileName') === fileName ||
    pick(d, 'relativePath', 'RelativePath') === fileName);
}

/** 并行采集状态；includeDeliverables=false 时跳过可能 404 的 deliverables 端点（更快） */
export async function collectStatus(pipelineId, session, { includeDeliverables = true } = {}) {
  const tasks = [
    getEventTypes(pipelineId, session).catch(() => []),
    getSkillRuns(pipelineId, session).catch(() => []),
  ];
  if (includeDeliverables) {
    tasks.push(listDeliverables(pipelineId, session).catch(() => ({ ok: false, items: [] })));
  }

  const results = await Promise.all(tasks);
  const types = results[0];
  const runs = results[1];
  const del = includeDeliverables
    ? results[2]
    : { ok: false, items: [], skipped: true };

  const saCount = types.filter(t => t === 'SA_Step_Completed').length;
  const skillStatus = {};
  for (const r of runs) {
    skillStatus[pick(r, 'skillId', 'SkillId')] = pick(r, 'status', 'Status');
  }

  return {
    pipelineId,
    events: {
      SkeletonCreated: types.includes('SkeletonCreated'),
      StageConfirmed: types.includes('StageConfirmed'),
      FragmentStabilized: types.includes('FragmentStabilized'),
      AnalysisCompleted: types.includes('AnalysisCompleted'),
      saStepCount: saCount,
      uniqueTypes: [...new Set(types)],
    },
    skills: skillStatus,
    deliverables: del.items.map(d => pick(d, 'relativePath', 'RelativePath', 'fileName', 'FileName')),
    deliverablesOk: del.ok,
    deliverablesSkipped: !!del.skipped,
    collectedAt: new Date().toISOString(),
  };
}

export async function validatePipeline(pipelineId, session) {
  const res = await apiE2e('GET', `/api/studio/ir/${pipelineId}/events`, {
    session,
    timeoutMs: 15_000,
    label: `validate pipeline ${pipelineId}`,
  });
  if (res.status === 404) throw new Error(`pipeline ${pipelineId} 不存在`);
  if (!res.ok && !isJnpfOk(res)) throw new Error(formatApiError(`pipeline ${pipelineId}`, res));
  return true;
}

/** 推断下一步（status 输出用） */
export function inferNextStep(status) {
  const e = status.events;
  const pm = status.skills['pm-skill'];
  const analyst = status.skills['analyst-skill'];

  if (!e.SkeletonCreated && pm !== 'completed') return 's0-gate 或 s1-pm';
  if (e.SkeletonCreated && !e.FragmentStabilized) return 's1-confirm';
  if (e.FragmentStabilized && analyst !== 'running' && analyst !== 'completed') return 's2-run';
  if (analyst === 'running' || (analyst === 'completed' && !e.AnalysisCompleted)) return 's2-wait';
  if (e.AnalysisCompleted) return 's2-check';
  return 'status';
}

export function isStepSatisfied(stepName, status) {
  const e = status.events;
  const skills = status.skills;
  switch (stepName) {
    case 's1-skeleton':
    case 's1-check':
      return e.SkeletonCreated;
    case 's1-confirm':
      return e.FragmentStabilized || e.StageConfirmed;
    case 's2-run':
      return skills['analyst-skill'] === 'running' || skills['analyst-skill'] === 'completed';
    case 's2-wait':
    case 's2-check':
      return e.AnalysisCompleted && e.saStepCount >= 9;
    case 's0-gate':
      return skills['pm-skill'] === 'completed';
    default:
      return false;
  }
}

export function printStatus(status, log = createLogger('e2e')) {
  log('pipelineId', status.pipelineId);
  log('skills', status.skills);
  log('events', {
    SkeletonCreated: status.events.SkeletonCreated,
    StageConfirmed: status.events.StageConfirmed,
    AnalysisCompleted: status.events.AnalysisCompleted,
    saSteps: status.events.saStepCount,
  });
  log('deliverables', status.deliverables);
  log('suggestedNext', inferNextStep(status));
  if (!status.deliverablesOk && !status.deliverablesSkipped) {
    log('WARN deliverables API 不可用 — 请重启 backend 加载最新代码');
  }
}

export function suggestNext(pipelineId, stepName) {
  console.log('');
  console.log('── 下一步 ──');
  console.log(`  node scripts/studio-e2e.mjs status --pipeline-id ${pipelineId}`);
  if (stepName) {
    console.log(`  node scripts/studio-e2e.mjs step ${stepName} --pipeline-id ${pipelineId}`);
    console.log(`  node scripts/studio-e2e.mjs step ${stepName} --pipeline-id ${pipelineId} --skip-if-done`);
  }
  console.log(`  node scripts/studio-e2e.mjs step <step> --pipeline-id ${pipelineId} --poll-once`);
  console.log('');
}

export const STEP_TIMEOUTS = {
  health: 10_000,
  create: 30_000,
  's0-gate': 180_000,
  's0-check': 15_000,
  's1-pm': 180_000,
  's1-skeleton': 180_000,
  's1-check': 15_000,
  's1-confirm': 60_000,
  's2-run': 30_000,
  's2-wait': Number(process.env.PHASE2_E2E_TIMEOUT_MS || 900_000),
  's2-check': 15_000,
  'rebuild-deliverables': 30_000,
  'd11-revise': 300_000,
  status: 15_000,
};

export const STEP_ORDER = [
  'create',
  's0-gate',
  's0-check',
  's1-check',
  's1-confirm',
  's2-run',
  's2-wait',
  's2-check',
  'd11-revise',
];

export const QUICK_STEPS = ['create', 's1-pm', 's1-skeleton', 's1-confirm', 's1-check'];

export const FULL_STEPS = [
  'create',
  's0-gate',
  's0-check',
  's1-check',
  's1-confirm',
  's2-run',
  's2-wait',
  's2-check',
];

export function printHelp() {
  console.log(`
Studio E2E — 分步执行，避免长时间卡死

用法:
  node scripts/studio-e2e.mjs init
  node scripts/studio-e2e.mjs step <name>
  node scripts/studio-e2e.mjs status
  node scripts/studio-e2e.mjs quick | all | from s2-wait

选项:
  --pipeline-id <id>    断点续跑
  --poll-once           只探测一次，不阻塞
  --skip-if-done        步骤已完成则跳过（幂等）
  --verbose             轮询时输出每次 probe（默认仅变化时）
  --timeout <ms>        单步超时
  --no-lock             跳过 pipeline 锁
  --retries <n>         API 重试次数（默认 3）
  --auto-analyst        confirm 时自动跑 Analyst
  --no-gate / --simulate / --requirement / --json

稳定性:
  - API 45s 超时 + 指数退避重试（502/503/504/429/网络错误）
  - JWT 401/600 自动重登
  - status 并行请求 events/runs/deliverables；轮询内 800ms TTL 缓存
  - 长等待自适应降频（1.5s→5s），减轻 backend 压力
  - 锁文件检测僵尸 PID + Ctrl+C 自动释放
  - .e2e-lock.json 防并发踩同一 pipeline

步骤: create | s0-gate | s0-check | s1-pm | s1-skeleton | s1-check |
      s1-confirm | s2-run | s2-wait | s2-check | rebuild-deliverables |
      d11-revise | status
`);
}

export function recordStepTiming(stepName, ms, ok = true) {
  const state = loadState() || {};
  const timings = state.stepTimings || {};
  timings[stepName] = { ms, ok, at: new Date().toISOString() };
  saveState({ stepTimings: timings });
}
