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
