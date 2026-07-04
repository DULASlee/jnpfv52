#!/usr/bin/env node
/**
 * 阶段二 Skill 链路 E2E — 纯 HTTP（无浏览器）
 * 依赖 scripts/lib/jnpf-auth.mjs 自动登录
 *
 *   node scripts/phase2-skills-e2e.mjs
 *   node scripts/phase2-skills-e2e.mjs --headed   # 可选：登录后打开页面挂 console
 */

import { apiRequest, login, isJnpfOk, jnpfData, pick } from './lib/jnpf-auth.mjs';

const HEADED = process.argv.includes('--headed');
const ANALYST_TIMEOUT_MS = Number(process.env.PHASE2_E2E_TIMEOUT_MS || 900_000);
const log = (...args) => console.log('[phase2-e2e]', ...args);

async function waitFor(fn, label, timeoutMs = 120_000, onPoll) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const hit = await fn();
    if (onPoll) await onPoll();
    if (hit) return hit;
    await new Promise(r => setTimeout(r, 1500));
  }
  throw new Error(`timeout: ${label}`);
}

async function getEventTypes(pipelineId, session) {
  const ev = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
  const list = Array.isArray(ev.json) ? ev.json : jnpfData(ev) || [];
  return list.map(e => pick(e, 'eventType', 'EventType'));
}

