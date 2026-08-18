/**
 * sa-service HTTP 路由集成测试（2026）
 *
 * 工具链：
 *   - supertest  — 内存绑定 Express app，无需启动真实 :3001 端口
 *   - jest.spyOn(globalThis, 'fetch') — 拦截 HttpLlmClient 的 LLM Gateway 调用
 *   - SA_TEST=1  — 防止 server.ts 在 import 时调用 app.listen()
 *
 * 覆盖范围：
 *   GET  /api/sa/health          — 健康检查
 *   POST /sa/run-step            — 单步（参数校验 + 正常路径）
 *   POST /api/sa/run-async       — 新异步主入口（非阻塞返回 taskId）
 *   GET  /api/sa/tasks/:taskId   — 轮询任务状态 + 完整产出
 */

process.env.SA_TEST = '1';
process.env.LLM_GATEWAY_URL = 'http://mock-llm-gateway/api/llm';
process.env.SA_DB_BACKEND = 'inmemory';

import request from 'supertest';
import { app, purgeExpiredTasks } from '../src/server';
import { AUDIT_FIELDS } from '../src/validators/builders';

// ──────────────────────────────────────────────────────────────
// 辅助：构造 fetch mock Response
// ──────────────────────────────────────────────────────────────
function mockFetchResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

/** 根据 LLM 请求 systemPrompt 返回对应步骤的合法产物（让真实 Validator 通过 — C2 注入后必需） */
function buildLlmMock(): ReturnType<typeof vi.spyOn> {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    const url = typeof input === 'string' ? input : (input as Request).url;

    if (!url.includes('mock-llm-gateway')) {
      return Promise.reject(new Error(`Unexpected fetch to: ${url}`));
    }

    const body: any = JSON.parse((init?.body as string) ?? '{}');
    const prompt: string = body?.systemPrompt ?? '';
    const ok = (obj: unknown) => mockFetchResponse({
      code: 200,
      data: { isSuccess: true, content: JSON.stringify(obj) },
    });

    // ScopeAgent — simple 事件
    if (prompt.includes('需求分析师')) {
      return ok({
        systemBoundary: { inScope: ['请假申请'], outOfScope: [] },
        externalEntities: [{ name: '员工', type: 'user', description: '提交请假申请的用户' }],
        businessEvents: [
          { id: 1, name: '提交请假申请', description: '员工提交请假申请', complexity: 'simple' },
        ],
        eventCount: 1,
      });
    }

    // DictAgent — 审计字段（DictValidator checkAuditFields/checkFieldTypes）+ 1 数据流（UI 绑定用）
    if (prompt.includes('数据字典分析师')) {
      return ok({
        elements: AUDIT_FIELDS,
        dataFlows: [{ name: '请假申请数据', fields: AUDIT_FIELDS }],
        dataStores: [],
      });
    }

    // UIAgent — screen 绑定数据流，fields 空（通过 UIValidator 各校验）
    if (prompt.includes('UI 设计分析师')) {
      return ok({
        screens: [{ id: '1', name: '请假申请表单', dataFlow: '请假申请数据', bpmNodeId: 'node-1', fields: [] }],
      });
    }

    // DFDAgent — 空 processes（DFDValidator forEach 空 → passed）
    if (prompt.includes('数据流分析师')) {
      return ok({ contextDiagram: {}, dfdLevels: {}, processes: [], dataFlows: [], dataStores: [] });
    }

    // BPMAgent — 空 activityNodes
    if (prompt.includes('业务流程分析师')) {
      return ok({ swimLanes: [], activityNodes: [], edges: [], exceptionPaths: [], dfdProcessMappings: {} });
    }

    // ERAgent — 空 entities
    if (prompt.includes('数据建模分析师')) {
      return ok({ entities: [], relationships: [] });
    }

    // StateMachineAgent — 空 stateMachines
    if (prompt.includes('状态机分析师')) {
      return ok({ stateMachines: [] });
    }

    // 默认（PSpec/DecisionTable 等，simple 事件不跑）
    return ok({});
  });
}

// ──────────────────────────────────────────────────────────────
// 生命周期
// ──────────────────────────────────────────────────────────────
let fetchMock: ReturnType<typeof vi.spyOn>;

beforeEach(() => {
  fetchMock = buildLlmMock();
});

afterEach(() => {
  fetchMock.mockRestore();
});

