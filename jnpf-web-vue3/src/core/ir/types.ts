/**
 * JNPF 前端 IR（Intermediate Representation）类型定义
 *
 * 这是整个代码生成体系的"宪法"。
 * 所有编译器和清洗器都基于这套类型。
 * 修改此文件需要架构师审批。
 *
 * @version 1.0.0
 */

// ============================================================
// 应用级 IR
// ============================================================

export interface ApplicationIR {
  version: '1.0';
  pages: PageIR[];
}

export type PageIR = FormPageIR | ListPageIR;

// ============================================================
// 页面级 IR
// ============================================================

export interface FormPageIR {
  type: 'form';
  id: string;
  name: string;
  config: FormConfig;
  fields: FieldIR[];
  databaseFields: DatabaseFieldIR[];
  expressions: ExpressionIR[];
  listConfig?: ListConfigIR;
  mobileConfig?: MobileConfigIR;
  workflow?: WorkflowIR;
  /** AI 探针 — 为顾问式 AI 预留的上下文接口 */
  aiHints?: {
    domain?: string;
    requirement?: string;
    designRationale?: string;
    confidence?: number;
  };
}

export interface ListPageIR {
  type: 'list';
  id: string;
  name: string;
  columns: ColumnIR[];
  searchFields: SearchFieldIR[];
  actions: ActionIR[];
}

// ============================================================
// 表单配置
// ============================================================

export interface FormConfig {
  labelPosition: 'left' | 'right' | 'top';
  labelWidth: number;
  labelSuffix: string;
  size: 'large' | 'default' | 'small';
  disabled: boolean;
  span: number;
  gutter: number;
  colon: boolean;
  popupType: 'general' | 'fullScreen' | 'drawer';
  generalWidth: string;
  fullScreenWidth: string;
  drawerWidth: string;
  hasCancelBtn: boolean;
  cancelButtonText: string;
  hasConfirmBtn: boolean;
  confirmButtonText: string;
  hasConfirmAndAddBtn: boolean;
  hasPrintBtn: boolean;
  printButtonText: string;
  primaryKeyPolicy: string;
  tablePolicy: string;
  concurrencyLock: boolean;
  logicalDelete: boolean;
}

// ============================================================
// 字段级 IR
// ============================================================

export interface FieldIR {
  id: string;
  model: string;
  label: string;
  component: ComponentMapping;
  config: FieldConfig;
  validation: ValidationRuleIR[];
  events: {
    change?: string;
    blur?: string;
    focus?: string;
    click?: string;
  };
  /** AI 探针 — 为顾问式 AI 预留的字段级上下文 */
  aiHints?: {
    semantic?: string;
    suggestedValidation?: string;
    suggestedDefault?: string;
  };
}

export interface ComponentMapping {
  jnpfKey: string;
  pc: string;
  app: string;
}

export interface FieldConfig {
  required: boolean;
  defaultValue: unknown;
  placeholder: string;
  disabled: boolean;
  readonly: boolean;
  hidden: boolean;
  span: number;
  labelWidth: number | null;
  maxlength: number | null;
  showWordLimit: boolean;
  clearable: boolean;
  min: number | null;
  max: number | null;
  precision: number | null;
  step: number | null;
  multiple: boolean;
  options: OptionItemIR[];
  dictType: string | null;
  relationData: {
    url: string | null;
    labelField: string | null;
    valueField: string | null;
  } | null;
  style: Record<string, string>;
}

export interface OptionItemIR {
  label: string;
  value: unknown;
  disabled?: boolean;
}

export interface ValidationRuleIR {
  type: 'required' | 'string' | 'number' | 'email' | 'phone' | 'idcard' | 'url' | 'pattern' | 'custom';
  message: string;
  pattern?: string;
  min?: number;
  max?: number;
  trigger: 'blur' | 'change';
}

// ============================================================
// 表达式 IR
// ============================================================

export interface ExpressionIR {
  id: string;
  name: string;
  type: 'form-lifecycle' | 'field-event' | 'computed' | 'validation';
  params: string[];
  body: string;
  level: 'empty' | 'simple' | 'medium' | 'complex';
  isAsync: boolean;
  originalCode: string;
}

// ============================================================
// 数据库字段
// ============================================================

export interface DatabaseFieldIR {
  id: string;
  name: string;
  type: string;
  length: number | null;
  nullable: boolean;
  defaultValue: unknown;
  description: string;
}

// ============================================================
// 列表配置
// ============================================================

export interface ListConfigIR {
  searchFields: SearchFieldIR[];
  columns: ColumnIR[];
  ruleList: unknown[];
}

export interface SearchFieldIR {
  field: string;
  label: string;
  component: string;
  options: OptionItemIR[];
}

export interface ColumnIR {
  field: string;
  label: string;
  width: number | null;
  fixed: 'left' | 'right' | null;
  sortable: boolean;
  formatter?: string;
}

export interface ActionIR {
  type: 'add' | 'edit' | 'delete' | 'batch-delete' | 'import' | 'export' | 'custom';
  label: string;
  permission: string | null;
}

// ============================================================
// 移动端配置
// ============================================================

export interface MobileConfigIR {
  formFields: FieldIR[];
  listColumns: ColumnIR[];
}

// ============================================================
// 工作流绑定
// ============================================================

export interface WorkflowIR {
  flowId: string;
  flowType: string;
  templateList: { id: string; name: string }[];
}
