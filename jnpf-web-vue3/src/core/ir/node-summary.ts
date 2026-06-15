/**
 * IR 节点摘要生成器（P1）
 *
 * 从 IR 生成节点摘要列表，供 LLM 分治编辑的节点定位步骤使用。
 * 节点数 <= 300 时用完整版（含 description），> 300 时用紧凑版（省略 description 以减少 token 消耗）。
 *
 * @module ir/node-summary
 * @version 1.0.0
 */

import type { FormPageIR } from './types';

// ============================================================
// 类型定义
// ============================================================

/** 单个节点摘要 */
interface NodeSummary {
  id: string;
  type: string;
  label: string;
  description: string;
}

// ============================================================
// 公开函数
// ============================================================

/**
 * 从 IR 生成节点摘要列表，供 LLM 列表选择。
 *
 * 输出格式：每行 `[nodeId] NodeType: label — description`
 */
export function buildNodeSummaryList(ir: FormPageIR): string {
  const nodes = collectAllSummaries(ir);
  if (nodes.length <= 300) {
    return formatFull(nodes);
  }
  return formatCompact(nodes);
}

/**
 * 紧凑版本：省略 description，减少 token 消耗。
 * 用于大 IR（> 300 节点）。
 */
export function buildCompactNodeSummaryList(ir: FormPageIR): string {
  return formatCompact(collectAllSummaries(ir));
}

// ============================================================
// 内部实现
// ============================================================

function collectAllSummaries(ir: FormPageIR): NodeSummary[] {
  const summaries: NodeSummary[] = [];

  // fields
  if (ir.fields) {
    for (const f of ir.fields) {
      summaries.push({
        id: f.id,
        type: f.component?.jnpfKey || 'Field',
        label: f.label,
        description: buildFieldDescription(f),
      });
    }
  }

  // databaseFields -> DataSource
  if (ir.databaseFields) {
    for (const d of ir.databaseFields) {
      summaries.push({
        id: d.id,
        type: 'DataSource',
        label: d.name,
        description: `数据库字段: ${d.name} (${d.type}), nullable=${d.nullable}`,
      });
    }
  }

  // expressions
  if (ir.expressions) {
    for (const e of ir.expressions) {
      summaries.push({
        id: e.id || '',
        type: 'Expression',
        label: e.name || '',
        description: `表达式: ${e.name || 'unnamed'}`,
      });
    }
  }

  // listConfig columns -> pseudo-fields
  if (ir.listConfig?.columns) {
    for (const c of ir.listConfig.columns) {
      summaries.push({
        id: c.field,
        type: 'Column',
        label: c.label,
        description: `列表列: ${c.label} (${c.field}), 宽度=${c.width ?? 'auto'}`,
      });
    }
  }

  // workflow templates
  if (ir.workflow?.templateList) {
    for (const t of ir.workflow.templateList) {
      summaries.push({
        id: t.id,
        type: 'WorkflowTemplate',
        label: t.name,
        description: `工作流模板: ${t.name}`,
      });
    }
  }

  return summaries;
}

function buildFieldDescription(f: import('./types').FieldIR): string {
  const parts: string[] = [];
  parts.push(`字段: ${f.label}`);
  if (f.config?.required) parts.push('必填');
  if (f.validation?.length) parts.push(`校验规则: ${f.validation.length}条`);
  if (f.config?.placeholder) parts.push(`占位: ${f.config.placeholder}`);
  return parts.join(', ');
}

function formatFull(nodes: NodeSummary[]): string {
  return nodes.map(n => `[${n.id}] ${n.type}: ${n.label} — ${n.description}`).join('\n');
}

function formatCompact(nodes: NodeSummary[]): string {
  return nodes.map(n => `[${n.id}] ${n.type}: ${n.label}`).join('\n');
}
