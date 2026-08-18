// SA SDK - 共享类型定义

import { ValidationError } from '../validators/types';

// =====================================================
// 客户需求
// =====================================================
export interface SkeletonBusinessEvent {
  eventId: string;
  eventName: string;
  complexityHint?: 'simple' | 'medium' | 'complex';
  description?: string;
}

export interface SARequest {
  tenantId: string;
  projectId: number;
  /** Studio 流水线实例 ID（三元组，必填，R12 宪法级） */
  pipelineId: number;
  requirementId: number;
  requirementText: string;          // 客户原始需求
  /** PM 已确认骨架中的 businessEvents — 有则跳过 ScopeAgent 重切，直接驱动 SA */
  skeletonBusinessEvents?: SkeletonBusinessEvent[];
  eventId?: number;
  eventDescription?: string;
  assetLevel?: 'PROJECT' | 'EVENT' | 'PROCESS';
  userId: string;
  runId?: string;
}

// =====================================================
// SA 步骤产出(对应 9 张表)
// =====================================================
export interface ScopeOutput {
  systemBoundary: { inScope: string[]; outOfScope: string[] };
  externalEntities: Array<{ name: string; type: string; description: string }>;
  businessEvents: Array<{
    id: number;
    irEventId?: string;
    name: string;
    description: string;
    complexity: 'simple' | 'medium' | 'complex';
  }>;
  eventCount: number;
}

export interface DFDOutput {
  contextDiagram: any;
  dfdLevels: any;
  processes: Array<{ id: string; name: string; inputFlows: string[]; outputFlows: string[]; parentId?: string }>;
  dataFlows: Array<{ name: string }>;
  dataStores: Array<{ name: string }>;
}

export interface BPMOutput {
  swimLanes: any[];
  activityNodes: any[];
  edges: any[];
  exceptionPaths: any[];
  dfdProcessMappings: Record<string, string>;
}

export interface DictOutput {
  elements: Array<{
    name: string;
    type: string;
    isFK?: boolean;
    refEntity?: string;
    isRequired?: boolean;
  }>;
  dataFlows: Array<{ name: string; fields: any[] }>;
  dataStores: Array<{ name: string; fields: any[] }>;
}

export interface PSpecOutput {
  processSpecs: Array<{
    id: string;
    name: string;
    input: string[];
    output: string[];
    validation?: string;
    algorithm?: string;
  }>;
}

export interface DecisionTableOutput {
  tables: Array<{
    id: string;
    conditions: Array<{ name: string; operator: string; value: any }>;
    actions: Array<{ name: string }>;
    rules: Array<{ conditionMask: boolean[]; actionIndex: number }>;
  }>;
}

export interface EROutput {
  entities: Array<{ name: string; columns: any[] }>;
  relationships: any[];
}

export interface StateMachineOutput {
  stateMachines: Array<{
    entity: string;
    states: string[];
    transitions: Array<{ from: string; to: string; trigger: string }>;
  }>;
}

export interface UIOutput {
  screens: Array<{
    id: string;
    name: string;
    dataFlow: string;
    bpmNodeId: string;
    fields: Array<{ name: string; type: string; required: boolean; controlType: string }>;
  }>;
}

// =====================================================
// SA 总产出
// =====================================================

/**
 * 单个业务事件的分析结果。
 * steps 以 IR 步骤名为 key（匹配 C# SaStepMapping），便于 C# 直接投影 IR 事件。
 * PROJECT 级步骤（DomainModel/AggregateDesign/EventCatalog/CommandQuery/DataModel/UISpec）
 * 在所有事件中共享；事件级步骤（IntegrationPoints/WorkflowSpec/DeliveryChecklist）
 * 按 complexity 按需存在。
 */
export interface SAEventResult {
  /** IR 侧 eventId（如 BE-001），与 PM 骨架对齐 */
  eventId: string;
  eventName: string;
  complexity: 'simple' | 'medium' | 'complex';
  /** IR 步骤名 → 产出对象；缺失表示该步骤未执行（按复杂度裁剪） */
  steps: Record<string, any>;
  /** 非空表示该事件处理失败，steps 可能不完整 */
  error?: string;
}

export interface SAOutput {
  projectId: number;
  tenantId: string;
  scope: ScopeOutput;
  dfd?: DFDOutput;
  bpm?: BPMOutput;
  dict?: DictOutput;
  er?: EROutput;
  stateMachine?: StateMachineOutput;
  /** 每个业务事件的分析结果（含 PROJECT 级共享步骤 + 事件级专属步骤） */
  eventResults: SAEventResult[];
  metadata: {
    totalDuration: number;
    totalRetries: number;
    validationStats: { step: string; attempts: number; passed: boolean }[];
  };
}

