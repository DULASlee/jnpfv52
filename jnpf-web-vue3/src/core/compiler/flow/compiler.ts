/**
 * FlowIR → JNPF 工作流配置编译器 (A3)
 *
 * 将 FlowIR 编译为 JNPF 平台可识别的工作流 JSON 配置。
 * 输出格式：FlowNode[] + FlowLine[] + FlowVariable[]
 *
 * @jnpf-generated v5.2.0 type=flow-compiler
 * @module compiler/flow
 */

import type { FlowIR, FlowNode, FlowEdge, FlowVariable } from '../../ir/flow-types';

// ============================================================
// 输出类型
// ============================================================

export interface FlowCompileResult {
  config: string;
  warnings: string[];
  nodeCount: number;
  edgeCount: number;
}

// ============================================================
// JNPF 平台工作流配置格式
// ============================================================

interface JnpfFlowNode {
  id: string;
  type: string;
  name: string;
  nodeType: number;
  position: { x: number; y: number };
  properties: Record<string, unknown>;
}

interface JnpfFlowLine {
  id: string;
  from: string;
  to: string;
  label: string;
  condition?: string;
}

interface JnpfFlowConfig {
  nodes: JnpfFlowNode[];
  lines: JnpfFlowLine[];
  variables: Array<{
    key: string;
    name: string;
    type: string;
    defaultValue: unknown;
  }>;
}

// ============================================================
// FlowCompiler
// ============================================================

export class FlowCompiler {
  /** 编译 FlowIR → 工作流配置 JSON */
  compile(ir: FlowIR): FlowCompileResult {
    const warnings: string[] = [];

    // Step 1: 验证
    const validationWarnings = this.validate(ir);
    warnings.push(...validationWarnings);

    // Step 2: 转换节点
    const nodes = ir.nodes.map(n => this.compileNode(n, warnings));

    // Step 3: 转换边
    const lines = ir.edges.map(e => this.compileEdge(e));

    // Step 4: 转换变量
    const variables = ir.variables.map(v => this.compileVariable(v));

    // Step 5: 构建配置
    const config: JnpfFlowConfig = { nodes, lines, variables };

    return {
      config: JSON.stringify(config, null, 2),
      warnings,
      nodeCount: nodes.length,
      edgeCount: lines.length,
    };
  }

  // ============================================================
  // 验证
  // ============================================================

  private validate(ir: FlowIR): string[] {
    const warnings: string[] = [];

    // 必须有 start 节点
    if (!ir.nodes.some(n => n.type === 'start')) {
      warnings.push('工作流缺少 start 节点');
    }

    // 必须有 end 节点
    if (!ir.nodes.some(n => n.type === 'end')) {
      warnings.push('工作流缺少 end 节点');
    }

    // 检查孤立节点
    const nodeIds = new Set(ir.nodes.map(n => n.id));
    const connectedIds = new Set<string>();
    for (const edge of ir.edges) {
      connectedIds.add(edge.sourceNodeId);
      connectedIds.add(edge.targetNodeId);
    }
    for (const id of nodeIds) {
      if (!connectedIds.has(id)) {
        warnings.push(`节点 "${id}" 未连接`);
      }
    }

    // 检查空节点引用
    for (const edge of ir.edges) {
      if (!nodeIds.has(edge.sourceNodeId)) {
        warnings.push(`边引用不存在的源节点: ${edge.sourceNodeId}`);
      }
      if (!nodeIds.has(edge.targetNodeId)) {
        warnings.push(`边引用不存在的目标节点: ${edge.targetNodeId}`);
      }
    }

    // 变量名校验
    const varNames = new Set<string>();
    for (const v of ir.variables) {
      if (varNames.has(v.name)) {
        warnings.push(`变量名重复: ${v.name}`);
      }
      varNames.add(v.name);
    }

    return warnings;
  }

  // ============================================================
  // 编译节点
  // ============================================================

  private compileNode(node: FlowNode, _warnings: string[]): JnpfFlowNode {
    const nodeTypeMap: Record<string, number> = {
      start: 0,
      end: 1,
      approval: 2,
      condition: 3,
      parallel: 4,
      subprocess: 5,
      notification: 6,
      script: 7,
      timer: 8,
    };

    return {
      id: node.id,
      type: node.type,
      name: node.name,
      nodeType: nodeTypeMap[node.type] ?? 0,
      position: node.position,
      properties: this.extractNodeProperties(node),
    };
  }

  /** 从 FlowNode.config 提取 JNPF 节点属性 */
  private extractNodeProperties(node: FlowNode): Record<string, unknown> {
    const props: Record<string, unknown> = {};

    switch (node.config.type) {
      case 'approval': {
        const ac = node.config as unknown as { type: string; approverType: string; approverIds: string[] };
        props.approverType = ac.approverType;
        props.approverIds = ac.approverIds;
        break;
      }
      case 'condition': {
        const cc = node.config as unknown as {
          type: string;
          conditions: Array<{ field: string; operator: string; value: unknown; nextNodeId: string }>;
          defaultNodeId: string;
        };
        props.conditions = cc.conditions;
        props.defaultNodeId = cc.defaultNodeId;
        break;
      }
      case 'notification': {
        const nc = node.config as unknown as { type: string; templateId: string; channel: string };
        props.templateId = nc.templateId;
        props.channel = nc.channel;
        break;
      }
      case 'timer': {
        const tc = node.config as unknown as { type: string; delay: number; delayType: string };
        props.delay = tc.delay;
        props.delayType = tc.delayType;
        break;
      }
      default:
        break;
    }

    return props;
  }

  // ============================================================
  // 编译边
  // ============================================================

  private compileEdge(edge: FlowEdge): JnpfFlowLine {
    return {
      id: edge.id,
      from: edge.sourceNodeId,
      to: edge.targetNodeId,
      label: edge.label ?? '',
      condition: edge.condition ?? undefined,
    };
  }

  // ============================================================
  // 编译变量
  // ============================================================

  private compileVariable(variable: FlowVariable): {
    key: string;
    name: string;
    type: string;
    defaultValue: unknown;
  } {
    return {
      key: variable.name,
      name: variable.name,
      type: variable.type,
      defaultValue: variable.defaultValue ?? null,
    };
  }
}
