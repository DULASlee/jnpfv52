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
import { app } from '../src/server';

// ──────────────────────────────────────────────────────────────
// 辅助：构造 fetch mock Response
// ──────────────────────────────────────────────────────────────
function mockFetchResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

/** 根据 LLM 请求 systemPrompt 返回对应步骤产物 */
function buildLlmMock(): ReturnType<typeof vi.spyOn> {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    const url = typeof input === 'string' ? input : (input as Request).url;

    // 只 mock LLM Gateway 调用，其他 fetch 直接 pass（如果有）
    if (!url.includes('mock-llm-gateway')) {
      return Promise.reject(new Error(`Unexpected fetch to: ${url}`));
    }

    const body: any = JSON.parse((init?.body as string) ?? '{}');
    const prompt: string = body?.systemPrompt ?? '';

    // ScopeAgent
    if (prompt.includes('系统边界') || prompt.includes('需求分析') || prompt.includes('businessEvents')) {
      return mockFetchResponse({
        code: 200,
        data: {
          isSuccess: true,
          content: JSON.stringify({
            systemBoundary: { inScope: ['请假申请'], outOfScope: [] },
            externalEntities: [{ name: '员工', type: 'user', description: '提交请假申请的用户' }],
            businessEvents: [
              { id: 1, name: '提交请假申请', description: '员工提交请假申请', complexity: 'simple' },
            ],
            eventCount: 1,
          }),
        },
      });
    }

    // UIAgent
    if (prompt.includes('UI 原型') || prompt.includes('UIAgent') || prompt.includes('screens')) {
      return mockFetchResponse({
        code: 200,
        data: {
          isSuccess: true,
          content: JSON.stringify({
            screens: [{
              id: '1', name: '请假申请表单', dataFlow: '请假申请数据', bpmNodeId: 'node-1',
              fields: [{ name: 'leave_type', type: 'NVARCHAR', required: true, controlType: 'Select' }],
            }],
          }),
        },
      });
    }

    // 其他步骤（DFD/BPM/Dict/ER/STD/PSpec/DecisionTable）→ 空产物
    return mockFetchResponse({
      code: 200,
      data: { isSuccess: true, content: '{}' },
    });
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
      .send({ tenantId: '0', projectId: 999, requirementText: '员工请假申请系统', userId: 'test' });

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
      .send({ tenantId: '0', projectId: 998, requirementText: '请假系统 v2', userId: 'test' });

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
      .send({ tenantId: '0', projectId: 997, requirementText: '请假申请完整验证', userId: 'test' });

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
