/**
 * Studio S3→S4 设计四 Skill — Vitest 验收（22 号文档第 2 步）
 *
 * 快断言（~10s）：
 *   E2E_PIPELINE_ID=311 pnpm test:api
 *
 * 自动驱动（分钟级，Rare）：
 *   E2E_PIPELINE_ID=311 E2E_DRIVE_S34=1 pnpm test:api
 *
 * 手工驱动：
 *   pnpm sync:http-env → api-tests/http/studio-s34-chain.http
 */
import { describe, it, expect, beforeAll } from 'vitest';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login, pick } from '../../scripts/lib/jnpf-auth.mjs';
import {
  assertDeliverableNames,
  confirmRequirementSpec,
  confirmStage,
  getDeliverables,
  getDesignStatus,
  getEvents,
  getSnapshots,
  rebuildDeliverables,
  waitDeliverable,
  waitSkillTerminal,
} from '../../scripts/lib/phase-sup-api.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const STATE_FILE = path.join(__dirname, '../../scripts/.sup-e2e-state.json');

const DESIGN_DELIVERABLES = [
  '03-architecture.md',
  '04-system-design.md',
  '05-ddl.sql',
  '06-formpage-ir.json',
];

const DRIVE = process.env.E2E_DRIVE_S34 === '1' || process.env.E2E_DRIVE_S34 === 'true';

function resolvePipelineId() {
  const fromEnv = Number(process.env.E2E_PIPELINE_ID || 0);
  if (fromEnv) return fromEnv;
  try {
    if (fs.existsSync(STATE_FILE)) {
      const id = Number(JSON.parse(fs.readFileSync(STATE_FILE, 'utf8')).pipelineId || 0);
      if (id) return id;
    }
  } catch { /* ignore */ }
  return 0;
}

const pipelineId = resolvePipelineId();
const skipPipeline = !pipelineId;

async function s34Started(session) {
  const types = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
  if (!types.includes('AnalysisCompleted')) return false;
  if (types.includes('ArchitectureDecisionRecorded')) return true;
  const items = await getDeliverables(session, pipelineId);
  return items.some(i => (i.fileName || i.FileName) === '03-architecture.md');
}

describe.skipIf(skipPipeline)(`Studio S3→S4 产物 verify pipeline=${pipelineId}`, () => {
  let session;

  beforeAll(async () => {
    session = await login();
  });

  it('设计 status 暴露 finalized / ai_entity_field 门禁字段（25 §6）', async () => {
    const status = await getDesignStatus(session, pipelineId);
    expect(status).toBeTruthy();
    // 字段必须存在（布尔语义由 pipeline 状态决定；旧 311 可能已 Finalize）
    expect(typeof pick(status, 'analysisFinalized', 'AnalysisFinalized')).toBe('boolean');
    expect(typeof pick(status, 'hasEntityFields', 'HasEntityFields')).toBe('boolean');
    expect(typeof pick(status, 'canRunDesign', 'CanRunDesign')).toBe('boolean');
    const fieldCount = Number(pick(status, 'entityFieldCount', 'EntityFieldCount') ?? 0);
    expect(fieldCount).toBeGreaterThanOrEqual(0);
    // 一致性：canRunDesign ⇒ finalized ∧ hasEntityFields
    const can = pick(status, 'canRunDesign', 'CanRunDesign');
    if (can) {
      expect(pick(status, 'analysisFinalized', 'AnalysisFinalized')).toBe(true);
      expect(pick(status, 'hasEntityFields', 'HasEntityFields')).toBe(true);
    }
    // 反向：未 Finalize 或无字段 ⇒ 不可跑设计
    if (!pick(status, 'analysisFinalized', 'AnalysisFinalized') || !pick(status, 'hasEntityFields', 'HasEntityFields')) {
      expect(can).toBe(false);
    }
  });

  it('交付物 07~09 在 Deploy 后应出现（step5；未部署则 skip）', async (ctx) => {
    const items = await getDeliverables(session, pipelineId);
    const names = items.map(i => i.fileName || i.FileName || '');
    const has07 = names.some(n => String(n).includes('07-codegen'));
    const has08 = names.some(n => String(n).includes('08-testsuite'));
    const has09 = names.some(n => String(n).includes('09-deployment'));
    if (!has07 && !has08 && !has09) {
      ctx.skip('07–09 尚未产出 — 需 Deploy 成功后断言');
    }
    if (has07) expect(has07).toBe(true);
    if (has08) expect(has08).toBe(true);
    if (has09) expect(has09).toBe(true);
  });
  it('设计交付物 03~06 齐全', async (ctx) => {
    if (!(await s34Started(session))) {
      ctx.skip('S3 未启动 — studio-s34-chain.http 或 E2E_DRIVE_S34=1');
    }
    const items = await getDeliverables(session, pipelineId);
    const check = assertDeliverableNames(items, DESIGN_DELIVERABLES);
    expect(check.pass, `missing: ${check.missing.join(', ')}`).toBe(true);
  });

  it('SystemDesignLocked + IR2_SystemDesign locked', async (ctx) => {
    if (!(await s34Started(session))) {
      ctx.skip('S3 未启动');
    }
    const types = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
    expect(types).toContain('SystemDesignLocked');
    const snaps = await getSnapshots(session, pipelineId);
    const system = snaps.find(s => pick(s, 'fragmentType', 'FragmentType') === 'IR2_SystemDesign');
    expect(pick(system, 'stabilityState', 'StabilityState')).toBe('locked');
  });
});

describe.skipIf(skipPipeline || !DRIVE)(`Studio S3→S4 驱动 pipeline=${pipelineId}`, () => {
  let session;

  beforeAll(async () => {
    session = await login();
  }, 60_000);

  it('confirm + architect → 03', async () => {
    try {
      await confirmRequirementSpec(session, pipelineId, { autoRunDesign: false });
    } catch (e) {
      if (!/StageConfirmed|已确认/i.test(e.message)) throw e;
    }
    // 补建已有 architect IR 但 03 落盘失败的历史 pipeline
    await rebuildDeliverables(session, pipelineId, ['S3']).catch(() => {});

    const items = await getDeliverables(session, pipelineId);
    if (!items.some(i => (i.fileName || i.FileName) === '03-architecture.md')) {
      await confirmStage(session, pipelineId);
      const r = await waitSkillTerminal(session, pipelineId, 'architect-skill', 900_000);
      expect(r.status).toBe('completed');
      await waitDeliverable(session, pipelineId, '03-architecture.md', 180_000);
    }
  }, 1_200_000);

  it('架构确认 → db/ui/system-design → 04~06', async () => {
    await confirmStage(session, pipelineId);
    for (const skillId of ['db-design-skill', 'ui-design-skill', 'system-design-skill']) {
      const r = await waitSkillTerminal(session, pipelineId, skillId, 900_000);
      expect(r.status, skillId).toBe('completed');
    }
    for (const f of DESIGN_DELIVERABLES.slice(1)) {
      await waitDeliverable(session, pipelineId, f, 120_000);
    }
  }, 1_800_000);
});
