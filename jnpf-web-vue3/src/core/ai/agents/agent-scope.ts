/**
 * AgentScope — Agent 视图机制（P0 审判令一）
 *
 * 每个 Agent 注册一个固定的 AgentScope，定义其可读、可写的 IR 节点类型。
 * Agent 只能操作自己视图范围内的节点，跨视图修改通过 OrchestratorAgent 协调。
 *
 * @module ai/agents/agent-scope
 * @version 1.0.0
 */

import type { FormPageIR } from '../../ir/types';

// ============================================================
// 类型定义
// ============================================================

/** IR 节点类型（与 types.ts 中的定义一致） */
export type IRNodeType =
  | 'Requirement'
  | 'ModuleDefinition'
  | 'TableDefinition'
  | 'Component'
  | 'DataSource'
  | 'WorkflowStep'
  | 'RuleNode'
  | 'CompileConfig'
  | 'Field'
  | 'Expression'
  | 'Action'
  | 'Validation';

export interface AgentScope {
  /** Agent 名称 */
  agentName: string;
  /** 该 Agent 负责产出的节点类型 */
  ownedNodeTypes: IRNodeType[];
  /** 该 Agent 可读的节点类型。包含 '*' 时表示概览读取全部（不展开内部字段） */
  readableNodeTypes: (IRNodeType | '*')[];
  /** 分治编辑时可写的节点类型 */
  writableNodeTypes: IRNodeType[];
}

export interface AgentSlice {
  /** 切片内的 IR 节点 */
  nodes: unknown[];
  /** 这些节点在整体 IR 中的角色描述 */
  context: string;
  /** Agent 可修改的节点 ID 列表（从 writableNodeTypes 推导） */
  writableNodeIds: string[];
}

// ============================================================
// 6 个 Agent 的静态注册表（编译时常量）
// ============================================================

export const AGENT_SCOPES: AgentScope[] = [
  {
    agentName: 'RequirementAnalystAgent',
    ownedNodeTypes: ['Requirement'],
    readableNodeTypes: ['*'],
    writableNodeTypes: ['Requirement'],
  },
  {
    agentName: 'ArchitectAgent',
    ownedNodeTypes: ['ModuleDefinition', 'TableDefinition'],
    readableNodeTypes: ['Requirement', 'ModuleDefinition'],
    writableNodeTypes: ['ModuleDefinition', 'TableDefinition'],
  },
  {
    agentName: 'UIAgent',
    ownedNodeTypes: ['Component'],
    readableNodeTypes: ['Component', 'DataSource', 'Requirement'],
    writableNodeTypes: ['Component'],
  },
  {
    agentName: 'DatabaseAgent',
    ownedNodeTypes: ['TableDefinition', 'DataSource'],
    readableNodeTypes: ['TableDefinition', 'DataSource', 'Requirement'],
    writableNodeTypes: ['TableDefinition', 'DataSource'],
  },
  {
    agentName: 'WorkflowAgent',
    ownedNodeTypes: ['WorkflowStep'],
    readableNodeTypes: ['WorkflowStep', 'Component'],
    writableNodeTypes: ['WorkflowStep'],
  },
  {
    agentName: 'RuleEngineAgent',
    ownedNodeTypes: ['RuleNode'],
    readableNodeTypes: ['RuleNode', 'Component', 'Requirement'],
    writableNodeTypes: ['RuleNode'],
  },
];

// ============================================================
// 切片提取
// ============================================================

/**
 * 从 IR 中按 Scope 过滤提取切片。
 *
 * 当 readableNodeTypes 包含 '*' 时，返回所有节点的摘要视图
 * （只包含 nodeId + type + name，不展开内部字段）。
 */
export function extractSlice(ir: FormPageIR, scope: AgentScope): AgentSlice {
  const allNodes = collectAllNodes(ir);
  const readableTypes = scope.readableNodeTypes;

  // 判断是否需要摘要视图
  const needsSummary = readableTypes.includes('*');

  let nodes: unknown[];
  if (needsSummary) {
    // 摘要视图：所有节点只暴露 id + type + name
    nodes = allNodes.map((n: Record<string, unknown>) => ({
      id: n.id,
      type: (n as Record<string, unknown>).type,
      name: (n as Record<string, unknown>).name,
    }));
  } else {
    // 精确过滤：只返回 readableNodeTypes 中指定的类型
    nodes = allNodes.filter((n: Record<string, unknown>) => readableTypes.includes((n as Record<string, unknown>).type as IRNodeType));
  }

  // 从 writableNodeTypes 推导可写节点 ID
  const writableNodeIds = allNodes
    .filter((n: Record<string, unknown>) => scope.writableNodeTypes.includes((n as Record<string, unknown>).type as IRNodeType))
    .map((n: Record<string, unknown>) => n.id as string);

  // 生成上下文描述
  const context =
    `Agent [${scope.agentName}] 切片：${nodes.length} 个可读节点，${writableNodeIds.length} 个可写节点。` +
    `可写类型：${scope.writableNodeTypes.join(', ')}。` +
    `可读类型：${needsSummary ? '全部（摘要视图）' : scope.readableNodeTypes.join(', ')}。`;

  return { nodes, context, writableNodeIds };
}

// ============================================================
// 内部工具
// ============================================================

/** 从 FormPageIR 中收集所有可枚举的节点 */
function collectAllNodes(ir: FormPageIR): unknown[] {
  const nodes: unknown[] = [];

  // fields
  if (ir.fields) {
    for (const f of ir.fields) {
      nodes.push({ id: f.id || '', type: 'Field', name: f.label || '' });
    }
  }

  // databaseFields → DataSource
  if (ir.databaseFields) {
    for (const d of ir.databaseFields) {
      nodes.push({ id: d.name || '', type: 'DataSource', name: d.name || '' });
    }
  }

  // expressions
  if (ir.expressions) {
    for (const e of ir.expressions) {
      nodes.push({ id: e.id || '', type: 'Expression', name: e.name || '' });
    }
  }

  // listConfig → Component
  if (ir.listConfig) {
    nodes.push({
      id: `${ir.id}_list`,
      type: 'Component',
      name: `${ir.name}_list`,
    });
    if (ir.listConfig.columns) {
      for (const c of ir.listConfig.columns) {
        nodes.push({ id: c.field || '', type: 'Field', name: c.label || c.field || '' });
      }
    }
  }

  // workflow → WorkflowStep
  if (ir.workflow?.templateList) {
    for (const t of ir.workflow.templateList) {
      nodes.push({ id: t.id || '', type: 'WorkflowStep', name: t.name || '' });
    }
  }

  return nodes;
}
