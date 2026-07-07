// SAOrchestrator - SDK 主入口
// 后端只需 await runSA(req) 即可完成完整需求分析

import {
  SARequest, SAContext, SAOutput, SAConfig, DEFAULT_SA_CONFIG,
  ScopeOutput, DFDOutput, BPMOutput, DictOutput, PSpecOutput,
  DecisionTableOutput, EROutput, StateMachineOutput, UIOutput,
  SAEventResult, SkeletonBusinessEvent,
  ISADatabase, ILLMClient, ValidationError
} from './orchestrator-types';

/** 简单信号量：控制并发事件数，防止同时打爆 LLM 限速 */
class Semaphore {
  private available: number;
  private queue: Array<() => void> = [];
  constructor(max: number) { this.available = max; }
  acquire(): Promise<void> {
    if (this.available > 0) { this.available--; return Promise.resolve(); }
    return new Promise(resolve => this.queue.push(() => { this.available--; resolve(); }));
  }
  release(): void {
    this.available++;
    const next = this.queue.shift();
    if (next) next();
  }
}
import {
  ScopeAgent, DFDAgent, BPMAgent, DictAgent, PSpecAgent,
  DecisionTableAgent, ERAgent, StateMachineAgent, UIAgent
} from '../agents';
import { runWithRetry, RetryResult } from './RetryLoop';
import { decideSteps, runScopeStep, classifyEvent } from './StepRouter';
import { DKEEFacade } from '../dkee';
import { logStep } from '../lib/structuredLogger';

// =====================================================
// 注入的 Validator(从 @your-org/sa-validators 包)
// 这里用 type-only 避免硬依赖,实际使用方注入
// =====================================================
export interface ValidatorBundle {
  DFDValidator: any;
  BPMValidator: any;
  DictValidator: any;
  LogicValidator: any;
  CrossEventConsistencyValidator: any;
  ERValidator: any;
  STDValidator: any;
  UIValidator: any;
}

// =====================================================
// SAOrchestrator
// =====================================================
export class SAOrchestrator {
  private agents: Map<string, any>;
  private dkee: DKEEFacade;

  constructor(
    private llm: ILLMClient,
    public db: ISADatabase,
    private validators: ValidatorBundle,
    private config: SAConfig = DEFAULT_SA_CONFIG
  ) {
    this.agents = new Map<string, any>([
      ['ScopeAgent', new ScopeAgent(llm)],
      ['DFDAgent', new DFDAgent(llm)],
      ['BPMAgent', new BPMAgent(llm)],
      ['DictAgent', new DictAgent(llm)],
      ['PSpecAgent', new PSpecAgent(llm)],
      ['DecisionTableAgent', new DecisionTableAgent(llm)],
      ['ERAgent', new ERAgent(llm)],
      ['StateMachineAgent', new StateMachineAgent(llm)],
      ['UIAgent', new UIAgent(llm)],
    ]);
    this.dkee = new DKEEFacade();
  }

  // ============================================================
  // 单步执行（C# Analyst Skill 逐步驱动）
  // ============================================================
  async runSingleStep(params: {
    tenantId: string;
    projectId: string;
    eventId: string;
    agentName: string;
    irStepName: string;
    requirementText: string;
    skeleton?: any;
    previousSteps?: Record<string, any>;
    runId?: string;
  }): Promise<any> {
    const start = Date.now();
    const ctx: SAContext = {
      tenantId: params.tenantId,
      projectId: Number(params.projectId) || 0,
      requirementId: 0,
      requirementText: params.requirementText,
      eventId: Number(params.eventId.replace(/\D/g, '')) || 1,
      eventDescription: params.eventId,
      assetLevel: 'EVENT',
      kgPatterns: [],
      domainModel: await this.db.getDomainModel('general'),
      previousSteps: { ...(params.previousSteps || {}), skeleton: params.skeleton },
      userId: 'analyst-skill',
      startTime: start,
      currentEventId: Number(params.eventId) || 0,
    };

    const tableMap: Record<string, string> = {
      ScopeAgent: 'sa_scope',
      DFDAgent: 'sa_dfd',
      BPMAgent: 'sa_business_process',
      DictAgent: 'sa_data_dictionary',
      PSpecAgent: 'sa_pspec',
      DecisionTableAgent: 'sa_decision_table',
      ERAgent: 'sa_er',
      StateMachineAgent: 'sa_state_machine',
      UIAgent: 'sa_ui',
    };

    const agentName = params.agentName;
    const tableName = tableMap[agentName] || 'sa_step';

    if (agentName === 'ScopeAgent') {
      return await runScopeStep(this, ctx);
    }

    const agent = this.agents.get(agentName);
    if (!agent) throw new Error(`Agent ${agentName} not found`);

    const output = await runWithRetry<any>(
      tableName,
      ctx,
      this.db,
      { maxRetries: this.config.maxRetries, retryDelayMs: this.config.retryDelayMs },
      async () => await agent.generate(ctx),
      (out) => this.runDefaultValidator(agentName, out, ctx),
    );

    logStep({
      level: 'info',
      runId: params.runId,
      tenantId: params.tenantId,
      projectId: params.projectId,
      eventId: params.eventId,
      stepName: params.irStepName,
      elapsedMs: Date.now() - start,
      message: `${agentName} step completed`,
    });

    return output.output;
  }