// ──────────────────────────────────────────────────────────────
// 1. 健康检查
// ──────────────────────────────────────────────────────────────
describe('GET /api/sa/health', () => {
  it('返回 status:ok', async () => {
    const res = await request(app).get('/api/sa/health');
    expect(res.status).toBe(200);
    expect(res.body.status).toBe('ok');
    expect(res.body.service).toBe('sa-service');
  });
});

// ──────────────────────────────────────────────────────────────
// 2. POST /sa/run-step — 参数校验
// ──────────────────────────────────────────────────────────────
describe('POST /sa/run-step — 参数校验', () => {
  it('缺少 tenantId → 400', async () => {
    const res = await request(app)
      .post('/sa/run-step')
      .send({ projectId: '1', eventId: 'BE-001', agentName: 'ScopeAgent' });
    expect(res.status).toBe(400);
  });

  it('缺少 agentName → 400', async () => {
    const res = await request(app)
      .post('/sa/run-step')
      .send({ tenantId: '0', projectId: '1', eventId: 'BE-001' });
    expect(res.status).toBe(400);
  });
});

// ──────────────────────────────────────────────────────────────
// 3. POST /api/sa/run-async — 非阻塞 + taskId
// ──────────────────────────────────────────────────────────────
describe('POST /api/sa/run-async', () => {
  it('缺少 requirementText → 400', async () => {
    const res = await request(app)
      .post('/api/sa/run-async')
      .send({ tenantId: '0', projectId: 1 });
    expect(res.status).toBe(400);
  });

  it('合法请求立即返回 taskId（<1s，不阻塞等待 SA 完成）', async () => {
    const start = Date.now();

    const res = await request(app)
      .post('/api/sa/run-async')
      .send({ tenantId: '0', projectId: 999, pipelineId: 999, requirementText: '员工请假申请系统', userId: 'test' });

    const elapsed = Date.now() - start;

    expect(res.status).toBe(200);
    expect(typeof res.body.taskId).toBe('string');
    expect(res.body.status).toBe('running');
    // 核心断言：必须立即返回，不能等 SA 跑完
    expect(elapsed).toBeLessThan(1000);
  });

  it('返回的 taskId 可通过 /api/sa/tasks/:taskId 查询', async () => {
    const asyncRes = await request(app)
      .post('/api/sa/run-async')
      .send({ tenantId: '0', projectId: 998, pipelineId: 998, requirementText: '请假系统 v2', userId: 'test' });

    expect(asyncRes.status).toBe(200);
    const { taskId } = asyncRes.body;

    const pollRes = await request(app).get(`/api/sa/tasks/${taskId}`);
    expect(pollRes.status).toBe(200);
    expect(pollRes.body.taskId).toBe(taskId);
    expect(['running', 'completed', 'failed']).toContain(pollRes.body.status);
  });
});

// ──────────────────────────────────────────────────────────────
// 4. GET /api/sa/tasks/:taskId
// ──────────────────────────────────────────────────────────────
describe('GET /api/sa/tasks/:taskId', () => {
  it('不存在的 taskId → 404', async () => {
    const res = await request(app).get('/api/sa/tasks/nonexistent-xyz');
    expect(res.status).toBe(404);
    expect(res.body).toHaveProperty('error');
  });

  it('completed 任务含 result.eventResults 数组', async () => {
    // 1. 发起异步任务
    const asyncRes = await request(app)
      .post('/api/sa/run-async')
      .send({ tenantId: '0', projectId: 997, pipelineId: 997, requirementText: '请假申请完整验证', userId: 'test' });

    const { taskId } = asyncRes.body;

    // 2. 轮询，最多 20s（MSW mock 让 LLM 极快，通常 <3s）
    const deadline = Date.now() + 20_000;
    let lastStatus = '';
    let result: any;

    while (Date.now() < deadline) {
      await new Promise(r => setTimeout(r, 300));
      const pollRes = await request(app).get(`/api/sa/tasks/${taskId}`);
      lastStatus = pollRes.body.status;

      if (lastStatus === 'completed') {
        result = pollRes.body.result;
        break;
      }
      if (lastStatus === 'failed') {
        // 打印错误信息便于调试
        console.error('SA task failed:', pollRes.body.error);
        break;
      }
    }

    expect(lastStatus).toBe('completed');
    expect(result).toBeDefined();
    expect(Array.isArray(result.eventResults)).toBe(true);
    expect(result.eventResults.length).toBeGreaterThan(0);

    // 每个 eventResult 应有 eventId、eventName、steps
    const ev = result.eventResults[0];
    expect(ev.eventId).toBeDefined();
    expect(ev.eventName).toBeTruthy();
    expect(ev.steps).toBeDefined();
  }, 25_000);
});

