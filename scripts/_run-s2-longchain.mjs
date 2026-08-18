/**
 * 临时长链驱动：gate → pm → confirm-skeleton → analyst → confirm-requirement-spec
 * 用法：node scripts/_run-s2-longchain.mjs [--pipeline-id 341] [--from gate|pm|confirm|analyst|materialize|status]
 */
import { login, apiRequest, jnpfData, pick } from './lib/jnpf-auth.mjs';
import {
  triggerSaGate,
  waitDeliverable,
  watchSkillTerminal,
  runPmSkill,
  confirmSkeleton,
  runAnalystSkill,
  confirmRequirementSpec,
  getDeliverables,
  getEvents,
  getSkillRuns,
  diagnosePipeline,
  printDiagnose,
  probeEnv,
  saveState,
  writeEvidence,
  assertDeliverableNames,
  log,
  warn,
} from './lib/phase-sup-api.mjs';

const args = process.argv.slice(2);
function arg(name, fallback) {
  const i = args.indexOf(name);
  if (i >= 0 && args[i + 1]) return args[i + 1];
  return fallback;
}

const pipelineId = Number(arg('--pipeline-id', process.env.E2E_PIPELINE_ID || 341));
const from = arg('--from', 'gate');

/**
 * 中等业务复杂度（紧凑版）：4 核心事件 + 2 角色审批。
 * 说明：差旅报销 6 事件曾导致 deepseek-v4-pro thinking 打满 MaxTokens=4096，
 * JSON 截断 → PM ToT「businessEvents 校验失败」。本版控制实体/字段规模。
 */
const USER_TEXT = `
员工请假与加班管理系统。

角色：员工、部门主管、HR专员。

业务事件：
1. 请假申请：员工提交请假类型（年假/事假/病假）、起止时间、事由；提交后进入待审批。
2. 请假审批：部门主管通过或驳回；驳回须填写意见；通过后扣减对应假期余额。
3. 加班申请：员工提交加班日期、时长、事由；主管审批通过后计入可调休时长。
4. 假期余额查询：员工与 HR 可查看年假/调休余额；HR 可手工调整并留痕。

约束：请假时长不得超过可用余额；同一自然日不可同时存在未结案的请假与加班单。
`.trim();

const STEPS = ['status', 'gate', 'pm', 'confirm', 'analyst', 'materialize', 'verify'];

function shouldRun(step) {
  if (from === 'status') return step === 'status';
  if (from === 'verify') return step === 'verify' || step === 'status';
  const idx = STEPS.indexOf(from);
  const cur = STEPS.indexOf(step);
  if (idx < 0 || cur < 0) return true;
  return cur >= idx;
}

async function printStatus(session, id) {
  const [items, runs, events, detailRes] = await Promise.all([
    getDeliverables(session, id),
    getSkillRuns(session, id),
    getEvents(session, id),
    apiRequest('GET', `/api/studio/pipeline/execute/${id}`, { session }),
  ]);
  const d = jnpfData(detailRes) || detailRes.json || {};
  log('pipeline', id, {
    name: pick(d, 'name', 'Name'),
    stage: pick(d, 'stage', 'Stage', 'currentStage', 'CurrentStage'),
    status: pick(d, 'status', 'Status'),
    workMode: pick(d, 'workMode', 'WorkMode'),
    projectId: pick(d, 'projectId', 'ProjectId'),
  });
  log('deliverables:', items.map(i => i.fileName || i.FileName).join(', ') || '(none)');
  log('runs:', runs.map(r => `${pick(r, 'skillId', 'SkillId')}:${pick(r, 'status', 'Status')}`).join(' | ') || '(none)');
  log('events:', events.length, 'recent:', events.slice(0, 12).map(e => e.eventType || e.EventType).join(' → '));
  return { items, runs, events, detail: d };
}

