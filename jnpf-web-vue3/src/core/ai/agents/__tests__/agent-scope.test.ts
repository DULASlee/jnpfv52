import { describe, it, expect } from 'vitest';
import { extractSlice, AGENT_SCOPES } from '../agent-scope';
import type { FormPageIR } from '../../../ir/types';

function createTestIRWithMultipleTypes(): { ir: FormPageIR } {
  const ir: FormPageIR = {
    type: 'form',
    id: 'test-form-multi',
    name: 'MultiTypeForm',
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
    fields: [
      { id: 'comp1', type: 'Component', name: 'btn1', label: '按钮1' },
      { id: 'comp2', type: 'Component', name: 'input1', label: '输入框1' },
    ] as any,
    databaseFields: [
      { id: 'db1', type: 'TableDefinition', name: 'users' },
      { id: 'db2', type: 'DataSource', name: 'userDS' },
    ] as any,
    expressions: [{ id: 'rule1', type: 'RuleNode', name: 'rule1' }] as any,
  };
  return { ir };
}

describe('AGENT_SCOPES', () => {
  it('TC-AS-1: AGENT_SCOPES定义存在且非空', () => {
    expect(AGENT_SCOPES).toBeDefined();
    expect(AGENT_SCOPES.length).toBeGreaterThan(0);
  });

  it('TC-AS-2: 每个scope都有agentName', () => {
    for (const scope of AGENT_SCOPES) {
      expect(scope.agentName).toBeDefined();
      expect(typeof scope.agentName).toBe('string');
      expect(scope.agentName.length).toBeGreaterThan(0);
    }
  });

  it('TC-AS-3: 每个scope都有 readable/writable 定义', () => {
    for (const scope of AGENT_SCOPES) {
      expect(scope.readableNodeTypes).toBeDefined();
      expect(scope.writableNodeTypes).toBeDefined();
      expect(Array.isArray(scope.readableNodeTypes)).toBe(true);
      expect(Array.isArray(scope.writableNodeTypes)).toBe(true);
    }
  });
});

describe('extractSlice', () => {
  it('TC-AS-4: 有效scope → 返回AgentSlice', () => {
    const { ir } = createTestIRWithMultipleTypes();
    const scope = AGENT_SCOPES[0];
    const slice = extractSlice(ir, scope);
    expect(slice).toBeDefined();
    expect(Array.isArray(slice.nodes)).toBe(true);
    expect(Array.isArray(slice.writableNodeIds)).toBe(true);
  });

  it('TC-AS-5: writeableNodeIds只包含writableNodeTypes节点', () => {
    const { ir } = createTestIRWithMultipleTypes();
    for (const scope of AGENT_SCOPES) {
      const slice = extractSlice(ir, scope);
      for (const id of slice.writableNodeIds) {
        const node = slice.nodes.find(n => (n as any).id === id);
        if (node) {
          expect(scope.writableNodeTypes).toContain((node as any).type);
        }
      }
    }
  });

  it('TC-AS-6: 空IR → 返回空nodes', () => {
    const emptyIr: FormPageIR = {
      type: 'form',
      id: 'empty',
      name: 'Empty',
      config: {} as any,
      fields: [],
      databaseFields: [],
      expressions: [],
    };
    const scope = AGENT_SCOPES[0];
    const slice = extractSlice(emptyIr, scope);
    expect(slice.nodes.length).toBe(0);
    expect(slice.writableNodeIds.length).toBe(0);
  });
});
