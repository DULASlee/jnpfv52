// sa-service/src/server.ts
// SA Service HTTP 入口 — 接通 Pipeline Engine 的桥梁

import express from 'express';
import cors from 'cors';
import { v4 as uuidv4 } from 'uuid';
import { SAOrchestrator } from './orchestrator/SAOrchestrator';
import { InMemorySADatabase } from './orchestrator/SADatabase';
import { ILLMClient, SARequest, SAOutput, SAConfig, DEFAULT_SA_CONFIG } from './orchestrator/orchestrator-types';

// ═══════════════════════════════════════════════════════
// 1. LLM 客户端适配器（桥接后端 ILlmGatewayService）
// ═══════════════════════════════════════════════════════
class HttpLlmClient implements ILLMClient {
  constructor(
    private gatewayUrl: string,
    private apiKey?: string,
  ) {}

  async generate(params: {
    systemPrompt: string;
    context: Record<string, any>;
    lastErrors?: string[];
    temperature?: number;
  }): Promise<any> {
    // 将 lastErrors 注入 context（错误回灌）
    const enrichedContext = { ...params.context };
    if (params.lastErrors && params.lastErrors.length > 0) {
      enrichedContext._lastErrors = params.lastErrors;
    }

    const response = await fetch(this.gatewayUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(this.apiKey ? { 'Authorization': `Bearer ${this.apiKey}` } : {}),
      },
      body: JSON.stringify({
        systemPrompt: params.systemPrompt,
        userPrompt: JSON.stringify(enrichedContext),
        temperature: params.temperature ?? 0.3,
        maxTokens: 4096,
      }),
    });

    if (!response.ok) {
      throw new Error(`LLM Gateway 返回 ${response.status}: ${await response.text()}`);
    }

    const data: any = await response.json();
    const content = data.content || data.choices?.[0]?.message?.content || data.result || data;

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
const LLM_GATEWAY_URL = process.env.LLM_GATEWAY_URL || 'http://localhost:5000/api/ai/generate';
const LLM_API_KEY = process.env.LLM_API_KEY || '';

const sseManager = new SSEManager();

// 运行中的任务追踪
const runningTasks = new Map<string, {
  status: 'running' | 'completed' | 'failed';
  result?: SAOutput;
  error?: string;
  startedAt: Date;
  completedAt?: Date;
}>();

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

    const llm = new HttpLlmClient(LLM_GATEWAY_URL, LLM_API_KEY);
    const db = new InMemorySADatabase();
    const emptyValidators = {
      DFDValidator: null, BPMValidator: null, DictValidator: null,
      LogicValidator: null, CrossEventConsistencyValidator: null,
      ERValidator: null, STDValidator: null, UIValidator: null,
    };
    const orchestrator = new SAOrchestrator(llm, db, emptyValidators);

    const saRequest: SARequest = {
      tenantId,
      projectId,
      requirementId: requirementId || 0,
      requirementText,
      userId: userId || 'system',
    };

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
    console.error('[SA Service] runSA 失败:', error);
    res.status(500).json({
      error: 'SA 流水线执行失败',
      message: error.message,
    });
  }
});

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
// GET /api/sa/tasks/:taskId — 查询任务状态
// ═══════════════════════════════════════════════════════
app.get('/api/sa/tasks/:taskId', (req, res) => {
  const task = runningTasks.get(req.params.taskId);
  if (!task) {
    return res.status(404).json({ error: '任务不存在' });
  }
  res.json({
    taskId: req.params.taskId,
    status: task.status,
    startedAt: task.startedAt,
    completedAt: task.completedAt,
    duration: task.completedAt
      ? task.completedAt.getTime() - task.startedAt.getTime()
      : Date.now() - task.startedAt.getTime(),
  });
});

// ═══════════════════════════════════════════════════════
// 启动
// ═══════════════════════════════════════════════════════
app.listen(PORT, () => {
  console.log(`[SA Service] 启动成功 → http://localhost:${PORT}`);
  console.log(`[SA Service] LLM Gateway → ${LLM_GATEWAY_URL}`);
  console.log(`[SA Service] 健康检查 → http://localhost:${PORT}/api/sa/health`);
});

export { app };
