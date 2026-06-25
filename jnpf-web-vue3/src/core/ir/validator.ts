/**
 * IR 验证器
 *
 * 职责：验证 IR 的结构完整性（必填字段、引用完整性）
 * 不做：语义正确性验证（由编译器负责）
 */

import type { FormPageIR } from './types';

export interface ValidationIssue {
  level: 'error' | 'warn' | 'ai-quality';
  path: string;
  message: string;
  suggestion?: string;
}

export function validateIR(ir: FormPageIR): ValidationIssue[] {
  const issues: ValidationIssue[] = [];

  // 1. 必填字段
  if (!ir.id) issues.push({ level: 'error', path: 'id', message: '缺少 id' });
  if (!ir.name) issues.push({ level: 'warn', path: 'name', message: '缺少 name' });
  if (!ir.fields?.length) {
    issues.push({ level: 'error', path: 'fields', message: '字段列表为空' });
  }

  // 2. 字段完整性
  for (let i = 0; i < (ir.fields?.length ?? 0); i++) {
    const field = ir.fields[i];
    const prefix = `fields[${i}]`;
    if (!field.model) issues.push({ level: 'error', path: `${prefix}.model`, message: '缺少 model' });
    if (!field.label) issues.push({ level: 'warn', path: `${prefix}.label`, message: '缺少 label' });
    if (!field.component?.jnpfKey) issues.push({ level: 'error', path: `${prefix}.component.jnpfKey`, message: '缺少组件类型' });
    if (!field.component?.pc) issues.push({ level: 'warn', path: `${prefix}.component.pc`, message: '缺少 PC 端组件映射' });
    if (!field.component?.app) issues.push({ level: 'warn', path: `${prefix}.component.app`, message: '缺少 App 端组件映射' });
  }

  // 3. 表达式引用完整性
  const exprIds = new Set(ir.expressions?.map(e => e.id) ?? []);
  for (let i = 0; i < (ir.fields?.length ?? 0); i++) {
    const field = ir.fields[i];
    if (!field.events) continue;
    for (const [event, exprId] of Object.entries(field.events)) {
      if (exprId && !exprIds.has(exprId)) {
        issues.push({
          level: 'error',
          path: `fields[${i}].events.${event}`,
          message: `引用了不存在的表达式: ${exprId}`,
        });
      }
    }
  }

  // 4. 表达式级别统计
  const complexCount = ir.expressions?.filter(e => e.level === 'complex').length ?? 0;
  if (complexCount > 0) {
    issues.push({
      level: 'warn',
      path: 'expressions',
      message: `有 ${complexCount} 个复杂表达式需人工迁移`,
    });
  }

  // 5. AI 质量探针（当前为空壳，等知识图谱就位后填充）
  issues.push(...validateAIQuality(ir));

  return issues;
}

function validateAIQuality(_ir: FormPageIR): ValidationIssue[] {
  const issues: ValidationIssue[] = [];
  // 占位：等知识图谱和领域规则就位后启用
  // 示例：
  // if (ir.aiHints?.domain === 'mes' && !ir.fields.some(f => f.model === 'equipmentId')) {
  //   issues.push({
  //     level: 'ai-quality', path: 'fields',
  //     message: 'MES 领域通常需要设备相关字段',
  //     suggestion: '考虑添加 equipmentId, equipmentName'
  //   });
  // }
  return issues;
}

export function hasErrors(issues: ValidationIssue[]): boolean {
  return issues.some(i => i.level === 'error');
}
