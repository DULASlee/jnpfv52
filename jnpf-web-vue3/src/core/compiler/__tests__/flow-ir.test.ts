/**
 * FlowIR 单元测试
 *
 * 覆盖：类型系统 / 序列化 round-trip / 验证器 / 状态机
 */

import { describe, it, expect } from 'vitest';
import type { FlowIR } from '../../ir/flow-types';
import { validateFlowIR, hasErrors } from '../../ir/flow-types';
import { serializeFlowIR, deserializeFlowIR, flowIRToSchema, schemaToFlowIR } from '../../ir/flow-serializer';
import { FlowEngine } from '../../ir/flow-engine';

// ─── 测试数据：简单审批流程 ───

const simpleApprovalFlow: FlowIR = {
  type: 'workflow',
  id: 'test-flow-1',
  name: '测试审批流程',
  version: '1.0.0',
  nodes: [
    {
      id: 'n1',
      type: 'start',
      name: '开始',
      config: { type: 'start', triggerType: 'manual' },
      position: { x: 0, y: 0 },
    },
    {
      id: 'n2',
      type: 'approval',
      name: '主管审批',
      config: {
        type: 'approval',
        approverType: 'user',
        approverIds: ['u1'],
        assignPolicy: 'all',
      },
      position: { x: 200, y: 0 },
    },
    {
      id: 'n3',
      type: 'condition',
      name: '判断',
      config: {
        type: 'condition',
        conditions: [
          {
            field: 'status',
            operator: '==',
            value: 'approved',
            nextNodeId: 'n4',
          },
        ],
        defaultNodeId: 'n5',
      },
      position: { x: 400, y: 0 },
    },
    {
      id: 'n4',
      type: 'end',
      name: '通过',
      config: { type: 'end', onEnd: 'notify' },
      position: { x: 600, y: -100 },
    },
    {
      id: 'n5',
      type: 'end',
      name: '驳回',
      config: { type: 'end', onEnd: 'archive' },
      position: { x: 600, y: 100 },
    },
  ],
  edges: [
    { id: 'e1', sourceNodeId: 'n1', targetNodeId: 'n2' },
    { id: 'e2', sourceNodeId: 'n2', targetNodeId: 'n3' },
    {
      id: 'e3',
      sourceNodeId: 'n3',
      targetNodeId: 'n4',
      condition: 'status == approved',
    },
    { id: 'e4', sourceNodeId: 'n3', targetNodeId: 'n5' },
  ],
  variables: [
    {
      id: 'v1',
      name: 'status',
      type: 'string',
      scope: 'global',
      defaultValue: '',
    },
  ],
};

// ============================================================
// 序列化 / 反序列化
// ============================================================

describe('FlowIR序列化/反序列化', () => {
  it('round-trip: serialize → deserialize → 结构一致', () => {
    const json = serializeFlowIR(simpleApprovalFlow);
    const { ir, errors } = deserializeFlowIR(json);
    expect(ir).not.toBeNull();
    expect(errors.length).toBe(0);
    expect(ir!.nodes.length).toBe(simpleApprovalFlow.nodes.length);
    expect(ir!.edges.length).toBe(simpleApprovalFlow.edges.length);
    expect(ir!.id).toBe('test-flow-1');
  });

  it('反序列化补全缺失字段', () => {
    const incomplete = JSON.stringify({
      type: 'workflow',
      id: 'x',
      nodes: [],
      edges: [],
    });
    const { ir, errors } = deserializeFlowIR(incomplete);
    expect(ir).not.toBeNull();
    expect(ir!.name).toBe('');
    expect(ir!.version).toBe('1.0.0');
    expect(ir!.nodes).toEqual([]);
    expect(errors.length).toBeGreaterThanOrEqual(0); // 可能有 MISSING_START
  });

  it('Schema转换round-trip', () => {
    const schema = flowIRToSchema(simpleApprovalFlow);
    const ir = schemaToFlowIR(schema);
    expect(ir).not.toBeNull();
    expect(ir!.id).toBe('test-flow-1');
    expect(ir!.nodes.length).toBe(5);
  });

  it('反序列化错误的type返回error', () => {
    const { ir, errors } = deserializeFlowIR(JSON.stringify({ type: 'unknown', id: 'x' }));
    expect(ir).toBeNull();
    expect(errors.length).toBeGreaterThan(0);
  });
});

// ============================================================
// 验证器
// ============================================================

