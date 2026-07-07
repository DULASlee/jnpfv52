#!/usr/bin/env node
/**
 * 第 4 步 — 全链 E2E（S0→S6，22 号文档 §6.4）
 *
 *   node scripts/phase5-fullchain-e2e.mjs
 *   node scripts/phase5-fullchain-e2e.mjs --from-step 3   # 从设计阶段起（需 --pipeline-id）
 *   node scripts/phase5-fullchain-e2e.mjs --fast            # 跳过 analyst（simulate IR1）
 *
 * 产出：.claude/evidence/phase5-fullchain-e2e.json
 */
import { login, pick } from './lib/jnpf-auth.mjs';
import {
  assertDeliverableNames,
  confirmSkeleton,
  confirmStage,
  createPipeline,
  getDeliverables,
  getEvents,
  log,
  probeSaService,
  runAnalystSkill,
  runDeploySkill,
  runPmSkill,
  triggerSaGate,
  waitDeliverable,
  waitSkillTerminal,
  writeEvidence,
} from './lib/phase-sup-api.mjs';
import {
  setupIr1Stable,
  setupIr2Locked,
  runDeveloperOrchestrator,
  waitDeveloperGreen,
  getSnapshots,
} from './lib/phase4-api.mjs';

const FAST = process.argv.includes('--fast');
const FROM_STEP = (() => {
  const idx = process.argv.indexOf('--from-step');
  return idx >= 0 ? Number(process.argv[idx + 1]) : 1;
})();
const PIPELINE_ARG = (() => {
  const idx = process.argv.indexOf('--pipeline-id');
  return idx >= 0 ? Number(process.argv[idx + 1]) : 0;
})();

const steps = [];
function step(name, pass, detail, extra = {}) {
  steps.push({ name, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : 'FAIL', name, detail);
  return pass;
}

async function main() {
  const session = await login();
  let pipelineId = PIPELINE_ARG;

  // ── Step 1: S0→S2 ──
  if (FROM_STEP <= 1) {
    if (!pipelineId) {
      pipelineId = await createPipeline(session, `P5-FULL-${Date.now()}`);
      step('create-pipeline', true, `id=${pipelineId}`, { pipelineId });
    }

    if (FROM_STEP === 1 && !PIPELINE_ARG) {
      await triggerSaGate(session, pipelineId, { autoRunPm: false });
      await waitDeliverable(session, pipelineId, '00-merged-requirement.md', 240_000);
      step('step1-gate', true, 'S0 gate passed', { pipelineId });

      await runPmSkill(session, pipelineId);
      const pm = await waitSkillTerminal(session, pipelineId, 'pm-skill', 300_000);
      step('step1-pm', pm.status === 'completed', pm.status, { pipelineId });
      if (pm.status !== 'completed') process.exit(1);

      await confirmSkeleton(session, pipelineId, false);
      step('step1-skeleton', true, 'IR-0 stable', { pipelineId });

      if (FAST || !(await probeSaService())) {
        await setupIr1Stable(session, pipelineId);
        step('step1-analyst', true, 'fast path simulate IR1', { pipelineId, skip: true });
      } else {
        await runAnalystSkill(session, pipelineId);
        const a = await waitSkillTerminal(session, pipelineId, 'analyst-skill', 900_000);
        step('step1-analyst', a.status === 'completed', a.status, { pipelineId });
        if (a.status !== 'completed') process.exit(1);
        await waitDeliverable(session, pipelineId, '02-requirement-spec.md', 120_000);
      }
    }
  }

  if (!pipelineId) throw new Error('--pipeline-id required when --from-step > 1');

  // ── Step 2: S3→S4 ──
  if (FROM_STEP <= 2) {
    await confirmStage(session, pipelineId);
    await waitSkillTerminal(session, pipelineId, 'architect-skill', 600_000);
    step('step2-architect', true, 'architect done', { pipelineId });

    await confirmStage(session, pipelineId);
    for (const id of ['db-design-skill', 'ui-design-skill', 'system-design-skill']) {
      await waitSkillTerminal(session, pipelineId, id, 600_000);
    }
    const snaps = await getSnapshots(session, pipelineId);
    const sys = snaps.find(s => pick(s, 'fragmentType', 'FragmentType') === 'IR2_SystemDesign');
    step('step2-design', pick(sys, 'stabilityState', 'StabilityState') === 'locked', 'IR2 locked', { pipelineId });
  }

  // ── Step 3: S5 developer ──
  if (FROM_STEP <= 3) {
    if (FROM_STEP === 3 && PIPELINE_ARG) {
      const snaps = await getSnapshots(session, pipelineId);
      const sys = snaps.find(s => pick(s, 'fragmentType', 'FragmentType') === 'IR2_SystemDesign');
      if (pick(sys, 'stabilityState', 'StabilityState') !== 'locked') {
        await setupIr2Locked(session, pipelineId);
      }
    } else {
      await confirmStage(session, pipelineId);
    }

    await runDeveloperOrchestrator(session, pipelineId);
    const green = await waitDeveloperGreen(session, pipelineId, 1_800_000);
    step('step3-developer', green.pass, green.detail, { pipelineId });
    if (!green.pass) process.exit(1);
  }

  // ── Step 4: deploy-skill ──
  if (FROM_STEP <= 4) {
    await runDeploySkill(session, pipelineId);
    const dep = await waitSkillTerminal(session, pipelineId, 'deploy-skill', 600_000);
    step('step4-deploy', dep.status === 'completed', dep.status, { pipelineId, error: dep.error });

    const events = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
    step('step4-deployment-event', events.includes('DeploymentVerified'), 'DeploymentVerified', { pipelineId });
  }

  // ── Step 5: deliverables 07~09 ──
  const items = await getDeliverables(session, pipelineId);
  const core = [
    '01-skeleton.md',
    '03-architecture.md',
    '04-system-design.md',
    '07-codegen-manifest.json',
    '08-testsuite.json',
    '09-deployment-report.md',
  ];
  const check = assertDeliverableNames(items, core);
  step('step5-deliverables', check.pass, check.pass ? '07~09 indexed' : `missing ${check.missing}`, {
    pipelineId,
    names: check.names,
  });

  const report = { pipelineId, steps, pass: steps.every(s => s.pass), fast: FAST, fromStep: FROM_STEP };
  writeEvidence('phase5-fullchain-e2e.json', report);
  process.exit(report.pass ? 0 : 1);
}

main().catch(e => {
  console.error(e);
  writeEvidence('phase5-fullchain-e2e.json', { pass: false, error: e.message, steps });
  process.exit(1);
});
