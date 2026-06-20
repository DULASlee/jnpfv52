// SAOrchestrator - SDK 主入口
// 后端只需 await runSA(req) 即可完成完整需求分析

import {
  SARequest, SAContext, SAOutput, SAConfig, DEFAULT_SA_CONFIG,
  ScopeOutput, DFDOutput, BPMOutput, DictOutput, PSpecOutput,
  DecisionTableOutput, EROutput, StateMachineOutput, UIOutput,
  ISADatabase, ILLMClient, ValidationError
} from './orchestrator-types';
import {
  ScopeAgent, DFDAgent, BPMAgent, DictAgent, PSpecAgent,
  DecisionTableAgent, ERAgent, StateMachineAgent, UIAgent
} from '../agents';
import { runWithRetry, RetryResult } from './RetryLoop';
import { decideSteps, runScopeStep, classifyEvent } from './StepRouter';
import { DKEEFacade } from '../dkee';
import { BaseAgent } from './BaseAgent';

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
  // 主入口:后端调用（3-Tier 架构：Project → Event → Process）
  // ============================================================
  async runSA(req: SARequest): Promise<SAOutput> {
    const startTime = Date.now();
    const validationStats: any[] = [];

    console.log(`[SA] 开始需求分析: project=${req.projectId} tenant=${req.tenantId}`);

    // 解析 Context(注入 KG 模式 + 领域模型)
    const ctx = await this.resolveContext(req);

    // ═══════════════════════════════════════
    // Phase 0: 边界提取（所有级别共享）
    // ═══════════════════════════════════════
    const scope = await runScopeStep(this, ctx);
    ctx.previousSteps['scope'] = scope;
    validationStats.push({ step: 'Scope', attempts: 1, passed: true });

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

    console.log(`[SA] Project 级完成: DFD/BPM/Dict/ER/STD 已生成`);

    // ═══════════════════════════════════════
    // Phase 2 & 3: EVENT / PROCESS 级（逐事件）
    // 单事件失败隔离：一个事件异常不阻断其他事件
    // ═══════════════════════════════════════
    let pspec: PSpecOutput | undefined;
    let decisionTable: DecisionTableOutput | undefined;
    let ui: UIOutput | undefined;

    for (const event of scope.businessEvents) {
      try {
        const tierDecision = classifyEvent(event, ctx.projectDict);
        ctx.assetLevel = tierDecision.assetLevel;
        ctx.currentEventId = event.id;

        console.log(`[SA] 事件 "${event.name}" → ${tierDecision.assetLevel}: ${tierDecision.reason}`);

        // PROCESS 级：复杂事件深度推演
        if (tierDecision.assetLevel === 'PROCESS') {
          if (tierDecision.stepsToRun.includes('PSpecAgent')) {
            pspec = await this.runStepWithValidation<PSpecOutput>('PSpecAgent', 'sa_pspec', ctx,
              async (output) => { const { id } = await this.db.savePSpec(output, ctx, ctx.dictId!, ctx.bpmId!); ctx.pspecId = id; });
            ctx.previousSteps['pspec'] = pspec;
            validationStats.push({ step: 'PSpec', attempts: 1, passed: true });
          }

          if (tierDecision.stepsToRun.includes('DecisionTableAgent')) {
            ctx.kgPatterns = await this.loadExistingDecisionTables(ctx);
            decisionTable = await this.runStepWithValidation<DecisionTableOutput>('DecisionTableAgent', 'sa_decision_table', ctx,
              async (output) => { const { id } = await this.db.saveDecisionTable(output, ctx, ctx.pspecId!, ctx.dictId!); ctx.decisionTableId = id; });
            ctx.previousSteps['decisionTable'] = decisionTable;
            validationStats.push({ step: 'DecisionTable', attempts: 1, passed: true });
          }
        }

        // EVENT / PROCESS 级：都跑 UI
        if (tierDecision.stepsToRun.includes('UIAgent')) {
          ui = await this.runStepWithValidation<UIOutput>('UIAgent', 'sa_ui', ctx,
            async (output) => { const { id } = await this.db.saveUI(output, ctx, ctx.bpmId!, ctx.dictId!); ctx.uiId = id; });
          ctx.previousSteps['ui'] = ui;
          validationStats.push({ step: 'UI', attempts: 1, passed: true });
        }
      } catch (e) {
        console.error(`[SA] 事件 "${event.name}" (id=${event.id}) 处理失败，跳过并继续:`, e);
        validationStats.push({ step: `Event_${event.id}`, attempts: 1, passed: false, error: (e as Error).message });
        // 重置事件级 context 增量字段，避免污染下一个事件
        ctx.previousSteps['pspec'] = undefined;
        ctx.previousSteps['decisionTable'] = undefined;
        ctx.previousSteps['ui'] = undefined;
      }
    }

    // ═══════════════════════════════════════
    // Phase 4: DKEE 提炼
    // ═══════════════════════════════════════
    if (this.config.enableDKEE) {
      await this.dkee.extractAndScore('general');
    }

    const totalDuration = Date.now() - startTime;
    console.log(`[SA] 完成: ${totalDuration}ms, 步骤统计:`, validationStats);

    return {
      projectId: ctx.projectId,
      tenantId: ctx.tenantId,
      scope,
      dfd, bpm, dict, pspec, decisionTable, er, stateMachine, ui,
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
