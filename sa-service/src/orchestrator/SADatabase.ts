// ISADatabase 实现 - in-memory mock(可直接替换为 SQL Server 实现)

import {
  ISADatabase, SAContext, ScopeOutput, DFDOutput, BPMOutput, DictOutput,
  PSpecOutput, DecisionTableOutput, EROutput, StateMachineOutput, UIOutput,
  ValidationLogRecord, KGPattern, DomainModelContext
} from './orchestrator-types';

// =====================================================
// InMemorySADatabase - 内存实现(用于开发和测试)
// 生产环境替换为 SqlServerSADatabase
// =====================================================
export class InMemorySADatabase implements ISADatabase {
  private scopes = new Map<number, any>();
  private dfds = new Map<number, any>();
  private bpms = new Map<number, any>();
  private dicts = new Map<number, any>();
  private pspecs = new Map<number, any>();
  private decisionTables = new Map<number, any>();
  private ers = new Map<number, any>();
  private stateMachines = new Map<number, any>();
  private uis = new Map<number, any>();
  private validationLogs: ValidationLogRecord[] = [];
  private nextId = 1;

  // ========================================================
  // Save 方法
  // ========================================================
  async saveScope(scope: ScopeOutput, ctx: SAContext): Promise<{ id: number }> {
    const id = this.nextId++;
    const pipelineId = ctx.pipelineId ?? ctx.projectId;
    this.scopes.set(id, { id, ...scope, projectId: ctx.projectId, pipelineId, tenantId: ctx.tenantId });
    return { id };
  }

  async saveDFD(dfd: DFDOutput, ctx: SAContext, scopeId: number): Promise<{ id: number }> {
    if (!this.scopes.has(scopeId)) throw new Error(`强外键违规: sa_scope(${scopeId}) 不存在`);
    const id = this.nextId++;
    this.dfds.set(id, { id, ...dfd, scopeId, projectId: ctx.projectId, tenantId: ctx.tenantId });
    return { id };
  }

  async saveBPM(bpm: BPMOutput, ctx: SAContext, dfdId: number): Promise<{ id: number }> {
    if (!this.dfds.has(dfdId)) throw new Error(`强外键违规: sa_dfd(${dfdId}) 不存在`);
    const id = this.nextId++;
    this.bpms.set(id, { id, ...bpm, dfdId, projectId: ctx.projectId, tenantId: ctx.tenantId });
    return { id };
  }

  async saveDict(dict: DictOutput, ctx: SAContext, dfdId: number, bpmId: number): Promise<{ id: number }> {
    if (!this.dfds.has(dfdId)) throw new Error(`强外键违规: sa_dfd(${dfdId}) 不存在`);
    if (!this.bpms.has(bpmId)) throw new Error(`强外键违规: sa_bpm(${bpmId}) 不存在`);
    const id = this.nextId++;
    this.dicts.set(id, { id, ...dict, dfdId, bpmId, projectId: ctx.projectId, tenantId: ctx.tenantId });
    return { id };
  }

  async savePSpec(pspec: PSpecOutput, ctx: SAContext, dictId: number, bpmId: number): Promise<{ id: number }> {
    if (!this.dicts.has(dictId)) throw new Error(`强外键违规: sa_dict(${dictId}) 不存在`);
    const id = this.nextId++;
    this.pspecs.set(id, { id, ...pspec, dictId, bpmId, projectId: ctx.projectId, tenantId: ctx.tenantId });
    return { id };
  }

  async saveDecisionTable(dt: DecisionTableOutput, ctx: SAContext, pspecId: number, dictId: number): Promise<{ id: number }> {
    // pspecId=0 表示"独立 DecisionTable，无关联 PSpec"（complex 事件并发运行时允许）
    if (pspecId !== 0 && !this.pspecs.has(pspecId)) throw new Error(`强外键违规: sa_pspec(${pspecId}) 不存在`);
    const id = this.nextId++;
    this.decisionTables.set(id, { id, ...dt, pspecId, dictId, projectId: ctx.projectId, tenantId: ctx.tenantId });
    return { id };
  }

  async saveER(er: EROutput, ctx: SAContext, dictId: number): Promise<{ id: number }> {
    if (!this.dicts.has(dictId)) throw new Error(`强外键违规: sa_dict(${dictId}) 不存在`);
    const id = this.nextId++;
    this.ers.set(id, { id, ...er, dictId, projectId: ctx.projectId, tenantId: ctx.tenantId });
    return { id };
  }

  async saveStateMachine(sm: StateMachineOutput, ctx: SAContext, dictId: number, bpmId: number): Promise<{ id: number }> {
    if (!this.dicts.has(dictId)) throw new Error(`强外键违规: sa_dict(${dictId}) 不存在`);
    const id = this.nextId++;
    this.stateMachines.set(id, { id, ...sm, dictId, bpmId, projectId: ctx.projectId, tenantId: ctx.tenantId });
    return { id };
  }

  async saveUI(ui: UIOutput, ctx: SAContext, bpmId: number, dictId: number): Promise<{ id: number }> {
    if (!this.bpms.has(bpmId)) throw new Error(`强外键违规: sa_bpm(${bpmId}) 不存在`);
    const id = this.nextId++;
    this.uis.set(id, { id, ...ui, bpmId, dictId, projectId: ctx.projectId, tenantId: ctx.tenantId });
    return { id };
  }

  // ========================================================
  // 校验日志
  // ========================================================
  async logValidation(record: ValidationLogRecord): Promise<void> {
    this.validationLogs.push(record);
  }

  // ========================================================
  // 读取方法
  // ========================================================
  async getProjectKGPatterns(projectId: number, limit = 5): Promise<KGPattern[]> {
    // Mock:返回空数组,实际从 kg_pattern 表查
    return [];
  }

  async getDomainModel(industry: string): Promise<DomainModelContext> {
    // Mock:返回空模型,实际从 domain_model 表查
    return {
      industry,
      standardFields: [],
      standardEntities: [],
      standardProcesses: [],
    };
  }

  async getAllDecisionTablesInProject(projectId: number): Promise<any[]> {
    return Array.from(this.decisionTables.values())
      .filter(dt => dt.projectId === projectId);
  }

  // 测试辅助
  getValidationLogs(): ValidationLogRecord[] {
    return this.validationLogs;
  }

  getStats() {
    return {
      scopes: this.scopes.size,
      dfds: this.dfds.size,
      bpms: this.bpms.size,
      dicts: this.dicts.size,
      pspecs: this.pspecs.size,
      decisionTables: this.decisionTables.size,
      ers: this.ers.size,
      stateMachines: this.stateMachines.size,
      uis: this.uis.size,
      validationLogs: this.validationLogs.length,
    };
  }
}
