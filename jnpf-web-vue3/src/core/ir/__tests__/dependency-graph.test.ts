import { describe, it, expect } from 'vitest';
import { buildDependencyGraph } from '../dependency-graph';
import type { FormPageIR } from '../types';

function createTestIR(overrides: Partial<FormPageIR> = {}): FormPageIR {
  return {
    type: 'form',
    id: 'test-form-1',
    name: 'TestForm',
    config: {
      labelPosition: 'left',
      labelWidth: 100,
      labelSuffix: ':',
      size: 'default',
      disabled: false,
      span: 24,
      gutter: 0,
      colon: true,
      popupType: 'dialog',
    } as any,
    fields: [],
    databaseFields: [],
    expressions: [],
    ...overrides,
  };
}

describe('buildDependencyGraph', () => {
  it('TC-DG-1: 空IR → 空图', () => {
    const ir = createTestIR();
    const graph = buildDependencyGraph(ir);
    expect(graph.nodes.size).toBe(0);
    expect(graph.forwardEdges.size).toBe(0);
    expect(graph.reverseEdges.size).toBe(0);
  });

  it('TC-DG-2: 3节点 → 图中有对应节点', () => {
    const ir = createTestIR({
      fields: [
        { id: 'A', component: { jnpfKey: 'Input' }, label: 'A' } as any,
        { id: 'B', component: { jnpfKey: 'Select' }, label: 'B' } as any,
        { id: 'C', component: { jnpfKey: 'DatePicker' }, label: 'C' } as any,
      ],
    });
    const graph = buildDependencyGraph(ir);
    expect(graph.nodes.size).toBeGreaterThanOrEqual(1);
    // 验证每个节点都有对应的正向边Map条目
    for (const [id] of graph.nodes) {
      expect(graph.forwardEdges.has(id)).toBe(true);
      expect(graph.reverseEdges.has(id)).toBe(true);
    }
  });

  it('TC-DG-3: 节点无ID → 抛出明确错误', () => {
    const ir = createTestIR({
      fields: [{ id: '', component: { jnpfKey: 'Input' }, label: 'NoID' } as any],
    });
    expect(() => buildDependencyGraph(ir)).toThrow();
  });

  it('TC-DG-4: databaseFields可正常收集(使用name作为id)', () => {
    const ir = createTestIR({
      databaseFields: [{ id: 'uuid-1', name: 'users', type: 'varchar', nullable: false } as any],
    });
    const graph = buildDependencyGraph(ir);
    // databaseFields使用name作为节点id
    expect(graph.nodes.has('users')).toBe(true);
  });
});