  // ============================================================
  // 主入口:后端调用（3-Tier 架构：Project → Event → Process）
  // ============================================================
  async runSA(req: SARequest): Promise<SAOutput> {
    const startTime = Date.now();
    const validationStats: any[] = [];

    logStep({
      level: 'info',
      runId: req.runId,
      tenantId: req.tenantId,
      projectId: String(req.projectId),
      message: 'SA run started',
    });

    // 解析 Context(注入 KG 模式 + 领域模型)
    const ctx = await this.resolveContext(req);

    // ═══════════════════════════════════════
    // Phase 0: 边界提取（PM 骨架已确认则跳过 ScopeAgent 重切）
    // ═══════════════════════════════════════
    let scope: ScopeOutput;
    if (req.skeletonBusinessEvents?.length) {
      scope = buildScopeFromSkeleton(req.skeletonBusinessEvents, req.requirementText);
      const { id } = await this.db.saveScope(scope, ctx);
      ctx.scopeId = id;
      ctx.previousSteps['scope'] = scope;
      validationStats.push({ step: 'Scope(skeleton)', attempts: 1, passed: true });
    } else {
      scope = await runScopeStep(this, ctx);
      ctx.previousSteps['scope'] = scope;
      validationStats.push({ step: 'Scope', attempts: 1, passed: true });
    }

    // ═══════════════════════════════════════
    // Phase 1: PROJECT 级（跑一次）
    // 产生全局 DFD / BPM / Dict / ER / STD
    // ═══════════════════════════════════════
    ctx.assetLevel = 'PROJECT';

    const dfd = await this.runStepWithValidation<DFDOutput>('DFDAgent', 'sa_dfd', ctx,
      async (output) => { const { id } = await this.db.saveDFD(output, ctx, ctx.scopeId!); ctx.dfdId = id; });
    ctx.previousSteps['dfd'] = dfd;
    validationStats.push({ step: 'DFD', attempts: 1, passed: true });

    const bpm = await this.runStepWithValidation<BPMOutput>('BPMAgent', 'sa_business_process', ctx,
      async (output) => { const { id } = await this.db.saveBPM(output, ctx, ctx.dfdId!); ctx.bpmId = id; });
    ctx.previousSteps['bpm'] = bpm;
    validationStats.push({ step: 'BPM', attempts: 1, passed: true });

    const dict = await this.runStepWithValidation<DictOutput>('DictAgent', 'sa_data_dictionary', ctx,
      async (output) => { const { id } = await this.db.saveDict(output, ctx, ctx.dfdId!, ctx.bpmId!); ctx.dictId = id; });
    ctx.previousSteps['dict'] = dict;
    ctx.projectDict = dict;  // 保存为 Project 级全局字典
    validationStats.push({ step: 'Dict', attempts: 1, passed: true });

    const er = await this.runStepWithValidation<EROutput>('ERAgent', 'sa_er', ctx,
      async (output) => { const { id } = await this.db.saveER(output, ctx, ctx.dictId!); ctx.erId = id; });
    ctx.previousSteps['er'] = er;
    validationStats.push({ step: 'ER', attempts: 1, passed: true });

    const stateMachine = await this.runStepWithValidation<StateMachineOutput>('StateMachineAgent', 'sa_state_machine', ctx,
      async (output) => { const { id } = await this.db.saveStateMachine(output, ctx, ctx.dictId!, ctx.bpmId!); ctx.stateMachineId = id; });
    ctx.previousSteps['stateMachine'] = stateMachine;
    validationStats.push({ step: 'StateMachine', attempts: 1, passed: true });

    logStep({
      level: 'info',
      runId: req.runId,
      tenantId: req.tenantId,
      projectId: String(req.projectId),
      message: 'SA project-level steps completed',
    });

    // ═══════════════════════════════════════
    // Phase 2 & 3: EVENT / PROCESS 级（并行）
    // 玛维斯算法：每个事件按 classifyEvent(complexity) 裁剪步骤；
    // 同一事件内 PSpec ∥ DecisionTable；所有事件 Promise.all 并发。
    // 单事件失败隔离，不阻断其他事件。
    // ═══════════════════════════════════════
    const MAX_CONCURRENT_EVENTS = 5;
    const semaphore = new Semaphore(MAX_CONCURRENT_EVENTS);
    const eventResultMap = new Map<string, SAEventResult>();

    const eventKey = (e: ScopeOutput['businessEvents'][number]) =>
      e.irEventId ?? String(e.id);

    // PROJECT 级步骤：所有事件共享（只读引用，安全并发访问）
    const projectSteps: Record<string, any> = {
      DomainModel: scope,
      AggregateDesign: ctx.previousSteps['dfd'],
      EventCatalog: ctx.previousSteps['bpm'],
      CommandQuery: ctx.previousSteps['dict'],
      DataModel: ctx.previousSteps['er'],
      UISpec: ctx.previousSteps['stateMachine'],
    };

    await Promise.all(scope.businessEvents.map(async (event) => {
      await semaphore.acquire();
      try {
        const tierDecision = classifyEvent(event, ctx.projectDict);

        logStep({
          level: 'info',
          runId: req.runId,
          tenantId: req.tenantId,
          projectId: String(req.projectId),
          eventId: String(event.id),
          message: `Event "${event.name}" [${event.complexity}] → ${tierDecision.assetLevel}: ${tierDecision.reason}`,
        });

        // 每个事件独立 context，避免共享 mutable 状态
        const eventCtx: SAContext = {
          ...ctx,
          previousSteps: { ...ctx.previousSteps },
          currentEventId: event.id,
          assetLevel: tierDecision.assetLevel,
          pspecId: undefined,
          decisionTableId: undefined,
          uiId: undefined,
          lastErrors: undefined,
        };

        const eventSteps: Record<string, any> = { ...projectSteps };

        try {
          // PROCESS 级：PSpec ∥ DecisionTable（两者互不依赖）
          if (tierDecision.assetLevel === 'PROCESS') {
            const existingDTs = await this.loadExistingDecisionTables(eventCtx);

            const [pspec, decisionTable] = await Promise.all([
              tierDecision.stepsToRun.includes('PSpecAgent')
                ? this.runStepWithValidation<PSpecOutput>(
                    'PSpecAgent', 'sa_pspec', eventCtx,
                    async (out) => { await this.db.savePSpec(out, eventCtx, ctx.dictId ?? 0, ctx.bpmId ?? 0); })
                : Promise.resolve(undefined),
              tierDecision.stepsToRun.includes('DecisionTableAgent')
                ? this.runStepWithValidation<DecisionTableOutput>(
                    'DecisionTableAgent', 'sa_decision_table',
                    { ...eventCtx, allDecisionTables: existingDTs },
                    async (out) => { await this.db.saveDecisionTable(out, eventCtx, 0, ctx.dictId ?? 0); })
                : Promise.resolve(undefined),
            ]);

            if (pspec) {
              eventCtx.previousSteps['pspec'] = pspec;
              eventSteps['IntegrationPoints'] = pspec;
              validationStats.push({ step: `PSpec[${event.id}]`, attempts: 1, passed: true });
            }
            if (decisionTable) {
              eventCtx.previousSteps['decisionTable'] = decisionTable;
              eventSteps['WorkflowSpec'] = decisionTable;
              validationStats.push({ step: `DecisionTable[${event.id}]`, attempts: 1, passed: true });
            }
          }

          // EVENT / PROCESS 级：UI
          if (tierDecision.stepsToRun.includes('UIAgent')) {
            const ui = await this.runStepWithValidation<UIOutput>(
              'UIAgent', 'sa_ui', eventCtx,
              async (out) => { await this.db.saveUI(out, eventCtx, ctx.bpmId ?? 0, ctx.dictId ?? 0); });
            eventSteps['DeliveryChecklist'] = ui;
            validationStats.push({ step: `UI[${event.id}]`, attempts: 1, passed: true });
          }

          eventResultMap.set(eventKey(event), {
            eventId: eventKey(event),
            eventName: event.name,
            complexity: event.complexity,
            steps: eventSteps,
          });
        } catch (e) {
          logStep({
            level: 'error',
            runId: req.runId,
            tenantId: req.tenantId,
            projectId: String(req.projectId),
          eventId: eventKey(event),
          message: `Event "${event.name}" failed: ${(e as Error).message}`,
          });
          validationStats.push({ step: `Event_${eventKey(event)}`, attempts: 1, passed: false, error: (e as Error).message });
          eventResultMap.set(eventKey(event), {
            eventId: eventKey(event),
            eventName: event.name,
            complexity: event.complexity,
            steps: eventSteps,
            error: (e as Error).message,
          });
        }
      } finally {
        semaphore.release();
      }
    }));

    // 保持原始 businessEvent 顺序
    const eventResults: SAEventResult[] = scope.businessEvents.map(e =>
      eventResultMap.get(eventKey(e)) ?? {
        eventId: eventKey(e), eventName: e.name, complexity: e.complexity,
        steps: { ...projectSteps }, error: 'event not processed',
      }
    );

    // ═══════════════════════════════════════
    // Phase 4: DKEE 提炼
    // ═══════════════════════════════════════
    if (this.config.enableDKEE) {
      await this.dkee.extractAndScore('general');
    }

    const totalDuration = Date.now() - startTime;
    logStep({
      level: 'info',
      runId: req.runId,
      tenantId: req.tenantId,
      projectId: String(req.projectId),
      elapsedMs: totalDuration,
      message: 'SA run completed',
      extra: { validationStats },
    });

    return {
      projectId: ctx.projectId,
      tenantId: ctx.tenantId,
      scope,
      dfd, bpm, dict, er, stateMachine,
      eventResults,
      metadata: { totalDuration, totalRetries: 0, validationStats },
    };
  }

