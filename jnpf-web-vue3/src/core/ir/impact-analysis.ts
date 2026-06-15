/**
 * 变更影响传播与分析（P0 审判令一）
 *
 * propagateImpact: 从变更节点出发沿反向边 BFS，收集所有受影响节点。
 * determineReentryStage: 将受影响节点类型映射到对应阶段，取最早阶段。
 *
 * @module ir/impact-analysis
 * @version 1.0.0
 */

import type { IRDependencyGraph } from './dependency-graph';

// ============================================================
// 类型定义
// ============================================================

export type PipelineStage = 'stage1' | 'stage2' | 'stage3' | 'stage4' | 'stage5';

// ============================================================
// 节点类型 → 阶段映射（编译时常量）
// ============================================================

export const NODE_TYPE_TO_STAGE: Record<string, PipelineStage> = {
  Requirement: 'stage1',
  ModuleDefinition: 'stage2',
  TableDefinition: 'stage2',
  Component: 'stage3',
  DataSource: 'stage3',
  WorkflowStep: 'stage3',
  RuleNode: 'stage3',
  CompileConfig: 'stage4',
  // 基础节点类型默认映射
  Field: 'stage3',
  Expression: 'stage3',
  Action: 'stage3',
  Validation: 'stage3',
};

// ============================================================
// 引擎函数
// ============================================================

/**
 * 影响传播：从变更节点出发，沿反向边 BFS，收集所有受影响节点。
 *
 * 使用标准 BFS（非递归），避免深度 IR 导致栈溢出。
 * 如果 changedNodeIds 为空集合，返回空集合。
 */
export function propagateImpact(graph: IRDependencyGraph, changedNodeIds: Set<string>): Set<string> {
  if (changedNodeIds.size === 0) {
    return new Set();
  }

  const visited = new Set<string>();
  const queue: string[] = [];

  // 初始化队列：所有变更节点
  for (const id of changedNodeIds) {
    if (graph.nodes.has(id)) {
      visited.add(id);
      queue.push(id);
    }
  }

  // BFS
  while (queue.length > 0) {
    const current = queue.shift()!;
    const dependents = graph.reverseEdges.get(current);
    if (!dependents) continue;

    for (const depId of dependents) {
      if (!visited.has(depId)) {
        visited.add(depId);
        queue.push(depId);
      }
    }
  }

  return visited;
}

/**
 * 确定重入阶段：取所有受影响节点映射到的阶段中最早的那一个。
 *
 * 如果 impactedNodeIds 为空集合，抛出错误。
 */
export function determineReentryStage(impactedNodeIds: Set<string>, graph: IRDependencyGraph): PipelineStage {
  if (impactedNodeIds.size === 0) {
    throw new Error('determineReentryStage: impactedNodeIds 为空集合，无法确定重入阶段');
  }

  let earliestStage: PipelineStage = 'stage5';

  for (const nodeId of impactedNodeIds) {
    const node = graph.nodes.get(nodeId);
    if (!node) continue;

    const stage = NODE_TYPE_TO_STAGE[node.type];
    if (!stage) continue;

    // 取最早的阶段（stage1 < stage2 < ... < stage5）
    if (stageOrder(stage) < stageOrder(earliestStage)) {
      earliestStage = stage;
    }
  }

  return earliestStage;
}

// ============================================================
// 内部工具
// ============================================================

function stageOrder(stage: PipelineStage): number {
  const orders: Record<PipelineStage, number> = {
    stage1: 1,
    stage2: 2,
    stage3: 3,
    stage4: 4,
    stage5: 5,
  };
  return orders[stage] ?? 5;
}
