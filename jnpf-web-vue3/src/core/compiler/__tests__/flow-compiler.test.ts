/**
 * FlowIR 编译器测试 (A3)
 */
import { describe, it, expect } from 'vitest';
import { FlowCompiler } from '../flow/compiler';
import type { FlowIR } from '../../ir/flow-types';

const simpleApprovalFlow: FlowIR = {
  type: 'workflow',
  id: 'test-flow',
  name: '简单审批流',
  version: '1.0',
  nodes: [
    { id: 'n1', type: 'start', name: '开始', position: { x: 0, y: 0 }, config: { type: 'start', triggerType: 'manual' } },
    {
      id: 'n2',
      type: 'approval',
      name: '经理审批',
      position: { x: 100, y: 0 },
      config: { type: 'approval', approverType: 'role', approverIds: ['manager'], assignPolicy: 'all' },
    },
    { id: 'n3', type: 'end', name: '结束', position: { x: 200, y: 0 }, config: { type: 'end' } },
  ],
  edges: [
    { id: 'e1', sourceNodeId: 'n1', targetNodeId: 'n2' },
    { id: 'e2', sourceNodeId: 'n2', targetNodeId: 'n3' },
  ],
  variables: [{ id: 'amount', name: '金额', type: 'number', defaultValue: 0, scope: 'global' }],
};

const invalidFlow: FlowIR = {
  type: 'workflow',
  id: 'bad',
  name: '无效流',
  version: '1',
  nodes: [
    {
      id: 'n1',
      type: 'approval',
      name: '审批',
      position: { x: 0, y: 0 },
      config: { type: 'approval', approverType: 'user', approverIds: ['u1'], assignPolicy: 'all' },
    },
  ],
  edges: [],
  variables: [],
};

describe('FlowCompiler', () => {
  const compiler = new FlowCompiler();

  it('简单审批流编译成功', () => {
    const result = compiler.compile(simpleApprovalFlow);
    expect(result.nodeCount).toBe(3);
    expect(result.edgeCount).toBe(2);

    const config = JSON.parse(result.config);
    expect(config.nodes.length).toBe(3);
    expect(config.lines.length).toBe(2);
    expect(config.variables.length).toBe(1);
  });

  it('编译输出的节点类型映射正确', () => {
    const result = compiler.compile(simpleApprovalFlow);
    const config = JSON.parse(result.config);

    const startNode = config.nodes.find((n: { type: string }) => n.type === 'start');
    expect(startNode).toBeDefined();
    expect(startNode.nodeType).toBe(0);

    const approvalNode = config.nodes.find((n: { type: string }) => n.type === 'approval');
    expect(approvalNode).toBeDefined();
    expect(approvalNode.nodeType).toBe(2);
    expect(approvalNode.properties.approverType).toBe('role');
  });

  it('无效FlowIR产生warnings', () => {
    const result = compiler.compile(invalidFlow);
    expect(result.warnings.length).toBeGreaterThan(0);
    const hasNoStart = result.warnings.some(w => w.includes('start'));
    const hasNoEnd = result.warnings.some(w => w.includes('end'));
    expect(hasNoStart).toBe(true);
    expect(hasNoEnd).toBe(true);
  });

  it('round-trip：编译→解析→结构一致', () => {
    const result = compiler.compile(simpleApprovalFlow);
    const config = JSON.parse(result.config);
    // 重新构建可验证的结构
    expect(config.nodes.length).toBe(simpleApprovalFlow.nodes.length);
    expect(config.lines.length).toBe(simpleApprovalFlow.edges.length);
    expect(config.variables.length).toBe(simpleApprovalFlow.variables.length);
  });

  it('孤立节点产生warning', () => {
    const flowWithOrphan: FlowIR = {
      ...simpleApprovalFlow,
      nodes: [
        ...simpleApprovalFlow.nodes,
        {
          id: 'orphan',
          type: 'notification',
          name: '孤岛',
          position: { x: 50, y: 50 },
          config: { type: 'notification', templateId: 'test', channel: 'sms', recipients: [] },
        },
      ],
    };
    const result = compiler.compile(flowWithOrphan);
    expect(result.warnings.some(w => w.includes('未连接'))).toBe(true);
  });

  it('notification节点属性正确提取', () => {
    const flow: FlowIR = {
      type: 'workflow',
      id: 'notif-flow',
      name: '通知流',
      version: '1',
      nodes: [
        { id: 's', type: 'start', name: 'S', position: { x: 0, y: 0 }, config: { type: 'start', triggerType: 'manual' } },
        {
          id: 'n',
          type: 'notification',
          name: '通知',
          position: { x: 50, y: 0 },
          config: { type: 'notification', templateId: 'approval_template', channel: 'email', recipients: [] },
        },
        { id: 'e', type: 'end', name: 'E', position: { x: 100, y: 0 }, config: { type: 'end' } },
      ],
      edges: [
        { id: 'l1', sourceNodeId: 's', targetNodeId: 'n' },
        { id: 'l2', sourceNodeId: 'n', targetNodeId: 'e' },
      ],
      variables: [],
    };
    const result = compiler.compile(flow);
    const config = JSON.parse(result.config);
    const notifNode = config.nodes.find((n: { type: string }) => n.type === 'notification');
    expect(notifNode.properties.template).toBe('approval_template');
    expect(notifNode.properties.channel).toBe('email');
  });
});