  // ============================================================
  // 内部:跑单步(带 retry + validator + DB 写入)
  // ============================================================
  async runStepWithValidation<T>(
    agentName: string,
    tableName: string,
    ctx: SAContext,
    saveToDb: (output: T) => Promise<void>,
    customValidator?: (output: T) => { passed: boolean; errors: ValidationError[] }
  ): Promise<T> {
    const agent = this.agents.get(agentName);
    if (!agent) throw new Error(`Agent ${agentName} not found`);

    const result = await runWithRetry<T>(
      tableName,
      ctx,
      this.db,
      { maxRetries: this.config.maxRetries, retryDelayMs: this.config.retryDelayMs },
      async () => await agent.generate(ctx),
      (output) => customValidator ? customValidator(output) : this.runDefaultValidator(agentName, output, ctx)
    );

    await saveToDb(result.output);
    return result.output;
  }

  // 默认 Validator 路由(根据 Agent 名选对应 Validator)
  // 判空保护：Validator 未注入时跳过校验（允许无校验模式运行）
  private runDefaultValidator(agentName: string, output: any, ctx: SAContext): { passed: boolean; errors: ValidationError[] } {
    try {
      switch (agentName) {
        case 'DFDAgent': {
          if (!this.validators.DFDValidator) {
            console.warn('[Validator] DFDValidator 未注入，跳过 DFD 校验');
            return { passed: true, errors: [] };
          }
          const v = new this.validators.DFDValidator(output);
          return v.validate();
        }
        case 'BPMAgent': {
          if (!this.validators.BPMValidator) {
            console.warn('[Validator] BPMValidator 未注入，跳过 BPM 校验');
            return { passed: true, errors: [] };
          }
          const dfdProcesses = ctx.previousSteps['dfd']?.processes || [];
          const v = new this.validators.BPMValidator(output, dfdProcesses);
          return v.validate();
        }
        case 'DictAgent': {
          if (!this.validators.DictValidator) {
            console.warn('[Validator] DictValidator 未注入，跳过 Dict 校验');
            return { passed: true, errors: [] };
          }
          const dfd = ctx.previousSteps['dfd'] || { dataFlows: [], dataStores: [] };
          const v = new this.validators.DictValidator(output, dfd);
          return v.validate();
        }
        case 'PSpecAgent': {
          if (!this.validators.LogicValidator) {
            console.warn('[Validator] LogicValidator 未注入，跳过 PSpec 校验');
            return { passed: true, errors: [] };
          }
          const dict = ctx.previousSteps['dict'];
          if (!dict) return { passed: true, errors: [] };
          const v = new this.validators.LogicValidator(output, dict);
          return v.validate();
        }
        case 'DecisionTableAgent': {
          if (!this.validators.CrossEventConsistencyValidator) {
            console.warn('[Validator] CrossEventConsistencyValidator 未注入，跳过 DecisionTable 校验');
            return { passed: true, errors: [] };
          }
          const allTables = ctx.allDecisionTables || [];
          const v = new this.validators.CrossEventConsistencyValidator(output, allTables);
          return v.validate();
        }
        case 'ERAgent': {
          if (!this.validators.ERValidator) {
            console.warn('[Validator] ERValidator 未注入，跳过 ER 校验');
            return { passed: true, errors: [] };
          }
          const dict = ctx.previousSteps['dict'];
          if (!dict) return { passed: true, errors: [] };
          const v = new this.validators.ERValidator(output, dict);
          return v.validate();
        }
        case 'StateMachineAgent': {
          if (!this.validators.STDValidator) {
            console.warn('[Validator] STDValidator 未注入，跳过 StateMachine 校验');
            return { passed: true, errors: [] };
          }
          const dict = ctx.previousSteps['dict'];
          const v = new this.validators.STDValidator(output, dict);
          return v.validate();
        }
        case 'UIAgent': {
          if (!this.validators.UIValidator) {
            console.warn('[Validator] UIValidator 未注入，跳过 UI 校验');
            return { passed: true, errors: [] };
          }
          const dict = ctx.previousSteps['dict'];
          const bpm = ctx.previousSteps['bpm'];
          if (!dict || !bpm) return { passed: true, errors: [] };
          const v = new this.validators.UIValidator(output, dict, bpm);
          return v.validate();
        }
        default:
          return { passed: true, errors: [] };
      }
    } catch (e) {
      console.error(`[Validator] ${agentName} 执行异常:`, e);
      return { passed: false, errors: [{ code: 'VALIDATOR_EXCEPTION', message: (e as Error).message, severity: 'ERROR' }] };
    }
  }

