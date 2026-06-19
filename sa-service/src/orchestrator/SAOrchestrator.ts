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
import { decideSteps, runScopeStep } from './StepRouter';
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
  // 主入口:后端调用
  // ============================================================
  async runSA(req: SARequest): Promise<SAOutput> {
    const startTime = Date.now();
    const validationStats: any[] = [];

    console.log(`[SA] 开始需求分析: project=${req.projectId} tenant=${req.tenantId}`);

    // 1. 解析 Context(注入 KG 模式 + 领域模型)
    const ctx = await this.resolveContext(req);

    // 2. Step 1: 跑 Scope(总是先跑,作为入口)
    const scopeResult = await runScopeStep(this, ctx);
    const scope: ScopeOutput = scopeResult;
    validationStats.push({ step: 'Scope', attempts: 1, passed: true });

    // 3. 根据事件复杂度决定后续步骤
    // 默认:取 events 中最高复杂度(也可以传 single event)
    const maxComplexity = this.getMaxComplexity(scope);
    const decision = decideSteps(maxComplexity, this.hasStateChange(scope));

    console.log(`[SA] 复杂度=${maxComplexity}, 路由: ${decision.reason}`);

    // 4. 按顺序跑后续步骤
    let dfd: DFDOutput | undefined, bpm: BPMOutput | undefined;
    let dict: DictOutput | undefined, pspec: PSpecOutput | undefined;
    let decisionTable: DecisionTableOutput | undefined;
    let er: EROutput | undefined, stateMachine: StateMachineOutput | undefined;
    let ui: UIOutput | undefined;

    // Step 2: DFD
    if (decision.runDFD) {
      const r = await this.runStepWithValidation<DFDOutput>('DFDAgent', 'sa_dfd', ctx,
        async (output) => { const { id } = await this.db.saveDFD(output, ctx, ctx.scopeId!); ctx.dfdId = id; });
      dfd = r;
      ctx.previousSteps['dfd'] = r;
      validationStats.push({ step: 'DFD', attempts: 1, passed: true });
    }

    // Step 3: BPM
    if (decision.runBPM && dfd) {
      const r = await this.runStepWithValidation<BPMOutput>('BPMAgent', 'sa_business_process', ctx,
        async (output) => { const { id } = await this.db.saveBPM(output, ctx, ctx.dfdId!); ctx.bpmId = id; });
      bpm = r;
      ctx.previousSteps['bpm'] = r;
      validationStats.push({ step: 'BPM', attempts: 1, passed: true });
    }

    // Step 4: 数据字典(★ 关键)
    if (decision.runDict && dfd) {
      const r = await this.runStepWithValidation<DictOutput>('DictAgent', 'sa_data_dictionary', ctx,
        async (output) => { const { id } = await this.db.saveDict(output, ctx, ctx.dfdId!, ctx.bpmId!); ctx.dictId = id; });
      dict = r;
      ctx.previousSteps['dict'] = r;
      validationStats.push({ step: 'Dict', attempts: 1, passed: true });
    }

    // Step 5: PSPEC
    if (decision.runPSpec && dict) {
      const r = await this.runStepWithValidation<PSpecOutput>('PSpecAgent', 'sa_pspec', ctx,
        async (output) => { const { id } = await this.db.savePSpec(output, ctx, ctx.dictId!, ctx.bpmId!); ctx.pspecId = id; });
      pspec = r;
      ctx.previousSteps['pspec'] = r;
      validationStats.push({ step: 'PSpec', attempts: 1, passed: true });
    }

    // Step 6: 判定表(★★ 跨事件一致)
    if (decision.runDecisionTable && dict) {
      ctx.kgPatterns = await this.loadExistingDecisionTables(ctx);
      const r = await this.runStepWithValidation<DecisionTableOutput>('DecisionTableAgent', 'sa_decision_table', ctx,
        async (output) => { const { id } = await this.db.saveDecisionTable(output, ctx, ctx.pspecId!, ctx.dictId!); ctx.decisionTableId = id; });
      decisionTable = r;
      ctx.previousSteps['decisionTable'] = r;
      validationStats.push({ step: 'DecisionTable', attempts: 1, passed: true });
    }

    // Step 7: ER
    if (decision.runER && dict) {
      const r = await this.runStepWithValidation<EROutput>('ERAgent', 'sa_er', ctx,
        async (output) => { const { id } = await this.db.saveER(output, ctx, ctx.dictId!); ctx.erId = id; });
      er = r;
      ctx.previousSteps['er'] = r;
      validationStats.push({ step: 'ER', attempts: 1, passed: true });
    }

    // Step 8: 状态机
    if (decision.runStateMachine && dict) {
      const r = await this.runStepWithValidation<StateMachineOutput>('StateMachineAgent', 'sa_state_machine', ctx,
        async (output) => { const { id } = await this.db.saveStateMachine(output, ctx, ctx.dictId!, ctx.bpmId!); ctx.stateMachineId = id; });
      stateMachine = r;
      ctx.previousSteps['stateMachine'] = r;
      validationStats.push({ step: 'StateMachine', attempts: 1, passed: true });
    }

    // Step 9: UI
    if (decision.runUI && bpm && dict) {
      const r = await this.runStepWithValidation<UIOutput>('UIAgent', 'sa_ui', ctx,
        async (output) => { const { id } = await this.db.saveUI(output, ctx, ctx.bpmId!, ctx.dictId!); ctx.uiId = id; });
      ui = r;
      ctx.previousSteps['ui'] = r;
      validationStats.push({ step: 'UI', attempts: 1, passed: true });
    }

    // 5. DKEE 提炼(从 9 张表抽取 Pattern)
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
  private runDefaultValidator(agentName: string, output: any, ctx: SAContext): { passed: boolean; errors: ValidationError[] } {
    try {
      switch (agentName) {
        case 'DFDAgent': {
          const v = new this.validators.DFDValidator(output);
          return v.validate();
        }
        case 'BPMAgent': {
          const dfdProcesses = ctx.previousSteps['dfd']?.processes || [];
          const v = new this.validators.BPMValidator(output, dfdProcesses);
          return v.validate();
        }
        case 'DictAgent': {
          const dfd = ctx.previousSteps['dfd'] || { dataFlows: [], dataStores: [] };
          const v = new this.validators.DictValidator(output, dfd);
          return v.validate();
        }
        case 'PSpecAgent': {
          const dict = ctx.previousSteps['dict'];
          if (!dict) return { passed: true, errors: [] };
          const v = new this.validators.LogicValidator(output, dict);
          return v.validate();
        }
        case 'DecisionTableAgent': {
          const allTables = ctx.allDecisionTables || [];
          const v = new this.validators.CrossEventConsistencyValidator(output, allTables);
          return v.validate();
        }
        case 'ERAgent': {
          const dict = ctx.previousSteps['dict'];
          if (!dict) return { passed: true, errors: [] };
          const v = new this.validators.ERValidator(output, dict);
          return v.validate();
        }
        case 'StateMachineAgent': {
          const dict = ctx.previousSteps['dict'];
          const v = new this.validators.STDValidator(output, dict);
          return v.validate();
        }
        case 'UIAgent': {
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
