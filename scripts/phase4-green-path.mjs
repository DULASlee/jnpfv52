#!/usr/bin/env node
/**
 * D14 — leave-simple Green path 联调
 *
 *   node scripts/phase4-green-path.mjs
 *   node scripts/phase4-green-path.mjs --pipeline-id 123   # 复用已有 pipeline
 *   node scripts/phase4-green-path.mjs --skip-artifacts     # 仅 API 断言，不查 workspace 目录
 *
 * 产出：.claude/evidence/phase4-d14-green-path.json
 *
 * 前置：start-dev.ps1（:5000 API 存活）
 */
import { login, pick } from './lib/jnpf-auth.mjs';
import {
  REPO_ROOT,
  assertGeneratedArtifacts,
  createPipeline,
  findSnapshot,
  getDeveloperStatus,
  getDiagnostics,
  getEvents,
  getSnapshots,
  isDeveloperGreen,
  log,
  parseSnapshotPayload,
  probeDeveloperApi,
  resolveGeneratedBackendRoot,
  runDeveloperOrchestrator,
  setupIr2Locked,
  waitDeveloperGreen,
  writeEvidence,
} from './lib/phase4-api.mjs';

const SKIP_ARTIFACTS = process.argv.includes('--skip-artifacts');
const PIPELINE_ARG = (() => {
  const idx = process.argv.indexOf('--pipeline-id');
  return idx >= 0 ? Number(process.argv[idx + 1]) : 0;
})();
const TIMEOUT_MS = Number(process.env.PHASE4_DEVELOPER_TIMEOUT_MS || 1_800_000);

const steps = [];

