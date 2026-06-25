/**
 * applyPatch / rollbackPatch 单元测试（P1 · 7 用例）
 */
import { describe, expect, it } from 'vitest';
import { applyPatch, rollbackPatch } from '../edit-patch';
import type { IREditPatch } from '../edit-patch';
import type { AgentSlice } from '../../ai/agents/agent-scope';
import type { FormPageIR } from '../types';

function makeTestIR(): FormPageIR {
  return {
    type: 'form',
    id: 'test_form',
    name: '测试表单',
    config: {
      labelPosition: 'left',
      labelWidth: 100,
      labelSuffix: ':',
      size: 'default',
      disabled: false,
      span: 24,
      gutter: 16,
      colon: true,
      popupType: 'general',
      generalWidth: '800px',
      fullScreenWidth: '100%',
      drawerWidth: '600px',
      hasCancelBtn: true,
      cancelButtonText: '取消',
      hasConfirmBtn: true,
      confirmButtonText: '提交',
      hasConfirmAndAddBtn: false,
      hasPrintBtn: false,
      printButtonText: '',
      primaryKeyPolicy: 'auto',
      tablePolicy: 'simple',
      concurrencyLock: false,
      logicalDelete: false,
    },
    fields: [
      {
        id: 'field_name',
        model: 'name',
        label: '姓名',
        component: { jnpfKey: 'Input', pc: 'a-input', app: 'wd-input', legacyApp: 'uni-input' },
        config: {
          required: true,
          defaultValue: '',
          placeholder: '请输入姓名',
          disabled: false,
          readonly: false,
          hidden: false,
          span: 12,
          labelWidth: null,
          maxlength: 50,
          showWordLimit: true,
          clearable: true,
          min: null,
          max: null,
          precision: null,
          step: null,
          multiple: false,
          options: [],
          dictType: null,
          relationData: null,
          style: {},
        },
        validation: [{ type: 'required', message: '姓名为必填', trigger: 'blur' }],
        events: {},
      },
      {
        id: 'field_age',
        model: 'age',
        label: '年龄',
        component: { jnpfKey: 'InputNumber', pc: 'a-input-number', app: 'wd-input-number', legacyApp: 'uni-input-number' },
        config: {
          required: false,
          defaultValue: 0,
          placeholder: '请输入年龄',
          disabled: false,
          readonly: false,
          hidden: false,
          span: 12,
          labelWidth: null,
          maxlength: null,
          showWordLimit: false,
          clearable: true,
          min: 0,
          max: 150,
          precision: 0,
          step: 1,
          multiple: false,
          options: [],
          dictType: null,
          relationData: null,
          style: {},
        },
        validation: [],
        events: {},
      },
    ],
    databaseFields: [
      { id: 'db_name', name: 'name', type: 'NVARCHAR(50)', length: 50, nullable: false, defaultValue: '', description: '' },
      { id: 'db_age', name: 'age', type: 'INT', length: null, nullable: true, defaultValue: '0', description: '' },
    ],
    expressions: [],
    listConfig: {
      searchFields: [],
      columns: [
        { field: 'name', label: '姓名', width: 200, fixed: null, sortable: false },
        { field: 'age', label: '年龄', width: 100, fixed: null, sortable: true },
      ],
      ruleList: [],
    },
  };
}

function makeSlice(writableIds: string[]): AgentSlice {
  return { nodes: [], context: 'test', writableNodeIds: writableIds };
}

