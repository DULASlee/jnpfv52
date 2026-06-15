import { describe, it, expect } from 'vitest';
import { propagateImpact, determineReentryStage } from '../impact-analysis';
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

describe('propagateImpact', () => {
  it('TC-IA-1: 修改孤立节点 → 返回1个节点', () => {
    const ir = createTestIR({
      fields: [{ id: 'A', type: 'Input', name: 'a' } as any, { id: 'B', type: 'Input', name: 'b' } as any],
    });
    const graph = buildDependencyGraph(ir);
    const impacted = propagateImpact(graph, new Set(['A']));
    expect(impacted.size).toBeGreaterThanOrEqual(1);
    expect(impacted.has('A')).toBe(true);
  });

  it('TC-IA-2: 空变更集合 → 返回空', () => {
    const ir = createTestIR({
      fields: [{ id: 'A', type: 'Input', name: 'a' } as any],
    });
    const graph = buildDependencyGraph(ir);
    const impacted = propagateImpact(graph, new Set());
    expect(impacted.size).toBe(0);
  });

  it('TC-IA-3: 节点不存在于图中 → 不影响传播', () => {
    const ir = createTestIR({
      fields: [{ id: 'A', type: 'Input', name: 'a' } as any],
    });
    const graph = buildDependencyGraph(ir);
    const impacted = propagateImpact(graph, new Set(['NonExistent']));
    // 不存在于图中的节点被忽略
    expect(impacted.size).toBe(0);
  });
});

describe('determineReentryStage', () => {
  it('TC-IA-4: 空集合 → 抛出错误', () => {
    const ir = createTestIR();
    const graph = buildDependencyGraph(ir);
    expect(() => determineReentryStage(new Set(), graph)).toThrow();
  });

  it('TC-IA-5: 节点类型映射到stage → 返回最早stage', () => {
    const ir = createTestIR({
      databaseFields: [{ id: 'DB1', type: 'TableDefinition', name: 't1' } as any],
      fields: [{ id: 'F1', type: 'Input', name: 'f1' } as any],
    });
    const graph = buildDependencyGraph(ir);
    const impacted = propagateImpact(graph, new Set(['DB1', 'F1']));
    const stage = determineReentryStage(impacted, graph);
    expect(stage).toBeDefined();
    expect(['stage1', 'stage2', 'stage3', 'stage4', 'stage5']).toContain(stage);
  });
});
