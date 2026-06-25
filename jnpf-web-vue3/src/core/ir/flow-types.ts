/**
 * FlowIR — 工作流中间表示
 *
 * 与 FormPageIR / DashboardIR 平级，都是 IR 联合类型的成员。
 * 设计目标：平台工作流 JSON ↔ FlowIR ↔ 可执行状态机。
 *
 * @jnpf-generated v5.2.0 type=ir-flow platform=universal
 */

// ─── 工作流整体 ───

export interface FlowIR {
  type: 'workflow';
  id: string;
  name: string;
  version: string;
  nodes: FlowNode[];
  edges: FlowEdge[];
  variables: FlowVariable[];
  aiHints?: {
    domain?: string;
    scenario?: string;
    complexity?: 'simple' | 'medium' | 'complex';
    estimatedSteps?: number;
  };
}

// ─── 节点类型 ───

export type FlowNodeType = 'start' | 'end' | 'approval' | 'condition' | 'parallel' | 'subprocess' | 'notification' | 'script' | 'timer';

export interface FlowNode {
  id: string;
  type: FlowNodeType;
  name: string;
  config: FlowNodeConfig;
  position: { x: number; y: number };
}

// ─── 节点配置（判别联合）───

export type FlowNodeConfig =
  | StartNodeConfig
  | EndNodeConfig
  | ApprovalNodeConfig
  | ConditionNodeConfig
  | ParallelNodeConfig
  | SubprocessNodeConfig
  | NotificationNodeConfig
  | ScriptNodeConfig
  | TimerNodeConfig;

export interface StartNodeConfig {
  type: 'start';
  triggerType: 'manual' | 'api' | 'event' | 'schedule';
}

export interface EndNodeConfig {
  type: 'end';
  onEnd?: 'notify' | 'archive' | 'both';
}

export interface ApprovalNodeConfig {
  type: 'approval';
  approverType: 'user' | 'role' | 'department' | 'auto';
  approverIds: string[];
  assignPolicy: 'all' | 'any' | 'sequential';
  timeout?: number;
  timeoutAction?: 'auto_pass' | 'auto_reject' | 'notify';
  formFields?: string[];
  commentRequired?: boolean;
}

export interface ConditionNodeConfig {
  type: 'condition';
  conditions: Array<{
    field: string;
    operator: '==' | '!=' | '>' | '>=' | '<' | '<=' | 'contains' | 'in';
    value: unknown;
    nextNodeId: string;
  }>;
  defaultNodeId: string;
}

export interface ParallelNodeConfig {
  type: 'parallel';
  branches: string[];
  joinPolicy: 'all' | 'any';
}

export interface SubprocessNodeConfig {
  type: 'subprocess';
  subprocessId: string;
  inputMapping?: Record<string, string>;
  outputMapping?: Record<string, string>;
}

export interface NotificationNodeConfig {
  type: 'notification';
  channel: 'sms' | 'email' | 'wechat' | 'system';
  templateId: string;
  recipients: Array<{ type: 'user' | 'role' | 'field'; value: string }>;
}

export interface ScriptNodeConfig {
  type: 'script';
  scriptType: 'expression' | 'function';
  content: string;
}

export interface TimerNodeConfig {
  type: 'timer';
  delay: number;
  delayType: 'fixed' | 'business_hours';
}

// ─── 连线 ───

export interface FlowEdge {
  id: string;
  sourceNodeId: string;
  targetNodeId: string;
  label?: string;
  condition?: string;
}

// ─── 变量 ───

export interface FlowVariable {
  id: string;
  name: string;
  type: 'string' | 'number' | 'boolean' | 'date' | 'array' | 'object';
  defaultValue?: unknown;
  scope: 'global' | 'node';
  nodeId?: string;
}

// ─── 运行时快照 ───

export interface FlowSnapshot {
  flowId: string;
  currentNodeId: string;
  variables: Record<string, unknown>;
  history: Array<{
    nodeId: string;
    action: string;
    timestamp: string;
    operator?: string;
  }>;
  status: 'running' | 'paused' | 'completed' | 'rejected' | 'error';
}

// ─── 验证结果 ───

export interface FlowIRIssue {
  level: 'error' | 'warning';
  path: string;
  message: string;
  code: string;
}

// ============================================================
// 验证器
// ============================================================

/**
 * 验证 FlowIR 结构完整性
 * 规则：
 * 1. start 节点必须存在且唯一
 * 2. end 节点至少存在一个
 * 3. 每个 edge 的 source/target 必须对应存在的节点
 * 4. condition 节点的 conditions.nextNodeId 必须存在
 * 5. parallel 节点的 branches 不能为空
 * 6. 无孤立节点（除 start 外至少一条入边，除 end 外至少一条出边）
 * 7. 无自环
 */
