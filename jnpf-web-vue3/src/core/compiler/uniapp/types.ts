/**
 * UniApp 编译器类型定义
 *
 * @jnpf-generated v5.2.0 type=compiler-types platform=uniapp
 */

// ============================================================
// 编译器配置 & 结果
// ============================================================

/** 生成的项目 = 文件路径 → 文件内容 */
export type GeneratedProject = Map<string, string>;

/** 编译器配置 */
export interface CompilerConfig {
  /** 实体名称（如 'student', 'order'） */
  entity: string;
  /** 实体中文名（如 '学生', '订单'） */
  entityLabel: string;
  /** API 基础路径（如 '/api/System/User'） */
  apiBasePath: string;
  /** 生成标记版本 */
  generatorVersion: string;
}

/** 编译结果 */
export interface CompileResult {
  project: GeneratedProject;
  warnings: string[];
  complexExpressions: string[];
}

// ============================================================
// 精简版 IR 类型（从 jnpf-web-vue3/core/ir/types.ts 提取）
// 编译器只需要字段级别的信息，完整的 IR 定义在 web 端
// ============================================================

export interface ComponentMapping {
  jnpfKey: string;
  pc: string;
  app: string;
  legacyApp: string;
}

export interface FieldConfigIR {
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
  options: FieldOptionIR[];
  dictType: string | null;
  style: Record<string, string>;
}

export interface FieldOptionIR {
  label: string;
  value: unknown;
  disabled?: boolean;
}

export interface FieldIR {
  id: string;
  model: string;
  label: string;
  component: ComponentMapping;
  config: FieldConfigIR;
}

export interface ExpressionIR {
  id: string;
  name: string;
  type: string;
  params: string[];
  body: string;
  level: 'empty' | 'simple' | 'medium' | 'complex';
  isAsync: boolean;
  originalCode: string;
}

export interface SearchFieldIR {
  field: string;
  label: string;
  component: string;
}

export interface ListConfigIR {
  searchFields: SearchFieldIR[];
}

export interface FormPageIR {
  type: 'form';
  id: string;
  name: string;
  config?: Record<string, unknown>;
  fields: FieldIR[];
  expressions: ExpressionIR[];
  listConfig?: ListConfigIR;
}
