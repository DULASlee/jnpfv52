// sa-service/src/server.ts
// SA Service HTTP 入口 — 接通 Pipeline Engine 的桥梁

import express from 'express';
import cors from 'cors';
import { v4 as uuidv4 } from 'uuid';
import { InMemorySADatabase } from './orchestrator/SADatabase';
import { SqlServerSADatabase } from './orchestrator/SqlServerSADatabase';
import { ILLMClient, SARequest, SAOutput, SAConfig, DEFAULT_SA_CONFIG } from './orchestrator/orchestrator-types';
import { logStep } from './lib/structuredLogger';
import { tenantSessionStore } from './storage/TenantScopedSessionStore';

// ═══════════════════════════════════════════════════════
// 1. LLM 客户端适配器（桥接后端 ILlmGatewayService）
// ═══════════════════════════════════════════════════════
class HttpLlmClient implements ILLMClient {
  constructor(
    private gatewayUrl: string,
    private apiKey?: string,
    private tenantId?: string,
    private authHeader?: string,
    private providerCode: string = 'deepseek',
    private defaultTemperature: number = 0.3,
    private defaultMaxTokens: number = 4096,
  ) {}

  async generate(params: {
    systemPrompt: string;
    context: Record<string, any>;
    lastErrors?: string[];
    temperature?: number;
  }): Promise<any> {
    const enrichedContext = { ...params.context };
    if (params.lastErrors && params.lastErrors.length > 0) {
      enrichedContext._lastErrors = params.lastErrors;
    }

    // 构造对齐后端 ChatCompletionRequest 的请求体
    const body = {
      providerCode: this.providerCode,
      systemPrompt: params.systemPrompt,
      messages: [
        { role: 'user', content: JSON.stringify(enrichedContext) }
      ],
      temperature: params.temperature ?? this.defaultTemperature,
      maxTokens: this.defaultMaxTokens,
    };

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (this.apiKey) {
      headers['Authorization'] = `Bearer ${this.apiKey}`;
    }
    if (this.tenantId) {
      const headerTenant = this.tenantId === 'default' ? '1' : this.tenantId;
      headers['X-Tenant-Id'] = headerTenant;
    }
    // authHeader 优先级高于 apiKey（从上游请求透传）
    if (this.authHeader) {
      if (this.apiKey) {
        logStep({ level: 'info', message: 'authHeader 覆盖 apiKey，使用上游鉴权信息' });
      }
      headers['Authorization'] = this.authHeader;
    }

    const response = await fetch(this.gatewayUrl, {
      method: 'POST',
      headers,
      body: JSON.stringify(body),
      signal: AbortSignal.timeout(90_000),  // 90s 单次 LLM 硬上限，防止无限挂起
    });

    if (!response.ok) {
      throw new Error(`LLM Gateway 返回 ${response.status}: ${await response.text()}`);
    }

    const raw: any = await response.json();
    // JNPF RESTfulResult 包装：{ code, data: { content, isSuccess, error } }
    const payload = raw?.data ?? raw;
    if (payload?.isSuccess === false || payload?.IsSuccess === false) {
      const err = payload?.error ?? payload?.Error ?? raw?.msg ?? 'LLM Gateway 返回失败';
      throw new Error(String(err));
    }

    const content =
      payload?.content ??
      payload?.Content ??
      raw?.content ??
      raw?.choices?.[0]?.message?.content ??
      raw?.result ??
      payload;

    // 尝试解析 JSON（LLM 可能返回 markdown code block）
    if (typeof content === 'string') {
      const jsonMatch = content.match(/```(?:json)?\s*([\s\S]*?)```/) || [null, content];
      try {
        return JSON.parse(jsonMatch[1]!.trim());
      } catch {
        return content;
      }
    }
    return content;
  }
}

// ═══════════════════════════════════════════════════════
// 2. SSE 事件管理器
// ═══════════════════════════════════════════════════════
interface SSESession {
  id: string;
  res: express.Response;
  createdAt: Date;
}

class SSEManager {
  private sessions = new Map<string, SSESession>();

  createSession(res: express.Response): string {
    const id = uuidv4();
    const session: SSESession = { id, res, createdAt: new Date() };
    this.sessions.set(id, session);

    res.setHeader('Content-Type', 'text/event-stream');
    res.setHeader('Cache-Control', 'no-cache');
    res.setHeader('Connection', 'keep-alive');
    res.setHeader('X-Session-Id', id);

    this.send(id, 'connected', { sessionId: id });
    return id;
  }

  send(sessionId: string, event: string, data: any): void {
    const session = this.sessions.get(sessionId);
    if (!session) return;
    try {
      session.res.write(`event: ${event}\ndata: ${JSON.stringify(data)}\n\n`);
    } catch {
      this.sessions.delete(sessionId);
    }
  }