  // ============================================================
  // 解析 Context(注入 KG 模式 + 领域模型)
  // ============================================================
  private async resolveContext(req: SARequest): Promise<SAContext> {
    const kgPatterns = await this.db.getProjectKGPatterns(req.projectId, 5);
    const industry = this.inferIndustry(req.requirementText);
    const domainModel = await this.db.getDomainModel(industry);

    return {
      tenantId: req.tenantId,
      projectId: req.projectId,
      pipelineId: req.pipelineId ?? req.projectId,
      requirementId: req.requirementId,
      requirementText: req.requirementText,
      eventId: req.eventId,
      eventDescription: req.eventDescription,
      assetLevel: req.assetLevel || 'PROJECT',
      kgPatterns,
      domainModel,
      previousSteps: {},
      userId: req.userId,
      startTime: Date.now(),
    };
  }

  private async loadExistingDecisionTables(ctx: SAContext): Promise<any[]> {
    return await this.db.getAllDecisionTablesInProject(ctx.projectId);
  }

  // ============================================================
  // 工具:推断行业
  // ============================================================
  private inferIndustry(text: string): string {
    if (/MES|制造|工单|工序|报工|加工/.test(text)) return 'manufacturing';
    if (/电商|订单|购物|支付/.test(text)) return 'ecommerce';
    if (/眼镜|验光|处方/.test(text)) return 'optical';
    return 'general';
  }

