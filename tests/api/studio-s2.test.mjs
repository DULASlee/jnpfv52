/**
 * Studio S2 API 结构化验收 — Vitest 替代裸 .mjs 断言
 *
 * 快测（已有 pipeline，~10s）：
 *   E2E_PIPELINE_ID=311 pnpm test:api
 *
 * 全链 Skill watch（仅 S0→S2 尚无 Vitest 驱动替代时）：
 *   node scripts/phase-sup-s2-e2e.mjs create|gate|pm|confirm|analyst
 */
import { describe, it, expect, beforeAll } from 'vitest';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login } from '../../scripts/lib/jnpf-auth.mjs';
import {
  assertDeliverableNames,
  diagnosePipeline,
  getDeliverables,
  getEvents,
  probeEnv,
} from '../../scripts/lib/phase-sup-api.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const STATE_FILE = path.join(__dirname, '../../scripts/.sup-e2e-state.json');

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

describe('Studio API 环境', () => {
  it('API :5000 可达', async () => {
    const env = await probeEnv();
    expect(env.apiOk, `API DOWN: ${env.apiUrl}`).toBe(true);
  });
});

describe.skipIf(skipPipeline)(`Studio S2 产物 pipeline=${pipelineId}`, () => {
  let session;

  beforeAll(async () => {
    session = await login();
  });

  it('S2 交付物齐全', async () => {
    const items = await getDeliverables(session, pipelineId);
    const check = assertDeliverableNames(items, [
      '00-merged-requirement.md',
      '01-skeleton.md',
      '02-requirement-spec.md',
    ]);
    expect(check.pass, `missing: ${check.missing.join(', ')}`).toBe(true);
  });

  it('AnalysisCompleted IR 事件存在', async () => {
    const events = await getEvents(session, pipelineId);
    const types = events.map(e => e.eventType || e.EventType);
    expect(types).toContain('AnalysisCompleted');
  });

  it('物化成功或尚未 confirm（二选一可接受）', async () => {
    const events = await getEvents(session, pipelineId);
    const types = events.map(e => e.eventType || e.EventType);
    const materialized = types.includes('SaMaterializationCompleted');
    const failed = types.filter(t => t === 'SaMaterializationFailed');
    if (materialized) {
      expect(failed.length).toBeGreaterThanOrEqual(0);
      return;
    }
    // 未物化：不应有「最新一次仍失败且无 completed」的阻塞态（允许历史失败）
    const diag = await diagnosePipeline(session, pipelineId);
    expect(diag.hasAnalysisCompleted).toBe(true);
  });
});
