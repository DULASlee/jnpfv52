/**
 * FlowIR 状态机
 *
 * 基于 FlowIR 的简单、安全的工作流执行引擎。
 * 条件求值不使用 eval/Function，仅支持安全的比较运算符。
 *
 * @jnpf-generated v5.2.0 type=ir-engine platform=universal
 */

import type { FlowIR, FlowNode, FlowSnapshot, ConditionNodeConfig } from './flow-types';

export class FlowEngine {
  private ir: FlowIR;
  private currentNodeId: string;
  private variables: Record<string, unknown>;
  private history: FlowSnapshot['history'];
  private status: FlowSnapshot['status'];

  constructor(ir: FlowIR) {
    this.ir = ir;
    const startNode = ir.nodes.find(n => n.type === 'start');
    if (!startNode) throw new Error('FlowIR has no start node');
    this.currentNodeId = startNode.id;
    this.variables = {};
    this.history = [];
    this.status = 'running';

    // 初始化全局变量默认值
    for (const v of ir.variables) {
      if (v.scope === 'global') {
        this.variables[v.name] = v.defaultValue ?? null;
      }
    }
  }

  // ==========================================================
  // 节点导航
  // ==========================================================

  /** 获取当前节点 */
  getCurrentNode(): FlowNode {
    const node = this.ir.nodes.find(n => n.id === this.currentNodeId);
    if (!node) throw new Error(`Node ${this.currentNodeId} not found`);
    return node;
  }

  /**
   * 推进到下一步
   * @param action — 动作标识
   *   - approval 节点：'approve' | 'reject'
   *   - condition 节点：自动求值
   *   - 其他：'next'
   */
  next(action?: string): { node: FlowNode; done: boolean } {
    const current = this.getCurrentNode();

    // 记录历史
    this.history.push({
      nodeId: current.id,
      action: action ?? 'auto',
      timestamp: new Date().toISOString(),
    });

    // end 节点 → 完成
    if (current.type === 'end') {
      this.status = 'completed';
      return { node: current, done: true };
    }

    // condition 节点 → 根据变量求值选择分支
    if (current.type === 'condition') {
      const config = current.config as ConditionNodeConfig;
      const nextId = this.evaluateConditions(config);
      this.currentNodeId = nextId;
      const nextNode = this.getCurrentNode();
      return { node: nextNode, done: nextNode.type === 'end' };
    }

    // 默认：沿第一条出边前进
    const outEdge = this.ir.edges.find(e => e.sourceNodeId === current.id);
    if (!outEdge) {
      // 无出边 → 完成
      this.status = 'completed';
      return { node: current, done: true };
    }

    this.currentNodeId = outEdge.targetNodeId;
    const nextNode = this.getCurrentNode();

    if (nextNode.type === 'end') {
      this.status = 'completed';
    }

    return { node: nextNode, done: nextNode.type === 'end' };
  }

  // ==========================================================
  // 动作发现
  // ==========================================================

  /** 获取当前节点可执行的动作列表 */
  getAvailableActions(): string[] {
    const current = this.getCurrentNode();
    switch (current.type) {
      case 'approval':
        return ['approve', 'reject'];
      case 'condition':
        return [];
      case 'parallel':
        return ['continue'];
      default:
        return ['next'];
    }
  }

  // ==========================================================
  // 条件求值（安全，零 eval/Function）
  // ==========================================================

  /**
   * 求值条件节点，返回应跳转的节点 ID。
   * 按顺序求值每个 condition，首个匹配的分支胜出。
   * 全部不匹配 → defaultNodeId。
   */
  private evaluateConditions(config: ConditionNodeConfig): string {
    for (const cond of config.conditions) {
      const value = this.variables[cond.field];
      if (this.evaluateSingleCondition(value, cond.operator, cond.value)) {
        return cond.nextNodeId;
      }
    }
    return config.defaultNodeId;
  }

  /**
   * 单条件求值 — 8 种安全比较运算符
   */
  private evaluateSingleCondition(value: unknown, operator: string, target: unknown): boolean {
    switch (operator) {
      case '==':
        // eslint-disable-next-line eqeqeq
        return value == target;
      case '!=':
        // eslint-disable-next-line eqeqeq
        return value != target;
      case '>':
        return Number(value) > Number(target);
      case '>=':
        return Number(value) >= Number(target);
      case '<':
        return Number(value) < Number(target);
      case '<=':
        return Number(value) <= Number(target);
      case 'contains':
        return String(value).includes(String(target));
      case 'in':
        return Array.isArray(target) && target.includes(value);
      default:
        return false;
    }
  }

  // ==========================================================
  // 变量操作
  // ==========================================================

  /** 设置变量 */
  setVariable(name: string, value: unknown): void {
    this.variables[name] = value;
  }

  /** 获取变量 */
  getVariable(name: string): unknown {
    return this.variables[name];
  }

  // ==========================================================
  // 快照
  // ==========================================================

  /** 获取当前快照 */
  getSnapshot(): FlowSnapshot {
    return {
      flowId: this.ir.id,
      currentNodeId: this.currentNodeId,
      variables: { ...this.variables },
      history: [...this.history],
      status: this.status,
    };
  }

  /** 从快照恢复 */
  restoreSnapshot(snapshot: FlowSnapshot): void {
    this.currentNodeId = snapshot.currentNodeId;
    this.variables = { ...snapshot.variables };
    this.history = [...snapshot.history];
    this.status = snapshot.status;
  }
}