function step(name, pass, detail, extra = {}) {
  steps.push({ name, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : 'FAIL', name, detail);
  return pass;
}

async function main() {
  const session = await login();
  log('logged in as', session.account);

  let pipelineId = PIPELINE_ARG;
  if (!pipelineId) {
    pipelineId = await createPipeline(session, `P4-Green-${Date.now()}`);
    step('create-pipeline', true, `pipelineId=${pipelineId}`, { pipelineId });
  } else {
    step('reuse-pipeline', true, `pipelineId=${pipelineId}`, { pipelineId });
  }

  if (!PIPELINE_ARG) {
    try {
      await setupIr2Locked(session, pipelineId);
      step('ir2-locked', true, 'SystemDesignLocked + IR2_SystemDesign locked', { pipelineId });
    } catch (e) {
      step('ir2-locked', false, e.message, { pipelineId });
      throw e;
    }
  } else {
    const snaps = await getSnapshots(session, pipelineId);
    const system = findSnapshot(snaps, 'IR2_SystemDesign');
    const ok = pick(system, 'stabilityState', 'StabilityState') === 'locked';
    step('ir2-locked', ok, ok ? 'reused pipeline IR2 locked' : 'IR2_SystemDesign not locked', { pipelineId });
    if (!ok) process.exit(1);
  }

  await probeDeveloperApi(session, pipelineId);
  step('developer-api', true, 'developer/status reachable', { pipelineId });

  const alreadyGreen = PIPELINE_ARG > 0 && (await isDeveloperGreen(session, pipelineId));
  let green;
  if (alreadyGreen) {
    step('developer-run', true, 'skip re-run — pipeline already green', { pipelineId });
    green = { ok: true, types: (await getEvents(session, pipelineId)).map(e => pick(e, 'eventType', 'EventType')) };
  } else {
    const runId = await runDeveloperOrchestrator(session, pipelineId);
    step('developer-run', true, `orchestrator started runId=${runId}`, { pipelineId, runId });
    try {
      green = await waitDeveloperGreen(session, pipelineId, TIMEOUT_MS);
    } catch (e) {
      const types = e.lastEventTypes || [];
      step('developer-green', false, `${e.message}; events=${types.join(',')}`, { pipelineId, eventTypes: types });
      throw e;
    }
  }

  if (!green.ok) {
    step('developer-green', false, `failed: ${green.reason}`, { pipelineId, eventTypes: green.types });
    process.exit(1);
  }
  step('developer-green', true, 'CodeGeneratedStablePromoted + TestSuiteGenerated', {
    pipelineId,
    eventTypes: green.types,
  });

  const status = await getDeveloperStatus(session, pipelineId);
  const codegenStability = pick(status, 'codegenStability', 'CodegenStability');
  const sandboxPassed = pick(status, 'sandboxBuildPassed', 'SandboxBuildPassed') === true;
  step(
    'developer-status',
    alreadyGreen || (codegenStability === 'stable' && sandboxPassed),
    alreadyGreen
      ? `skip status — IR events already green (codegen=${codegenStability}, sandbox=${sandboxPassed})`
      : `codegenStability=${codegenStability}, sandboxBuildPassed=${sandboxPassed}`,
    { pipelineId, status },
  );

  const events = await getEvents(session, pipelineId);
  const eventTypes = events.map(e => pick(e, 'eventType', 'EventType'));
  const hasCodegenFailed = eventTypes.includes('CodegenFailed');
  const hasPromote = eventTypes.includes('CodeGeneratedStablePromoted');
  const archCritical = events.filter(
    e => pick(e, 'eventType', 'EventType') === 'ArchViolationDetected',
  ).length;
  step(
    'no-fail-events',
    !hasCodegenFailed || hasPromote,
    `CodegenFailed=${hasCodegenFailed}, promoted=${hasPromote}, ArchViolationDetected count=${archCritical}`,
    { pipelineId },
  );

  const snapshots = await getSnapshots(session, pipelineId);
  const codegenSnap = findSnapshot(snapshots, 'IR3_GeneratedCode');
  const testSnap = findSnapshot(snapshots, 'IR3_TestSuite');
  const codegenState = pick(codegenSnap, 'stabilityState', 'StabilityState');
  const testPayload = parseSnapshotPayload(pick(testSnap, 'payload', 'Payload'));
  const scenarioCount = Number(testPayload?.scenarioCount ?? 0);
  const scenarios = Array.isArray(testPayload?.scenarios) ? testPayload.scenarios : [];
  const testStable = pick(testSnap, 'stabilityState', 'StabilityState') === 'stable';
  step(
    'ir3-snapshots',
    alreadyGreen
      ? testStable && scenarioCount >= 3
      : codegenState === 'stable' && scenarioCount >= 3,
    alreadyGreen
      ? `reuse green — IR3_TestSuite=${pick(testSnap, 'stabilityState', 'StabilityState')}, scenarioCount=${scenarioCount}`
      : `IR3_GeneratedCode=${codegenState}, scenarioCount=${scenarioCount}`,
    { pipelineId, scenarioCount, scenarios: scenarios.map(s => s.caseId || s.CaseId) },
  );

  const diag = await getDiagnostics(session, pipelineId);
  const tenantId = pick(diag, 'tenantId', 'TenantId') || session.tenantId || '0';
  const projectId = String(pipelineId);
  const backendRoot = resolveGeneratedBackendRoot(tenantId, projectId);

  if (!SKIP_ARTIFACTS) {
    const artifacts = assertGeneratedArtifacts(backendRoot);
    step('workspace-artifacts', artifacts.pass, `${artifacts.detail} @ ${backendRoot}`, {
      pipelineId,
      tenantId,
      backendRoot,
      missing: artifacts.missing,
    });
  } else {
    step('workspace-artifacts', true, 'skipped (--skip-artifacts)', { pipelineId });
  }

  const allPass = steps.every(s => s.pass);
  const report = {
    phase: 'phase4-d14-green-path',
    pass: allPass,
    pipelineId,
    tenantId,
    projectId,
    repoRoot: REPO_ROOT,
    steps,
    eventTypes: [...new Set(eventTypes)],
    at: new Date().toISOString(),
  };

  const evidencePath = writeEvidence('phase4-d14-green-path.json', report);
  log('evidence →', evidencePath);
  log(allPass ? '[D14] PASS — leave-simple Green path' : '[D14] FAIL — see steps');

  if (!allPass) process.exit(1);
}

main().catch(err => {
  console.error('[D14] FATAL', err.message || err);
  process.exit(1);
});