  // 取 events 中最高复杂度
  private getMaxComplexity(scope: ScopeOutput): 'simple' | 'medium' | 'complex' {
    const order = { simple: 0, medium: 1, complex: 2 } as const;
    let max: 'simple' | 'medium' | 'complex' = 'simple';
    scope.businessEvents.forEach((e: { complexity: 'simple' | 'medium' | 'complex' }) => {
      if (order[e.complexity] > order[max]) max = e.complexity;
    });
    return max;
  }

  // 是否有状态变化
  private hasStateChange(scope: ScopeOutput): boolean {
    return scope.businessEvents.some(e =>
      /状态|报工|审批|驳回|关闭|完成/.test(e.name + e.description)
    );
  }
}

/** PM 已确认骨架 → 跳过 ScopeAgent LLM 重切，保留 IR eventId（BE-001 等） */
export function buildScopeFromSkeleton(
  skeletonEvents: SkeletonBusinessEvent[],
  requirementText: string,
): ScopeOutput {
  const inScope = skeletonEvents.map(e => e.eventName).filter(Boolean);
  if (inScope.length === 0 && requirementText) {
    inScope.push(requirementText.slice(0, 80));
  }
  return {
    systemBoundary: { inScope, outOfScope: [] },
    externalEntities: [],
    businessEvents: skeletonEvents.map((e, idx) => ({
      id: idx + 1,
      irEventId: e.eventId,
      name: e.eventName,
      description: e.description ?? e.eventName,
      complexity: (e.complexityHint ?? 'simple') as 'simple' | 'medium' | 'complex',
    })),
    eventCount: skeletonEvents.length,
  };
}
