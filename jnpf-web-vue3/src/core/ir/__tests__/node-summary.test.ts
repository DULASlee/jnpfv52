import { describe, it, expect } from 'vitest';
import { buildNodeSummaryList } from '../node-summary';
import type { FormPageIR } from '../types';

function createTestIR(nodeCount: number): FormPageIR {
  const fields = Array.from({ length: nodeCount }, (_, i) => ({
    id: `field_${i}`,
    component: { jnpfKey: i % 3 === 0 ? 'Input' : i % 3 === 1 ? 'Select' : 'DatePicker' },
    label: `字段${i}`,
  }));
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
    fields: fields as any,
    databaseFields: [],
    expressions: [],
  };
}

describe('buildNodeSummaryList', () => {
  it('TC-NS-1: 3节点IR → 每行包含节点id', () => {
    const ir = createTestIR(3);
    const summary = buildNodeSummaryList(ir);
    // format: [id] Type: label — description
    expect(summary).toContain('[field_0]');
    expect(summary).toContain('[field_1]');
    expect(summary).toContain('[field_2]');
    expect(summary).toContain('—');
  });

  it('TC-NS-2: 3节点IR → 输出3行', () => {
    const ir = createTestIR(3);
    const summary = buildNodeSummaryList(ir);
    const lines = summary.trim().split('\n');
    expect(lines.length).toBe(3);
  });

  it('TC-NS-3: 空IR → 返回空字符串', () => {
    const ir = createTestIR(0);
    expect(buildNodeSummaryList(ir)).toBe('');
  });

  it('TC-NS-4: 10节点 → 输出10行', () => {
    const ir = createTestIR(10);
    const lines = buildNodeSummaryList(ir).trim().split('\n');
    expect(lines.length).toBe(10);
  });
});