export function validateFlowIR(ir: FlowIR): FlowIRIssue[] {
  const issues: FlowIRIssue[] = [];
  const nodeIds = new Set(ir.nodes.map(n => n.id));

  // Rule 1: start 节点唯一
  const starts = ir.nodes.filter(n => n.type === 'start');
  if (starts.length === 0) {
    issues.push({
      level: 'error',
      path: 'nodes',
      message: '缺少 start 节点',
      code: 'MISSING_START',
    });
  } else if (starts.length > 1) {
    issues.push({
      level: 'error',
      path: 'nodes',
      message: `发现 ${starts.length} 个 start 节点，只能有 1 个`,
      code: 'DUPLICATE_START',
    });
  }

  // Rule 2: end 节点至少一个
  const ends = ir.nodes.filter(n => n.type === 'end');
  if (ends.length === 0) {
    issues.push({
      level: 'warning',
      path: 'nodes',
      message: '缺少 end 节点',
      code: 'MISSING_END',
    });
  }

  // Rule 3: edge 引用有效性
  for (const edge of ir.edges) {
    if (!nodeIds.has(edge.sourceNodeId)) {
      issues.push({
        level: 'error',
        path: `edges.${edge.id}`,
        message: `sourceNodeId "${edge.sourceNodeId}" 不是有效节点`,
        code: 'INVALID_EDGE_SOURCE',
      });
    }
    if (!nodeIds.has(edge.targetNodeId)) {
      issues.push({
        level: 'error',
        path: `edges.${edge.id}`,
        message: `targetNodeId "${edge.targetNodeId}" 不是有效节点`,
        code: 'INVALID_EDGE_TARGET',
      });
    }
    // Rule 7: 无自环
    if (edge.sourceNodeId === edge.targetNodeId) {
      issues.push({
        level: 'error',
        path: `edges.${edge.id}`,
        message: `节点 "${edge.sourceNodeId}" 存在自环`,
        code: 'SELF_LOOP',
      });
    }
  }

  // Rule 4: condition 条件引用有效性
  for (const node of ir.nodes) {
    if (node.type === 'condition') {
      const config = node.config as ConditionNodeConfig;
      for (let i = 0; i < config.conditions.length; i++) {
        if (!nodeIds.has(config.conditions[i].nextNodeId)) {
          issues.push({
            level: 'error',
            path: `nodes.${node.id}.conditions[${i}]`,
            message: `nextNodeId "${config.conditions[i].nextNodeId}" 不是有效节点`,
            code: 'INVALID_CONDITION_TARGET',
          });
        }
      }
      if (config.defaultNodeId && !nodeIds.has(config.defaultNodeId)) {
        issues.push({
          level: 'error',
          path: `nodes.${node.id}.defaultNodeId`,
          message: `defaultNodeId "${config.defaultNodeId}" 不是有效节点`,
          code: 'INVALID_DEFAULT_TARGET',
        });
      }
    }

    // Rule 5: parallel branches 非空
    if (node.type === 'parallel') {
      const config = node.config as ParallelNodeConfig;
      if (config.branches.length === 0) {
        issues.push({
          level: 'error',
          path: `nodes.${node.id}.branches`,
          message: 'parallel 节点至少需要一个分支',
          code: 'EMPTY_BRANCHES',
        });
      }
      for (const branchId of config.branches) {
        if (!nodeIds.has(branchId)) {
          issues.push({
            level: 'error',
            path: `nodes.${node.id}.branches`,
            message: `分支节点 "${branchId}" 不是有效节点`,
            code: 'INVALID_BRANCH_TARGET',
          });
        }
      }
    }
  }

  // Rule 6: 孤立节点检测
  const edgeSources = new Set(ir.edges.map(e => e.sourceNodeId));
  const edgeTargets = new Set(ir.edges.map(e => e.targetNodeId));

  for (const node of ir.nodes) {
    // start 节点可以没有入边
    if (node.type === 'start') continue;
    // end 节点可以没有出边
    if (node.type === 'end') continue;

    if (!edgeTargets.has(node.id) && (node.type as string) !== 'start') {
      issues.push({
        level: 'warning',
        path: `nodes.${node.id}`,
        message: `节点 "${node.name}" 没有入边`,
        code: 'ORPHAN_NODE_NO_INPUT',
      });
    }
    if (!edgeSources.has(node.id) && (node.type as string) !== 'end') {
      issues.push({
        level: 'warning',
        path: `nodes.${node.id}`,
        message: `节点 "${node.name}" 没有出边`,
        code: 'ORPHAN_NODE_NO_OUTPUT',
      });
    }
  }

  return issues;
}

/** 检查是否存在 error 级别的问题 */
export function hasErrors(issues: FlowIRIssue[]): boolean {
  return issues.some(i => i.level === 'error');
}
