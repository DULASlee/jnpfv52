// DKEE Pattern 类型定义

export type PatternType = 'field_naming' | 'decision_rule' | 'state_machine' | 'process_pattern';
export type PatternSource = 'human-created' | 'ai-discovered' | 'self-play';
export type IndustryType = 'manufacturing' | 'ecommerce' | 'optical' | 'general';

// =====================================================
// 源记录(SAS 表查出来的原始数据)
// =====================================================
export interface DictSourceRecord {
  id: number;
  project_id: number;
  elements: Array<{
    name: string;
    type: string;
    isFK?: boolean;
    refEntity?: string;
    isRequired?: boolean;
  }>;
  data_flows: Array<{ name: string; fields: any[] }>;
  data_stores: Array<{ name: string; fields: any[] }>;
  tags: any[];
  pattern_tags: any[];
}

export interface DecisionTableSourceRecord {
  id: number;
  project_id: number;
  tables: Array<{
    id: string;
    conditions: Array<{ name: string; operator: string; value: any }>;
    actions: Array<{ name: string }>;
    rules: any[];
  }>;
  cross_event_consistency: boolean;
}

export interface StateMachineSourceRecord {
  id: number;
  project_id: number;
  state_machines: Array<{
    entity: string;
    states: string[];
    transitions: Array<{ from: string; to: string; trigger: string }>;
  }>;
  states_in_dict: boolean;
}

// =====================================================
// 提炼后的 Pattern
// =====================================================
export interface BasePattern {
  id?: number;
  type: PatternType;
  industry: IndustryType;
  source: PatternSource;
  sourceProjects: number[];
  sourceRecords: Array<{ saTable: string; recordId: number; version: number }>;
  patternTags?: string[];
  notes?: string;
}

// 1. 字段命名 Pattern
export interface FieldNamingPattern extends BasePattern {
  type: 'field_naming';
  commonFields: Array<{
    name: string;
    type: string;
    frequency: number;
    isFK: boolean;
    isRequired: boolean;
    refEntity?: string;
  }>;
  fieldCount: number;
  minOccurrenceThreshold: number;  // 至少出现几次才进 Pattern
}

// 2. 业务规则 Pattern
export interface DecisionRulePattern extends BasePattern {
  type: 'decision_rule';
  ruleSet: Array<{
    condition: string;
    operator: string;
    threshold: any;
    action: string;
    frequency: number;
  }>;
  hasDefaultRule: boolean;
  ruleCount: number;
}

// 3. 状态机 Pattern
export interface StateMachinePattern extends BasePattern {
  type: 'state_machine';
  entity: string;
  standardStates: string[];
  standardTransitions: Array<{
    from: string;
    to: string;
    trigger: string;
    frequency: number;
  }>;
}

// 4. 流程 Pattern
export interface ProcessPattern extends BasePattern {
  type: 'process_pattern';
  standardProcesses: Array<{
    id: string;
    name: string;
    frequency: number;
  }>;
}

export type AnyPattern = FieldNamingPattern | DecisionRulePattern | StateMachinePattern | ProcessPattern;

// =====================================================
// 提炼结果
// =====================================================
export interface ExtractionResult {
  industry: IndustryType;
  patternsExtracted: number;
  patternsSaved: number;
  patternsUpdated: number;
  newPatterns: AnyPattern[];
  updatedPatterns: AnyPattern[];
  durationMs: number;
}