describe('applyPatch', () => {
  it('TC-1: 3 replace ops all in scope -> 3 applied, 0 failed', () => {
    const ir = makeTestIR();
    const patch: IREditPatch = {
      targetNodeIds: ['field_name'],
      operations: [
        { op: 'replace', path: '$.fields[field_name].label', value: '员工姓名', reason: '改名' },
        { op: 'replace', path: '$.fields[field_name].config.placeholder', value: '请输入员工姓名', reason: '更准确' },
        { op: 'replace', path: '$.fields[field_age].config.required', value: true, reason: '年龄改必填' },
      ],
      explanation: '更新字段',
    };
    const slice = makeSlice(['field_name', 'field_age']);
    const result = applyPatch(ir, patch, slice);
    expect(result.applied).toHaveLength(3);
    expect(result.failed).toHaveLength(0);
  });

  it('TC-2: 1 op out of scope -> 1 applied, 1 failed', () => {
    const ir = makeTestIR();
    const patch: IREditPatch = {
      targetNodeIds: ['field_name', 'field_age'],
      operations: [
        { op: 'replace', path: '$.fields[field_name].label', value: '新姓名', reason: '改名' },
        { op: 'replace', path: '$.fields[field_age].label', value: '新年龄', reason: '改名' },
      ],
      explanation: '更新',
    };
    const slice = makeSlice(['field_name']);
    const result = applyPatch(ir, patch, slice);
    expect(result.applied).toHaveLength(1);
    expect(result.failed).toHaveLength(1);
    expect(result.failed[0].failureReason).toContain('field_age');
  });

  it('TC-3: path to non-existent -> 0 applied, 1 failed', () => {
    const ir = makeTestIR();
    const patch: IREditPatch = {
      targetNodeIds: ['nonexistent'],
      operations: [{ op: 'replace', path: '$.fields[nonexistent].label', value: 'no', reason: 'test' }],
      explanation: 'fail',
    };
    const slice = makeSlice(['nonexistent']);
    const result = applyPatch(ir, patch, slice);
    expect(result.applied).toHaveLength(0);
    expect(result.failed).toHaveLength(1);
  });

  it('TC-4: add -> 1 applied, new data in result', () => {
    const ir = makeTestIR();
    const patch: IREditPatch = {
      targetNodeIds: ['field_name'],
      operations: [{ op: 'add', path: '$.fields[field_name].config.newProp', value: 'added', reason: 'test' }],
      explanation: 'add',
    };
    const slice = makeSlice(['field_name']);
    const result = applyPatch(ir, patch, slice);
    expect(result.applied).toHaveLength(1);
    expect(result.failed).toHaveLength(0);
  });

  it('TC-5: remove -> 1 applied', () => {
    const ir = makeTestIR();
    const patch: IREditPatch = {
      targetNodeIds: ['field_age'],
      operations: [{ op: 'remove', path: '$.fields[field_age].validation', reason: '删除校验' }],
      explanation: 'remove',
    };
    const slice = makeSlice(['field_age']);
    const result = applyPatch(ir, patch, slice);
    expect(result.applied).toHaveLength(1);
  });

  it('TC-6: rollbackPatch restores original via deepEqual', () => {
    const ir = makeTestIR();
    const irCopy = JSON.parse(JSON.stringify(ir));
    const patch: IREditPatch = {
      targetNodeIds: ['field_name'],
      operations: [
        { op: 'replace', path: '$.fields[field_name].label', value: '临时名称', reason: 'test' },
        { op: 'replace', path: '$.fields[field_name].config.required', value: false, reason: 'test' },
      ],
      explanation: 'temp',
    };
    const slice = makeSlice(['field_name']);
    const { result, applied } = applyPatch(ir, patch, slice);
    expect(applied).toHaveLength(2);
    const restored = rollbackPatch(result, applied);
    expect(restored).toEqual(irCopy);
  });

  it('TC-7: empty ops -> 0,0, result equals input', () => {
    const ir = makeTestIR();
    const patch: IREditPatch = { targetNodeIds: [], operations: [], explanation: 'noop' };
    const slice = makeSlice(['field_name']);
    const result = applyPatch(ir, patch, slice);
    expect(result.applied).toHaveLength(0);
    expect(result.failed).toHaveLength(0);
    expect(result.result).toEqual(ir);
  });
});
