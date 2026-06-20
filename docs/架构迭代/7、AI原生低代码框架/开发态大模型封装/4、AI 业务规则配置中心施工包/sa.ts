// SA 资产 TypeScript 类型(与后端 SDK 对齐)

// 验证状态
export type ValidationStatus = 'PASS' | 'FAIL' | 'PENDING';

// 数据字典
export interface FieldElement {
  name: string;
  type: string;
  isFK?: boolean;
  refEntity?: string;
  isRequired?: boolean;
  scope?: string;
}

export interface DataFlowDef {
  name: string;
  fields: FieldElement[];
}

export interface DataStoreDef {
  name: string;
  fields: FieldElement[];
}

export interface DataDictionary {
  id: number;
  project_id: number;
  elements: FieldElement[];
  dataFlows: DataFlowDef[];
  dataStores: DataStoreDef[];
  validation_status: ValidationStatus;
  human_confirmed: boolean;
  is_pattern_source: boolean;
  updated_at: string;
}

// 判定表
export interface DecisionTable {
  id: string;
  project_id: number;
  event_id: number;
  conditions: Array<{ name: string; operator: string; value: any }>;
  actions: Array<{ name: string }>;
  rules: Array<{ conditionMask: boolean[]; actionIndex: number }>;
  cross_event_consistency?: boolean;
  validation_status: ValidationStatus;
  human_confirmed: boolean;
  is_pattern_source: boolean;
  updated_at: string;
}

// 状态机
export interface StateMachine {
  entity: string;
  states: string[];
  transitions: Array<{ from: string; to: string; trigger: string }>;
  states_in_dict?: boolean;
  validation_status: ValidationStatus;
  human_confirmed: boolean;
  is_pattern_source: boolean;
  updated_at: string;
}

// 项目概览
export interface SAProject {
  projectId: number;
  tenantId: string;
  requirementText: string;
  status: 'analyzing' | 'awaiting_review' | 'completed';
  scope?: any;
  validationStats: {
    scope: ValidationStatus;
    dfd: ValidationStatus;
    dict: ValidationStatus;
    decisionTable: ValidationStatus;
    er: ValidationStatus;
    ui: ValidationStatus;
  };
  createdAt: string;
  updatedAt: string;
}

// 修改记录(供 DKEE 学习)
export interface ChangeRecord {
  table: 'sa_data_dictionary' | 'sa_decision_table' | 'sa_state_machine';
  recordId: number;
  field: string;
  before: any;
  after: any;
  userId: string;
  reason?: string;
  timestamp: string;
}

// DKEE Pattern
export interface KGPattern {
  id: number;
  pattern_type: 'field_naming' | 'decision_rule' | 'state_machine';
  industry: string;
  pattern_content: any;
  score: number;
  usage_count: number;
  source: 'human-created' | 'ai-discovered' | 'self-play';
  is_active: boolean;
}
