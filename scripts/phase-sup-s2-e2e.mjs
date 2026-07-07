#!/usr/bin/env node
/**
 * S0→S2 分步验收 — 禁止一次性傻等 15 分钟
 *
 * 用法（每步单独跑，看清输出再进下一步）：
 *   node scripts/phase-sup-s2-e2e.mjs probe
 *   node scripts/phase-sup-s2-e2e.mjs create
 *   node scripts/phase-sup-s2-e2e.mjs gate
 *   node scripts/phase-sup-s2-e2e.mjs pm
 *   node scripts/phase-sup-s2-e2e.mjs confirm
 *   node scripts/phase-sup-s2-e2e.mjs analyst          # compile 模式：不需 sa-service（agent 模式才需 :3001）
 *   node scripts/phase-sup-s2-e2e.mjs verify
 *   node scripts/phase-sup-s2-e2e.mjs materialize-wait # 待纳入标准 E2E（ADR-004）
 *   node scripts/phase-sup-s2-e2e.mjs diagnose         # 随时 dump 运行时状态
 *
 * 等价手工命令见每步末尾 hint（jnpf-api.mjs）。
 *
 * ★ 日常快断言（已有 pipeline，~10s）：E2E_PIPELINE_ID=<id> pnpm test:api
 *   本脚本定位：Skill watch / 新建 pipeline / evidence JSON — 见 openspec/specs/studio-e2e-toolchain/spec.md
 *
 * 状态文件：scripts/.sup-e2e-state.json（create 写入 pipelineId）
 * 环境变量：E2E_PIPELINE_ID 可覆盖 state
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login } from './lib/jnpf-auth.mjs';
import {
  assertDeliverableNames,
  confirmSkeleton,
  createPipeline,
  diagnosePipeline,
  getDeliverables,
  getEvents,
  loadState,
  log,
  printDiagnose,
  probeEnv,
  probeSaService,
  resolvePipelineId,
  runAnalystSkill,
  runPmSkill,
  saveState,
  triggerSaGate,
  uploadAnnexFile,
  waitDeliverable,
  watchSkillTerminal,
  writeEvidence,
} from './lib/phase-sup-api.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const FIXTURE = path.join(__dirname, 'fixtures', 'step1-leave-requirement.txt');
const cmd = process.argv[2] || 'help';

function arg(name) {
  const idx = process.argv.indexOf(name);
  return idx >= 0 ? process.argv[idx + 1] : undefined;
}

function usage() {
  console.log(`
S0→S2 分步验收（每步独立，带心跳与 failed 快速失败）

  probe      检查 :5000 API 与 :3001 sa-service
  create     创建 pipeline（写入 scripts/.sup-e2e-state.json）
  gate       触发 SA 门控并等 00-merged-requirement.md
  pm         跑 pm-skill 并 watch 至 completed/failed
  confirm    confirm-skeleton（不自动跑 analyst）
  analyst    跑 analyst-skill 并 watch（需 sa-service UP）
  verify     检查 deliverables + AnalysisCompleted
  diagnose   打印当前 pipeline 运行时快照
  all        连续跑全程（仍带心跳；不推荐首次排查时用）

选项：
  --pipeline-id N     指定 pipeline（默认读 state 文件）
  --skip-analyst      all 模式下 sa 不可用时跳过 analyst（标记 SKIP 非 PASS）

状态：${fs.existsSync(path.join(__dirname, '.sup-e2e-state.json')) ? JSON.stringify(loadState()) : '(无，先 create)'}
`);
}

async function stepProbe() {
  const env = await probeEnv();
  log('API :5000', env.apiOk ? 'OK' : 'DOWN', env.apiUrl);
  log('SA  :3001', env.saUp ? 'OK' : 'DOWN', env.saUrl);
  if (!env.apiOk) process.exit(1);
  if (!env.saUp) {
    log('hint: analyst 步骤需要 sa-service；PM/门控不需要');
  }
}

async function stepCreate(session) {
  const requirementText = fs.existsSync(FIXTURE) ? fs.readFileSync(FIXTURE, 'utf8') : undefined;
  const pipelineId = await createPipeline(session, `SUP-S2-${Date.now()}`, requirementText);
  log('pipelineId =', pipelineId);
  log('hint: node scripts/jnpf-api.mjs POST /api/studio/pipeline/execute/create \'{"name":"test","userRequirement":"..."}\'');

  if (fs.existsSync(FIXTURE)) {
    const up = await uploadAnnexFile(session, FIXTURE);
    saveState({ attachment: up });
    log('attachment uploaded:', up.name);
  }
  return pipelineId;
}

async function stepGate(session, pipelineId) {
  const state = loadState();
  await triggerSaGate(session, pipelineId, {
    autoRunPm: false,
    attachments: state.attachment ? [state.attachment] : [],
  });
  log('sa-gate triggered, waiting 00-merged-requirement.md …');
  log('hint: node scripts/jnpf-api.mjs GET /api/studio/pipeline/execute/' + pipelineId + '/deliverables');
  await waitDeliverable(session, pipelineId, '00-merged-requirement.md', 240_000);
  log('OK 00-merged-requirement.md');
}

async function stepPm(session, pipelineId) {
  await runPmSkill(session, pipelineId);
  log('pm-skill started, watching …');
  log('hint: node scripts/jnpf-api.mjs GET /api/studio/skills/' + pipelineId + '/runs');
  const result = await watchSkillTerminal(session, pipelineId, 'pm-skill', { timeoutMs: 300_000 });
  if (result.status !== 'completed') process.exit(1);
  await waitDeliverable(session, pipelineId, '01-skeleton.md', 60_000);
  log('OK 01-skeleton.md');
}

async function stepConfirm(session, pipelineId) {
  await confirmSkeleton(session, pipelineId, false);
  log('OK skeleton confirmed (IR-0 stable)');
  log('hint: node scripts/jnpf-api.mjs POST /api/studio/skills/pm/' + pipelineId + '/confirm-skeleton \'{"autoRunAnalyst":false}\'');
}

async function stepAnalyst(session, pipelineId) {
  // compile 模式不依赖 sa-service；confirm 物化由 C# SaMaterializer 直连主库
  const saUp = await probeSaService();
  if (!saUp) {
    log('WARN sa-service :3001 未就绪 — compile 模式 analyst 仍可跑（物化由 C# SaMaterializer，无需 sa-service）');
  }
  await runAnalystSkill(session, pipelineId);
  log('analyst-skill started, watching (heartbeat 15s) …');
  log('hint: node scripts/jnpf-api.mjs GET /api/studio/ir/' + pipelineId + '/events');
  const result = await watchSkillTerminal(session, pipelineId, 'analyst-skill', {
    timeoutMs: 120_000,
    stallSec: 60,
  });
  if (result.status !== 'completed') process.exit(1);
  await waitDeliverable(session, pipelineId, '02-requirement-spec.md', 120_000);
  log('OK 02-requirement-spec.md');
  const events = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
  if (!events.includes('AnalysisCompleted')) {
    log('WARN: AnalysisCompleted 事件未出现');
  } else {
    log('OK AnalysisCompleted');
  }
}

async function stepVerify(session, pipelineId) {
  const items = await getDeliverables(session, pipelineId);
  const expected = ['00-merged-requirement.md', '01-skeleton.md', '02-requirement-spec.md'];
  const check = assertDeliverableNames(items, expected);
  log('deliverables:', check.names.join(', '));
  if (!check.pass) {
    log('FAIL missing:', check.missing.join(', '));
    process.exit(1);
  }
  const diag = await diagnosePipeline(session, pipelineId);
  printDiagnose(diag);
  writeEvidence('phase-sup-s2-e2e.json', { pipelineId, pass: true, steps: ['verify'], diag });
  log('PASS verify');
}

async function stepDiagnose(session, pipelineId) {
  const diag = await diagnosePipeline(session, pipelineId);
  printDiagnose(diag);
  const p = writeEvidence(`diagnose-pipeline-${pipelineId}.json`, diag);
  log('snapshot →', p);
}

async function main() {
  if (cmd === 'help' || cmd === '-h' || cmd === '--help') {
    usage();
    return;
  }

  const session = cmd === 'probe' ? null : await login();
  if (cmd === 'probe') {
    await stepProbe();
    return;
  }

  const pipelineId = ['create'].includes(cmd) ? 0 : resolvePipelineId(arg('--pipeline-id'));

  switch (cmd) {
    case 'create':
      await stepCreate(session);
      break;
    case 'gate':
      await stepGate(session, pipelineId);
      break;
    case 'pm':
      await stepPm(session, pipelineId);
      break;
    case 'confirm':
      await stepConfirm(session, pipelineId);
      break;
    case 'analyst':
      await stepAnalyst(session, pipelineId);
      break;
    case 'verify':
      await stepVerify(session, pipelineId);
      break;
    case 'diagnose':
      await stepDiagnose(session, pipelineId);
      break;
    case 'all': {
      await stepProbe();
      const pid = await stepCreate(session);
      await stepGate(session, pid);
      await stepPm(session, pid);
      await stepConfirm(session, pid);
      const skip = process.argv.includes('--skip-analyst');
      const saUp = await probeSaService();
      if (saUp && !skip) {
        await stepAnalyst(session, pid);
        await stepVerify(session, pid);
      } else {
        log(skip ? 'SKIP analyst (--skip-analyst)' : 'SKIP analyst (sa-service DOWN)');
        process.exit(1);
      }
      break;
    }
    default:
      usage();
      process.exit(1);
  }
}

main().catch(e => {
  console.error('[sup-e2e] FATAL', e.message);
  process.exit(1);
});