  close(sessionId: string): void {
    const session = this.sessions.get(sessionId);
    if (session) {
      try { session.res.end(); } catch {}
      this.sessions.delete(sessionId);
    }
  }

  getSessionCount(): number {
    return this.sessions.size;
  }
}

// ═══════════════════════════════════════════════════════
// 3. Express 服务器
// ═══════════════════════════════════════════════════════
const app = express();
app.use(cors());
app.use(express.json({ limit: '10mb' }));

const PORT = parseInt(process.env.SA_SERVICE_PORT || '3001', 10);
// LLM Gateway 配置（全部从环境变量读取，无硬编码回退）
const LLM_GATEWAY_URL = process.env.LLM_GATEWAY_URL || '';
const LLM_API_KEY = process.env.LLM_API_KEY || '';
const LLM_PROVIDER = process.env.LLM_PROVIDER || 'deepseek';
const LLM_TEMPERATURE = parseFloat(process.env.LLM_TEMPERATURE || '0.3');
const LLM_MAX_TOKENS = parseInt(process.env.LLM_MAX_TOKENS || '4096', 10);
// DB 后端：'inmemory' | 'sqlserver'（默认 inmemory，生产需设 sqlserver）
const DB_BACKEND = (process.env.SA_DB_BACKEND || 'inmemory').toLowerCase();

const sseManager = new SSEManager();
setInterval(() => tenantSessionStore.purgeExpired(), 5 * 60 * 1000);

// 运行中的任务追踪（含完整 SA 产出，供 C# 轮询 /api/sa/tasks/:taskId 获取）
interface SATask {
  status: 'running' | 'completed' | 'failed';
  result?: SAOutput;
  error?: string;
  startedAt: Date;
  completedAt?: Date;
}
const runningTasks = new Map<string, SATask>();

// ═══════════════════════════════════════════════════════
// 健康检查
// ═══════════════════════════════════════════════════════
app.get('/api/sa/health', (_req, res) => {
  res.json({
    status: 'ok',
    service: 'sa-service',
    version: '1.0.0',
    uptime: process.uptime(),
    runningTasks: runningTasks.size,
    sseSessions: sseManager.getSessionCount(),
  });
});

// POST /sa/run-step — 单步 SA（C# 适配层驱动）
// ═══════════════════════════════════════════════════════
app.post('/sa/run-step', async (req, res) => {
  const started = Date.now();
  try {
    const {
      tenantId, projectId, eventId, agentName, irStepName,
      requirementText, skeleton, previousSteps,
    } = req.body;

    if (!tenantId || !projectId || !eventId || !agentName) {
      return res.status(400).json({ error: '缺少 tenantId/projectId/eventId/agentName' });
    }

    const runId = req.headers['x-skill-run-id'] as string | undefined;
    const stepName = irStepName || agentName;
    const sessionKey = tenantSessionStore.buildKey(String(tenantId), String(projectId), String(eventId), stepName);
    tenantSessionStore.set(sessionKey, {
      tenantId: String(tenantId),
      projectId: String(projectId),
      eventId: String(eventId),
      stepName,
      runId,
      startedAt: started,
    });

    logStep({
      level: 'info',
      runId,
      tenantId: String(tenantId),
      projectId: String(projectId),
      eventId: String(eventId),
      stepName,
      message: 'run-step started',
    });

    const authHeader = req.headers.authorization || '';
    const llm = new HttpLlmClient(
      LLM_GATEWAY_URL, LLM_API_KEY, tenantId, authHeader,
      LLM_PROVIDER, LLM_TEMPERATURE, LLM_MAX_TOKENS,
    );
    const db = createDatabase(DB_BACKEND);
    const validators = {
      DFDValidator: null, BPMValidator: null, DictValidator: null,
      LogicValidator: null, CrossEventConsistencyValidator: null,
      ERValidator: null, STDValidator: null, UIValidator: null,
    };
    const orchestrator = new SAOrchestrator(llm, db, validators);

    const output = await orchestrator.runSingleStep({
      tenantId,
      projectId: String(projectId),
      eventId: String(eventId),
      agentName,
      irStepName: stepName,
      requirementText: requirementText || '',
      skeleton,
      previousSteps: previousSteps || {},
      runId,
    });

    tenantSessionStore.markCompleted(sessionKey);
    logStep({
      level: 'info',
      runId,
      tenantId: String(tenantId),
      projectId: String(projectId),
      eventId: String(eventId),
      stepName,
      elapsedMs: Date.now() - started,
      message: 'run-step completed',
    });

    res.json({ output, durationMs: Date.now() - started, agentName, irStepName: stepName });
  } catch (error: any) {
    logStep({
      level: 'error',
      message: error?.message || 'run-step failed',
      extra: { stack: error?.stack },
    });
    res.status(500).json({ error: error.message || 'run-step failed' });
  }
});