// r6-safe: vitest 测试轮询 helper，setTimeout 在 await Promise 内立即解析，非前端组件定时器
async function pollTaskStatus(taskId: string, timeoutMs = 20_000): Promise<string> {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    await new Promise(r => setTimeout(r, 300));
    const poll = await request(app).get(`/api/sa/tasks/${taskId}`);
    const s = poll.body.status;
    if (s === 'completed' || s === 'failed') return s;
  }
  return 'timeout';
}

// ──────────────────────────────────────────────────────────────
// 5. runningTasks TTL purge（B 组防内存泄漏）
// ──────────────────────────────────────────────────────────────
describe('runningTasks TTL purge', () => {
  it('completed task 未过期：purgeExpiredTasks() 清 0，task 仍可查', async () => {
    const { taskId } = (await request(app)
      .post('/api/sa/run-async')
      .send({ tenantId: '0', projectId: 996, pipelineId: 996, requirementText: 'purge 未过期', userId: 'test' })).body;
    expect(await pollTaskStatus(taskId)).toBe('completed');

    expect(purgeExpiredTasks()).toBe(0);
    const stillThere = await request(app).get(`/api/sa/tasks/${taskId}`);
    expect(stillThere.status).toBe(200);
  }, 25_000);

  it('completed task 过期：purgeExpiredTasks(future) 清 1 + GET 返回 404', async () => {
    const { taskId } = (await request(app)
      .post('/api/sa/run-async')
      .send({ tenantId: '0', projectId: 995, pipelineId: 995, requirementText: 'purge 过期', userId: 'test' })).body;
    expect(await pollTaskStatus(taskId)).toBe('completed');

    const future = Date.now() + 31 * 60 * 1000;
    // purge 清所有过期 task（含前面测试遗留的 completed task），当前 taskId 必在其列
    const removed = purgeExpiredTasks(future);
    expect(removed).toBeGreaterThanOrEqual(1);

    const after = await request(app).get(`/api/sa/tasks/${taskId}`);
    expect(after.status).toBe(404);
  }, 25_000);
});

// ──────────────────────────────────────────────────────────────
// 6. 真实 Validator 拒绝畸形产出（C3：验证 Validator 真正生效，非 null 占位）
// ──────────────────────────────────────────────────────────────
describe('真实 Validator 拒绝畸形产出（C3）', () => {
  let deformedMock: ReturnType<typeof vi.spyOn>;
  beforeEach(() => {
    // 覆盖外层 buildLlmMock：DFD 返回畸形（process 无 inputFlows → DFD_NO_INPUT）
    deformedMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
      const url = typeof input === 'string' ? input : (input as Request).url;
      if (!url.includes('mock-llm-gateway')) return Promise.reject(new Error('unexpected'));
      const body: any = JSON.parse((init?.body as string) ?? '{}');
      const prompt: string = body?.systemPrompt ?? '';
      const ok = (o: unknown) => mockFetchResponse({ code: 200, data: { isSuccess: true, content: JSON.stringify(o) } });
      if (prompt.includes('需求分析师')) {
        return ok({ systemBoundary: { inScope: ['x'], outOfScope: [] }, externalEntities: [], businessEvents: [{ id: 1, name: 'e', description: 'd', complexity: 'simple' }], eventCount: 1 });
      }
      if (prompt.includes('数据流分析师')) {
        // 畸形：process 无 inputFlows/outputFlows → DFDValidator DFD_NO_INPUT/DFD_NO_OUTPUT
        return ok({ contextDiagram: {}, dfdLevels: {}, processes: [{ id: 'P1', name: 'p', inputFlows: [], outputFlows: [] }], dataFlows: [], dataStores: [] });
      }
      return ok({});
    });
  });
  afterEach(() => { deformedMock.mockRestore(); });

  it('DFD process 无 inputFlow → DFDValidator 拒绝 → retry 5 次 → task failed', async () => {
    const asyncRes = await request(app)
      .post('/api/sa/run-async')
      .send({ tenantId: '0', projectId: 994, pipelineId: 994, requirementText: '畸形 DFD', userId: 'test' });
    const { taskId } = asyncRes.body;
    const status = await pollTaskStatus(taskId);
    expect(status).toBe('failed');
  }, 30_000);
});
