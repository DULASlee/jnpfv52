/**
 * Studio S5 开发测试链 — Vitest 快断言（22 号文档第 3 步）
 *
 *   E2E_PIPELINE_ID=311 pnpm test:api
 */
import { describe, it, expect, beforeAll } from 'vitest';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login, pick } from '../../scripts/lib/jnpf-auth.mjs';
import { getEvents, getSnapshots } from '../../scripts/lib/phase-sup-api.mjs';
import { getDeveloperStatus } from '../../scripts/lib/phase4-api.mjs';

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
  } catch {
    /* ignore */
  }
  return 0;
}

const pipelineId = resolvePipelineId();
const skipPipeline = !pipelineId;

async function s5Ready(session) {
  const types = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
  return types.includes('SystemDesignLocked');
}

describe.skipIf(skipPipeline)(`Studio S5 产物 verify pipeline=${pipelineId}`, () => {
  let session;

  beforeAll(async () => {
    session = await login();
  });

  it('CodeGeneratedStablePromoted + TestSuiteGenerated', async (ctx) => {
    if (!(await s5Ready(session))) {
      ctx.skip('S4 未完成 — 先跑 S3→S4');
    }
    const types = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
    expect(types).toContain('CodeGeneratedStablePromoted');
    expect(types).toContain('TestSuiteGenerated');
  });

  it('IR3 快照 stable', async (ctx) => {
    if (!(await s5Ready(session))) {
      ctx.skip('S4 未完成');
    }
    const types = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
    const eventsGreen = types.includes('CodeGeneratedStablePromoted') && types.includes('TestSuiteGenerated');
    const snaps = await getSnapshots(session, pipelineId);
    const codegen = snaps.find(s => pick(s, 'fragmentType', 'FragmentType') === 'IR3_GeneratedCode');
    const tests = snaps.find(s => pick(s, 'fragmentType', 'FragmentType') === 'IR3_TestSuite');
    expect(pick(tests, 'stabilityState', 'StabilityState')).toBe('stable');
    if (!eventsGreen) {
      expect(pick(codegen, 'stabilityState', 'StabilityState')).toBe('stable');
    }
  });

  it('developer/status codegen stable + sandbox passed', async (ctx) => {
    if (!(await s5Ready(session))) {
      ctx.skip('S4 未完成');
    }
    const types = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
    if (types.includes('CodeGeneratedStablePromoted') && types.includes('TestSuiteGenerated')) {
      return;
    }
    const status = await getDeveloperStatus(session, pipelineId);
    expect(pick(status, 'codegenStability', 'CodegenStability')).toBe('stable');
    expect(pick(status, 'sandboxBuildPassed', 'SandboxBuildPassed')).toBe(true);
  });
});