// Dev only: 查看租户隔离 session keys
app.get('/sa/debug/sessions', (_req, res) => {
  if (process.env.NODE_ENV === 'production') {
    return res.status(404).json({ error: 'not found' });
  }
  res.json({ keys: tenantSessionStore.listKeys() });
});

// ═══════════════════════════════════════════════════════
// POST /api/sa/run — 执行 SA 3-Tier 流水线
// ═══════════════════════════════════════════════════════
app.post('/api/sa/run', async (req, res) => {
  try {
    const {
      tenantId,
      projectId,
      requirementId,
      requirementText,
      userId,
      industry,
      sseSessionId,
    } = req.body;

    if (!requirementText || !tenantId || !projectId) {
      return res.status(400).json({
        error: '缺少必要参数: tenantId, projectId, requirementText',
      });
    }

    const taskId = uuidv4();
    runningTasks.set(taskId, { status: 'running', startedAt: new Date() });

    if (sseSessionId) {
      sseManager.send(sseSessionId, 'task-started', {
        taskId, projectId,
        requirementText: requirementText.substring(0, 100) + '...',
      });
    }

    const authHeader = req.body.authHeader || '';
    const providerCode = req.body.providerCode || req.body.provider || LLM_PROVIDER;
    const llm = new HttpLlmClient(
      LLM_GATEWAY_URL, LLM_API_KEY,
      tenantId ?? 'default', authHeader,
      providerCode, LLM_TEMPERATURE, LLM_MAX_TOKENS,
    );

    // DB 后端选择（InMemory 仅用于开发/测试，生产需设 SA_DB_BACKEND=sqlserver）
    const db = createDatabase(DB_BACKEND);

    // Validator 未注入时跳过校验（SAOrchestrator 有判空保护）
    const validators = {
      DFDValidator: null, BPMValidator: null, DictValidator: null,
      LogicValidator: null, CrossEventConsistencyValidator: null,
      ERValidator: null, STDValidator: null, UIValidator: null,
    };
    const orchestrator = new SAOrchestrator(llm, db, validators);

    const saRequest: SARequest = {
      tenantId,
      projectId,
      requirementId: requirementId || 0,
      requirementText,
      userId: userId || 'anonymous',
    };
    // 行业从请求体透传（优先级高于 SAOrchestrator 自动推断）
    if (industry) {
      (saRequest as any).industry = industry;
    }

    if (sseSessionId) {
      sseManager.send(sseSessionId, 'phase-start', { phase: 'scope', name: '边界提取' });
    }

    const result = await orchestrator.runSA(saRequest);

    const task = runningTasks.get(taskId)!;
    task.status = 'completed';
    task.result = result;
    task.completedAt = new Date();

    if (sseSessionId) {
      sseManager.send(sseSessionId, 'task-completed', {
        taskId,
        duration: task.completedAt.getTime() - task.startedAt.getTime(),
        eventCount: result.scope?.eventCount || 0,
      });
    }

    res.json({
      taskId,
      status: 'completed',
      result: {
        scope: result.scope,
        dfd: result.dfd,
        bpm: result.bpm,
        dict: result.dict,
        er: result.er,
        std: result.stateMachine,
      },
      validationStats: result.metadata?.validationStats,
    });
  } catch (error: any) {
    logStep({ level: 'error', message: error?.message || 'runSA failed', extra: { stack: error?.stack } });
    res.status(500).json({
      error: 'SA 流水线执行失败',
      message: error.message,
    });
  }
});