// =====================================================
// SA 上下文(贯穿整个流水线)
// =====================================================
export interface SAContext {
  tenantId: string;
  projectId: number;
  /** 流水线实例 ID（三元组，落库 sa_* 表 pipeline_id 列） */
  pipelineId: number;
  requirementId: number;
  requirementText: string;
  eventId?: number;
  eventDescription?: string;
  assetLevel: 'PROJECT' | 'EVENT' | 'PROCESS';

  // 上下文注入
  kgPatterns: KGPattern[];                // 知识图谱注入
  domainModel: DomainModelContext;        // 领域模型注入
  previousSteps: Record<string, any>;     // 上一步产出(供下一步使用)

  // 运行时 ID（流水线执行过程中逐步填充）
  scopeId?: number;
  dfdId?: number;
  bpmId?: number;
  dictId?: number;
  pspecId?: number;
  decisionTableId?: number;
  erId?: number;
  stateMachineId?: number;
  uiId?: number;

  // 3-Tier 架构：Project 级全局字典 + 当前事件 ID
  projectDict?: DictOutput;              // Project 级全局字典（Phase 1 产出）
  currentEventId?: number;               // 当前正在处理的事件 ID

  // 跨事件判定表注入
  allDecisionTables?: any[];

  // 重试闭环
  lastErrors?: string[];

  // 元数据
  userId: string;
  startTime: number;
}

export interface KGPattern {
  id: string;
  type: 'field_naming' | 'decision_rule' | 'state_machine' | 'process_pattern';
  content: any;
  score: number;
  tags: string[];
}

export interface DomainModelContext {
  industry: string;
  standardFields: Array<{ name: string; type: string; description: string }>;
  standardEntities: Array<{ name: string; description: string; commonFields: string[] }>;
  standardProcesses: Array<{ id: string; name: string; description: string }>;
}

// =====================================================
// LLM 客户端接口(任何 LLM 都能接入)
// =====================================================
export interface ILLMClient {
  generate(params: {
    systemPrompt: string;
    context: Record<string, any>;
    lastErrors?: string[];
    temperature?: number;
  }): Promise<any>;
}

// =====================================================
// 数据库接口
// =====================================================
export interface ISADatabase {
  // 写入
  saveScope(scope: ScopeOutput, ctx: SAContext): Promise<{ id: number }>;
  saveDFD(dfd: DFDOutput, ctx: SAContext, scopeId: number): Promise<{ id: number }>;
  saveBPM(bpm: BPMOutput, ctx: SAContext, dfdId: number): Promise<{ id: number }>;
  saveDict(dict: DictOutput, ctx: SAContext, dfdId: number, bpmId: number): Promise<{ id: number }>;
  savePSpec(pspec: PSpecOutput, ctx: SAContext, dictId: number, bpmId: number): Promise<{ id: number }>;
  saveDecisionTable(dt: DecisionTableOutput, ctx: SAContext, pspecId: number, dictId: number): Promise<{ id: number }>;
  saveER(er: EROutput, ctx: SAContext, dictId: number): Promise<{ id: number }>;
  saveStateMachine(sm: StateMachineOutput, ctx: SAContext, dictId: number, bpmId: number): Promise<{ id: number }>;
  saveUI(ui: UIOutput, ctx: SAContext, bpmId: number, dictId: number): Promise<{ id: number }>;

  // 校验日志
  logValidation(record: ValidationLogRecord): Promise<void>;

  // 读取
  getProjectKGPatterns(projectId: number, limit?: number): Promise<KGPattern[]>;
  getDomainModel(industry: string): Promise<DomainModelContext>;
  getAllDecisionTablesInProject(projectId: number): Promise<any[]>;
}

export interface ValidationLogRecord {
  tenantId: string;
  projectId: number;
  pipelineId: number;
  saTableName: string;
  saRecordId?: number;
  validatorName: string;
  retryCount: number;
  previousErrors: string[] | null;
  isConverged: boolean;
  validationStatus: 'PASS' | 'FAIL';
  errors: ValidationError[];
  durationMs: number;
}

// =====================================================
// SA 配置
// =====================================================
export interface SAConfig {
  maxRetries: number;
  retryDelayMs: number;
  enableDKEE: boolean;
  enableCrossEventCheck: boolean;
  logLevel: 'debug' | 'info' | 'warn' | 'error';
}

export const DEFAULT_SA_CONFIG: SAConfig = {
  maxRetries: 5,
  retryDelayMs: 1000,
  enableDKEE: true,
  enableCrossEventCheck: true,
  logLevel: 'info',
};

// 重新导出 Validator 类型供 SDK 使用
export { ValidationError } from '../validators/types';