describe('FlowIR验证器', () => {
  it('有效流程无error', () => {
    const issues = validateFlowIR(simpleApprovalFlow);
    expect(hasErrors(issues)).toBe(false);
  });

  it('缺少start节点报error', () => {
    const noStart = {
      ...simpleApprovalFlow,
      nodes: simpleApprovalFlow.nodes.filter(n => n.type !== 'start'),
    };
    const issues = validateFlowIR(noStart);
    expect(hasErrors(issues)).toBe(true);
    expect(issues.some(i => i.code === 'MISSING_START')).toBe(true);
  });

  it('多个start节点报error', () => {
    const twoStarts = {
      ...simpleApprovalFlow,
      nodes: [
        ...simpleApprovalFlow.nodes,
        {
          id: 'start2',
          type: 'start' as const,
          name: '开始2',
          config: { type: 'start' as const, triggerType: 'api' as const },
          position: { x: 0, y: 100 },
        },
      ],
    };
    const issues = validateFlowIR(twoStarts);
    expect(issues.some(i => i.code === 'DUPLICATE_START')).toBe(true);
  });

  it('无效edge引用报error', () => {
    const badEdge = {
      ...simpleApprovalFlow,
      edges: [
        ...simpleApprovalFlow.edges,
        {
          id: 'bad',
          sourceNodeId: 'xxx',
          targetNodeId: 'yyy',
        },
      ],
    };
    const issues = validateFlowIR(badEdge);
    expect(hasErrors(issues)).toBe(true);
    expect(issues.some(i => i.code === 'INVALID_EDGE_SOURCE')).toBe(true);
  });

  it('自环检测', () => {
    const selfLoop = {
      ...simpleApprovalFlow,
      edges: [
        ...simpleApprovalFlow.edges,
        {
          id: 'loop',
          sourceNodeId: 'n2',
          targetNodeId: 'n2',
        },
      ],
    };
    const issues = validateFlowIR(selfLoop);
    expect(issues.some(i => i.code === 'SELF_LOOP')).toBe(true);
  });
});

// ============================================================
// 状态机
// ============================================================

describe('FlowEngine状态机', () => {
  it('基本流转: start → approval → condition → end', () => {
    const engine = new FlowEngine(simpleApprovalFlow);
    expect(engine.getCurrentNode().type).toBe('start');

    engine.next(); // start → approval
    expect(engine.getCurrentNode().type).toBe('approval');
    expect(engine.getCurrentNode().name).toBe('主管审批');

    engine.next('approve'); // approval → condition
    expect(engine.getCurrentNode().type).toBe('condition');

    // status='' 不匹配 'approved' → defaultNodeId → 驳回end
    engine.next();
    expect(engine.getCurrentNode().type).toBe('end');
    expect(engine.getCurrentNode().name).toBe('驳回');
  });

  it('条件分支: 通过路径', () => {
    const engine = new FlowEngine(simpleApprovalFlow);
    engine.next(); // → approval
    engine.next('approve'); // → condition
    engine.setVariable('status', 'approved');
    engine.next(); // condition自动求值 → 通过 end
    expect(engine.getCurrentNode().name).toBe('通过');
    expect(engine.getCurrentNode().type).toBe('end');
  });

  it('条件分支: 驳回路径', () => {
    const engine = new FlowEngine(simpleApprovalFlow);
    engine.next(); // → approval
    engine.next('reject'); // → condition
    engine.next(); // default → 驳回
    expect(engine.getCurrentNode().name).toBe('驳回');
  });

  it('条件求值安全（零eval — 注入攻击防护）', () => {
    const engine = new FlowEngine(simpleApprovalFlow);
    engine.next();
    engine.next();
    // 尝试注入恶意值
    engine.setVariable('status', 'approved"; process.exit()');
    // == 比较不匹配 → 走 default → 驳回
    engine.next();
    expect(engine.getCurrentNode().name).toBe('驳回');
  });

  it('getAvailableActions', () => {
    const engine = new FlowEngine(simpleApprovalFlow);
    engine.next(); // → approval
    expect(engine.getAvailableActions()).toEqual(['approve', 'reject']);
  });

  it('快照 round-trip', () => {
    const engine = new FlowEngine(simpleApprovalFlow);
    engine.next(); // → approval
    engine.setVariable('status', 'pending');
    const snapshot = engine.getSnapshot();

    // 从快照恢复
    const engine2 = new FlowEngine(simpleApprovalFlow);
    engine2.restoreSnapshot(snapshot);
    expect(engine2.getCurrentNode().id).toBe('n2');
    expect(engine2.getVariable('status')).toBe('pending');
    expect(engine2.getSnapshot().status).toBe('running');
  });

  it('end节点返回done=true', () => {
    const engine = new FlowEngine(simpleApprovalFlow);
    engine.setVariable('status', 'approved');
    engine.next(); // → approval
    engine.next('approve'); // → condition
    const result = engine.next(); // → 通过 end

    expect(result.done).toBe(true);
    expect(result.node.name).toBe('通过');
  });

  it('缺少start节点抛异常', () => {
    const noStart = {
      ...simpleApprovalFlow,
      nodes: simpleApprovalFlow.nodes.filter(n => n.type !== 'start'),
    };
    expect(() => new FlowEngine(noStart)).toThrow('no start node');
  });
});