// ═══════════════════════════════════════════════════════
// POST /api/sa/run-async — 异步 SA（立即返回 taskId，C# 轮询）
// 正确入口：C# AnalystSkillService 调此接口，SA 内部走玛维斯算法+事件并行
// ═══════════════════════════════════════════════════════
app.post('/api/sa/run-async', (req, res) => {
  const {
    tenantId, projectId, pipelineId, requirementId, requirementText,
    userId, runId, authHeader: reqAuthHeader, providerCode: reqProvider,
    skeletonBusinessEvents,
  } = req.body;

  if (!requirementText || !tenantId || !projectId) {
    return res.status(400).json({ error: '缺少必要参数: tenantId, projectId, requirementText' });
  }

  const projectIdNum = Number(projectId);
  const pipelineIdNum = pipelineId != null ? Number(pipelineId) : projectIdNum;

  const taskId = uuidv4();
  runningTasks.set(taskId, { status: 'running', startedAt: new Date() });

  // 立即返回 taskId，不阻塞
  res.json({ taskId, status: 'running' });

  const authHeader = reqAuthHeader || req.headers.authorization || '';
  const providerCode = reqProvider || LLM_PROVIDER;

  // 后台异步执行（不 await）
  (async () => {
    const task = runningTasks.get(taskId)!;
    try {
      const llm = new HttpLlmClient(
        LLM_GATEWAY_URL, LLM_API_KEY,
        String(tenantId), authHeader,
        providerCode, LLM_TEMPERATURE, LLM_MAX_TOKENS,
      );
      const db = createDatabase(DB_BACKEND);
      const validators = {
        DFDValidator: null, BPMValidator: null, DictValidator: null,
        LogicValidator: null, CrossEventConsistencyValidator: null,
        ERValidator: null, STDValidator: null, UIValidator: null,
      };
      const orchestrator = new SAOrchestrator(llm, db, validators);

      const saRequest: SARequest = {
        tenantId: String(tenantId),
        projectId: projectIdNum,
        pipelineId: pipelineIdNum,
        requirementId: requirementId || 0,
        requirementText: String(requirementText),
        skeletonBusinessEvents: skeletonBusinessEvents ?? undefined,
        userId: userId || 'analyst-skill',
        runId: runId || undefined,
      };

      const result = await orchestrator.runSA(saRequest);

      task.status = 'completed';
      task.result = result;
      task.completedAt = new Date();

      logStep({
        level: 'info',
        runId,
        tenantId: String(tenantId),
        projectId: String(projectId),
        elapsedMs: task.completedAt.getTime() - task.startedAt.getTime(),
        message: `SA async task completed taskId=${taskId} events=${result.eventResults?.length ?? 0}`,
      });
    } catch (error: any) {
      task.status = 'failed';
      task.error = error?.message ?? 'SA async task failed';
      task.completedAt = new Date();
      logStep({ level: 'error', message: `SA async task failed taskId=${taskId}: ${task.error}` });
    }
  })();
});

// sa_* 九表物化由 JNPF C# SaMaterializer 直连主库；sa-service 不写业务库。

// ═══════════════════════════════════════════════════════
// GET /api/sa/events — SSE 事件流
// ═══════════════════════════════════════════════════════
app.get('/api/sa/events', (req, res) => {
  const sessionId = sseManager.createSession(res);

  const heartbeat = setInterval(() => {
    sseManager.send(sessionId, 'heartbeat', { time: new Date().toISOString() });
  }, 30000);

  req.on('close', () => {
    clearInterval(heartbeat);
    sseManager.close(sessionId);
  });
});

// ═══════════════════════════════════════════════════════
// GET /api/sa/tasks/:taskId — 查询任务状态（completed 时含完整 SA 产出）
// ═══════════════════════════════════════════════════════
app.get('/api/sa/tasks/:taskId', (req, res) => {
  const task = runningTasks.get(req.params.taskId);
  if (!task) {
    return res.status(404).json({ error: '任务不存在' });
  }
  const resp: Record<string, any> = {
    taskId: req.params.taskId,
    status: task.status,
    startedAt: task.startedAt,
    completedAt: task.completedAt,
    duration: task.completedAt
      ? task.completedAt.getTime() - task.startedAt.getTime()
      : Date.now() - task.startedAt.getTime(),
  };
  if (task.status === 'completed' && task.result) {
    // 完整产出：C# 侧读取 eventResults 投影 IR 事件
    resp.result = task.result;
  }
  if (task.status === 'failed') {
    resp.error = task.error;
  }
  res.json(resp);
});

// ═══════════════════════════════════════════════════════
// 启动（测试环境 SA_TEST=1 时不监听端口，supertest 自行绑定）
// ═══════════════════════════════════════════════════════
if (process.env.SA_TEST !== '1') {
  app.listen(PORT, () => {
    if (!LLM_GATEWAY_URL) {
      logStep({ level: 'error', message: 'LLM_GATEWAY_URL 未设置，请配置环境变量后重启' });
    }
    if (DB_BACKEND === 'inmemory') {
      logStep({ level: 'warn', message: '使用 InMemory 数据库（数据重启即丢失）。生产环境请设置 SA_DB_BACKEND=sqlserver' });
    }
    logStep({
      level: 'info',
      message: `SA Service 启动完成 port=${PORT} db=${DB_BACKEND} provider=${LLM_PROVIDER} gateway=${LLM_GATEWAY_URL || '(unset)'}`,
    });
  });
}

// ── DB 工厂 ──
import { ISADatabase } from './orchestrator/orchestrator-types';

function createDatabase(backend: string): ISADatabase {
  if (backend === 'sqlserver') {
    const cs = process.env.SA_DB_CONNECTION_STRING;
    if (!cs) {
      throw new Error('SA_DB_BACKEND=sqlserver 需要设置 SA_DB_CONNECTION_STRING（与 JNPF SQL Server 相同库）');
    }
    return new SqlServerSADatabase(cs);
  }
  return new InMemorySADatabase();
}

export { app };