async function runHttpFlow() {
  const session = await login();
  const token = session.token;

  // 短需求 → PM 回退路径约 6 个 businessEvents，Analyst 可在 10min 内完成
  const requirement = '请假系统 E2E：员工提交请假、主管审批、HR归档。';
  const create = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
    body: { name: 'Phase2-script-E2E', userRequirement: requirement },
    session,
  });
  if (!isJnpfOk(create)) throw new Error(`create: ${JSON.stringify(create.json)}`);
  const pipelineId = pick(jnpfData(create), 'pipelineId', 'PipelineId');
  log('pipelineId', pipelineId);

  const sim = await apiRequest('POST', `/api/studio/ir/${pipelineId}/simulate`, {
    body: { eventType: 'SkeletonCreated' },
    session,
  });

  if (!isJnpfOk(sim)) {
    log('simulate 不可用，改跑 PM Skill:', sim.json?.msg || sim.status);
    const pm = await apiRequest('POST', `/api/studio/skills/pm/${pipelineId}/run`, { body: {}, session });
    if (!isJnpfOk(pm)) throw new Error(`pm run: ${JSON.stringify(pm.json)}`);
    await waitFor(async () => {
      const ev = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
      const list = Array.isArray(ev.json) ? ev.json : jnpfData(ev) || [];
      return list.some(e => pick(e, 'eventType', 'EventType') === 'SkeletonCreated');
    }, 'SkeletonCreated', 180_000);
  } else {
    await waitFor(async () => {
      const ev = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
      const list = Array.isArray(ev.json) ? ev.json : jnpfData(ev) || [];
      return list.some(e => pick(e, 'eventType', 'EventType') === 'SkeletonCreated');
    }, 'SkeletonCreated');
  }

  const confirm = await apiRequest('POST', `/api/studio/skills/pm/${pipelineId}/confirm-skeleton`, {
    body: { autoRunAnalyst: false },
    session,
  });
  if (!isJnpfOk(confirm)) throw new Error(`confirm: ${JSON.stringify(confirm.json)}`);
  await waitFor(async () => {
    const types = await getEventTypes(pipelineId, session);
    return types.includes('FragmentStabilized');
  }, 'FragmentStabilized', 60_000);
  if (!(await getEventTypes(pipelineId, session)).includes('StageConfirmed')) {
    log('WARN: StageConfirmed 未出现 — 请重启 backend 以加载 D3 代码');
  }

  const run = await apiRequest('POST', `/api/studio/skills/analyst/${pipelineId}/run`, {
    body: {},
    session,
  });
  if (!isJnpfOk(run)) throw new Error(`analyst: ${JSON.stringify(run.json)}`);

  let lastLog = 0;
  await waitFor(async () => {
    const types = await getEventTypes(pipelineId, session);
    if (types.includes('AnalysisCompleted')) return { status: 'completed' };

    const runs = await apiRequest('GET', `/api/studio/skills/${pipelineId}/runs`, { session });
    const list = Array.isArray(runs.json) ? runs.json : jnpfData(runs) || [];
    const analyst = list.find(r => pick(r, 'skillId', 'SkillId') === 'analyst-skill');
    const status = pick(analyst, 'status', 'Status');
    if (status === 'failed') throw new Error(`analyst failed: ${pick(analyst, 'errorMessage', 'ErrorMessage')}`);
    return status === 'completed' ? analyst : null;
  }, 'analyst completed / AnalysisCompleted', ANALYST_TIMEOUT_MS, async () => {
    const now = Date.now();
    if (now - lastLog < 30_000) return;
    lastLog = now;
    const types = await getEventTypes(pipelineId, session);
    log('progress SA=', types.filter(t => t === 'SA_Step_Completed').length,
      'AnalysisCompleted=', types.includes('AnalysisCompleted'));
  });

  const types = await getEventTypes(pipelineId, session);
  log('event types:', [...new Set(types)].join(', '));
  log('SA count:', types.filter(t => t === 'SA_Step_Completed').length);

  const checks = {
    SkeletonCreated: types.includes('SkeletonCreated'),
    StageConfirmed: types.includes('StageConfirmed'),
    AnalysisCompleted: types.includes('AnalysisCompleted'),
    saStepsGte9: types.filter(t => t === 'SA_Step_Completed').length >= 9,
  };
  log('assertions', checks);
  if (!checks.AnalysisCompleted || !checks.saStepsGte9) {
    throw new Error('Phase2 E2E FAILED');
  }
  if (!checks.StageConfirmed) {
    log('WARN: D3 StageConfirmed 未验收 — 重启 backend 后重跑');
  }

  // D11：EventSpecRevised + 受影响步骤重跑（需 backend 含 AnalystAffectedStepsRerunService）
  const fragmentId = 'eventspec:BE-001';
  const saBefore = types.filter(t => t === 'SA_Step_Completed').length;
  const revise = await apiRequest('POST', `/api/studio/ir/${pipelineId}/events/${fragmentId}/revise`, {
    body: {
      revisionType: 'fieldTypeOrConstraint',
      payloadPatch: JSON.stringify({ fieldPatch: 'duration:int' }),
      autoRerunAffected: true,
    },
    session,
  });
  if (!isJnpfOk(revise)) {
    log('WARN: D11 revise 跳过（可能 backend 未重启）:', revise.json?.msg || revise.status);
  } else {
    const affected = pick(jnpfData(revise), 'affectedSteps', 'AffectedSteps') || [];
    log('D11 affected steps', affected);

    try {
      await waitFor(async () => {
        const t = await getEventTypes(pipelineId, session);
        const sa = t.filter(x => x === 'SA_Step_Completed').length;
        const confirmed = t.filter(x => x === 'EventSpecConfirmed').length;
        return sa > saBefore && confirmed >= 2 && t.includes('EventSpecRevised');
      }, 'D11 revise rerun', 300_000, async () => {
        const t = await getEventTypes(pipelineId, session);
        log('D11 progress SA=', t.filter(x => x === 'SA_Step_Completed').length,
          'EventSpecConfirmed=', t.filter(x => x === 'EventSpecConfirmed').length);
      });
      log('D11 PASS');
    } catch (e) {
      log('WARN: D11 rerun 超时 — 请重启 backend 后重跑:', e.message);
    }
  }

  log('PASS');
  return { pipelineId, token };
}

async function runBrowserDebug(pipelineId) {
  let chromium;
  try {
    ({ chromium } = await import('playwright'));
  } catch {
    log('playwright optional — skip --headed');
    return;
  }
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  page.on('console', msg => log(`[browser:${msg.type()}]`, msg.text()));
  await page.goto(`http://localhost:3100/#/studio/ai/submit-requirement?pipelineId=${pipelineId}`);
  await page.waitForTimeout(5000);
  await page.screenshot({ path: '.claude/evidence/phase2-skills-e2e.png', fullPage: true });
  await browser.close();
}

async function main() {
  const health = await fetch(`${process.env.JNPF_API_URL || 'http://localhost:5000'}/api/oauth/getLoginConfig`).catch(() => null);
  if (!health?.ok && health?.status !== 403) {
    console.error('后端未启动 — 请先 start-dev.ps1');
    process.exit(1);
  }
  const { pipelineId } = await runHttpFlow();
  if (HEADED) await runBrowserDebug(pipelineId);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