async function main() {
  log('probe env…');
  const env = await probeEnv();
  log('env', env);
  if (!env.apiOk) throw new Error('API :5000 不可达');

  const session = await login();
  saveState({ pipelineId, name: `longchain-${pipelineId}` });
  log(`pipeline=${pipelineId} from=${from}`);

  if (shouldRun('status')) {
    await printStatus(session, pipelineId);
    if (from === 'status') return;
  }

  if (shouldRun('gate')) {
    log('── STEP gate: sa-gate ──');
    await triggerSaGate(session, pipelineId, {
      autoRunPm: true,
      userText: USER_TEXT,
    });
    await waitDeliverable(session, pipelineId, '00-merged-requirement.md', 300_000);
    log('gate OK → 00-merged-requirement.md');
  }

  if (shouldRun('pm')) {
    log('── STEP pm: watch pm-skill ──');
    // sa-gate autoRunPm=true 时可能已在跑；失败/无 run 则手动触发
    const runs = await getSkillRuns(session, pipelineId);
    const pm = runs.find(r => pick(r, 'skillId', 'SkillId') === 'pm-skill');
    const pmStatus = pm ? pick(pm, 'status', 'Status') : '';
    if (pmStatus === 'completed') {
      log('pm already completed, skip run');
    } else if (!pm || pmStatus === 'failed' || pmStatus === 'cancelled') {
      warn(`pm previous=${pmStatus || 'none'}, run`);
      await runPmSkill(session, pipelineId);
      await watchSkillTerminal(session, pipelineId, 'pm-skill', { timeoutMs: 300_000 });
    } else {
      log(`pm already ${pmStatus}, watching…`);
      await watchSkillTerminal(session, pipelineId, 'pm-skill', { timeoutMs: 300_000 });
    }
    await waitDeliverable(session, pipelineId, '01-skeleton.md', 120_000);
    log('pm OK → 01-skeleton.md');
  }

  if (shouldRun('confirm')) {
    log('── STEP confirm: confirm-skeleton (autoRunAnalyst=true) ──');
    await confirmSkeleton(session, pipelineId, true);
    log('confirm-skeleton OK');
  }

  if (shouldRun('analyst')) {
    log('── STEP analyst: watch analyst-skill ──');
    const runs = await getSkillRuns(session, pipelineId);
    const an = runs.find(r => pick(r, 'skillId', 'SkillId') === 'analyst-skill');
    const st = an ? pick(an, 'status', 'Status') : '';
    if (!an || st === 'failed' || st === 'cancelled') {
      warn(`analyst previous=${st || 'none'}, run`);
      await runAnalystSkill(session, pipelineId);
    } else if (st === 'completed') {
      log('analyst already completed');
    } else {
      log(`analyst ${st || 'pending'}, watching…`);
    }
    if (st !== 'completed') {
      await watchSkillTerminal(session, pipelineId, 'analyst-skill', {
        timeoutMs: 600_000,
        stallSec: 240,
      });
    }
    await waitDeliverable(session, pipelineId, '02-requirement-spec.md', 180_000);
    log('analyst OK → 02-requirement-spec.md');
  }

  if (shouldRun('materialize')) {
    log('── STEP materialize: confirm-requirement-spec ──');
    const events = await getEvents(session, pipelineId);
    const types = events.map(e => e.eventType || e.EventType);
    if (types.includes('SaMaterializationCompleted')) {
      log('already materialized, skip');
    } else {
      const result = await confirmRequirementSpec(session, pipelineId, { autoRunDesign: false });
      log('confirm-requirement-spec →', JSON.stringify(result)?.slice(0, 300));
      // 等物化 IR
      const deadline = Date.now() + 180_000;
      while (Date.now() < deadline) {
        const ev = await getEvents(session, pipelineId);
        const t = ev.map(e => e.eventType || e.EventType);
        if (t.includes('SaMaterializationCompleted')) {
          log('SaMaterializationCompleted ✓');
          break;
        }
        if (t.includes('SaMaterializationFailed')) {
          const fail = ev.find(e => (e.eventType || e.EventType) === 'SaMaterializationFailed');
          throw new Error(`物化失败: ${(fail?.payloadPreview || fail?.PayloadPreview || '').slice(0, 300)}`);
        }
        await new Promise(r => setTimeout(r, 3000));
      }
    }
  }

  if (shouldRun('verify') || true) {
    log('── VERIFY ──');
    const { items, events } = await printStatus(session, pipelineId);
    const check = assertDeliverableNames(items, [
      '00-merged-requirement.md',
      '01-skeleton.md',
      '02-requirement-spec.md',
    ]);
    const types = events.map(e => e.eventType || e.EventType);
    const diag = await diagnosePipeline(session, pipelineId);
    printDiagnose(diag);
    const evidence = {
      pipelineId,
      at: new Date().toISOString(),
      deliverables: check,
      hasAnalysisCompleted: types.includes('AnalysisCompleted'),
      hasMaterialized: types.includes('SaMaterializationCompleted'),
      eventTypes: types.slice(0, 40),
      diag,
    };
    const path = writeEvidence(`s2-longchain-${pipelineId}.json`, evidence);
    log('evidence →', path);
    if (!check.pass) throw new Error(`交付物缺失: ${check.missing.join(', ')}`);
    if (!types.includes('AnalysisCompleted')) throw new Error('缺少 AnalysisCompleted');
    log('LONGCHAIN PASS');
  }
}

main().catch(err => {
  console.error('[sup-e2e:FAIL]', err.message || err);
  process.exit(1);
});
