/**
 * 编译器类型定义
 */

import type { FormPageIR } from '../../ir/types';

/** 生成的项目 = 文件路径 → 文件内容 */
export type GeneratedProject = Map<string, string>;

/** 编译器配置 */
export interface CompilerConfig {
  /** 实体名称（如 'student', 'order'） */
  entity: string;
  /** 实体中文名（如 '学生', '订单'） */
  entityLabel: string;
  /** API 基础路径（如 '/api/student'） */
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
