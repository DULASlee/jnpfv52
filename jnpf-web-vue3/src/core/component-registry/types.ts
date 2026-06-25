/**
 * 组件注册表类型定义
 *
 * 遵循 SemVer 2.0 版本规范
 */

export interface ComponentEntry {
  /** 唯一标识（如 'JnpfInput', 'ECharts:Bar'） */
  type: string;

  /** 显示名称（如 '输入框', '柱状图'） */
  name: string;

  /** 分类 */
  category: ComponentCategory;

  /** PC 端组件名（Ant Design Vue） */
  pc: string;

  /** 移动端组件名（UniApp） */
  app: string;

  /** 组件版本，遵循 SemVer 2.0 (major.minor.patch) */
  version?: string;

  /** 是否已废弃。查找时会打印警告。 */
  deprecated?: boolean;

  /** 废弃后替代的组件类型。仅在 deprecated=true 时有意义。 */
  replacedBy?: string;

  /** 默认属性 */
  defaultProps?: Record<string, unknown>;

  /** 属性定义（设计器配置面板用，后续扩展） */
  propSchema?: PropSchema[];
}

export type ComponentCategory =
  | 'form-input'
  | 'form-select'
  | 'form-datetime'
  | 'form-switch'
  | 'form-upload'
  | 'form-special'
  | 'layout'
  | 'data-display'
  | 'chart'
  | 'popup'
  | 'other';

export interface PropSchema {
  name: string;
  label: string;
  type: 'string' | 'number' | 'boolean' | 'select' | 'color' | 'json';
  default?: unknown;
  options?: { label: string; value: unknown }[];
  required?: boolean;
}
