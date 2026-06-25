/**
 * IR ↔ Schema 双向转换测试
 */

import { describe, it, expect } from 'vitest';
import { formIRToSchema, schemaToFormIR, dashboardIRToSchema, exportIRSchemaContract } from '../../ir/ir-to-schema';
import type { FormPageIR } from '../../ir/types';

// 最小 IR fixture
const minimalIR: FormPageIR = {
  type: 'form',
  id: 'test-form',
  name: '测试表单',
  config: {
    labelPosition: 'right',
    labelWidth: 100,
    labelSuffix: '',
    size: 'default',
    disabled: false,
    span: 24,
    gutter: 12,
    colon: true,
    popupType: 'general',
    generalWidth: '600px',
    fullScreenWidth: '80%',
    drawerWidth: '600px',
    hasCancelBtn: true,
    cancelButtonText: '取消',
    hasConfirmBtn: true,
    confirmButtonText: '确定',
    hasConfirmAndAddBtn: false,
    hasPrintBtn: false,
    printButtonText: '打印',
    primaryKeyPolicy: 'auto',
    tablePolicy: 'single',
    concurrencyLock: false,
    logicalDelete: false,
  },
  fields: [
    {
      id: 'f1',
      model: 'userName',
      label: '用户名',
      component: {
        jnpfKey: 'JnpfInput',
        pc: 'JnpfInput',
        app: 'uni-easyinput',
        legacyApp: 'uni-easyinput',
      },
      config: {
        required: true,
        defaultValue: '',
        placeholder: '请输入用户名',
        disabled: false,
        readonly: false,
        hidden: false,
        span: 24,
        labelWidth: null,
        maxlength: 50,
        showWordLimit: false,
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
      validation: [],
      events: {},
    },
    {
      id: 'f2',
      model: 'email',
      label: '邮箱',
      component: {
        jnpfKey: 'JnpfInput',
        pc: 'JnpfInput',
        app: 'uni-easyinput',
        legacyApp: 'uni-easyinput',
      },
      config: {
        required: false,
        defaultValue: '',
        placeholder: '请输入邮箱',
        disabled: false,
        readonly: false,
        hidden: false,
        span: 24,
        labelWidth: null,
        maxlength: null,
        showWordLimit: false,
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
      validation: [],
      events: {},
    },
  ],
  databaseFields: [],
  expressions: [],
  listConfig: {
    searchFields: [
      {
        field: 'userName',
        label: '用户名',
        component: 'JnpfInput',
        options: [],
      },
    ],
    columns: [],
    ruleList: [],
  },
};

// ============================================================
// 双向转换
// ============================================================

describe('IR ↔ Schema 双向转换', () => {
  it('FormPageIR → Schema → FormPageIR round-trip', () => {
    const schema = formIRToSchema(minimalIR);
    const ir2 = schemaToFormIR(schema);

    expect(ir2).not.toBeNull();
    expect(ir2!.type).toBe('form');
    expect(ir2!.fields.length).toBe(minimalIR.fields.length);

    // 核心字段保留
    for (let i = 0; i < minimalIR.fields.length; i++) {
      expect(ir2!.fields[i].model).toBe(minimalIR.fields[i].model);
      expect(ir2!.fields[i].label).toBe(minimalIR.fields[i].label);
      expect(ir2!.fields[i].component.jnpfKey).toBe(minimalIR.fields[i].component.jnpfKey);
    }

    // listConfig 保留
    expect(ir2!.listConfig?.searchFields?.length).toBe(1);
  });

  it('无效 Schema 返回 null', () => {
    expect(schemaToFormIR({})).toBeNull();
    expect(schemaToFormIR({ formData: 123 })).toBeNull();
    expect(
      schemaToFormIR({
        formData: 'invalid json{{{',
      }),
    ).toBeNull();
  });

  it('DashboardIR → Schema', () => {
    const mockDashboard = {
      type: 'dashboard',
      id: 'test',
      name: 'Test Dashboard',
      size: { width: 1920, height: 1080 },
      background: { type: 'color', value: '#000' },
      theme: 'dark',
      widgets: [],
      dataSources: [],
    };
    const schema = dashboardIRToSchema(mockDashboard);
    expect(schema.dashboardName).toBe('Test Dashboard');
    expect(schema.widgetCount).toBe(0);
  });

  it('JSON Schema 契约可导出', () => {
    const contract = exportIRSchemaContract();
    expect(contract.version).toBe('1.0.0');
    expect(contract.exports).toBeDefined();
    expect((contract.exports as Record<string, unknown>).formIRToSchema).toBeDefined();
  });
});
