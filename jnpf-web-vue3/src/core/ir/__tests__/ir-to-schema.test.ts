/**
 * IR ↔ Schema 双向转换测试 (new API)
 *
 * 测试 formIRToSchema / schemaToFormIR round-trip
 */

import { describe, it, expect } from 'vitest';
import { cleanSchema } from '../schema-cleaner';
import { formIRToSchema, schemaToFormIR, dashboardIRToSchema, exportIRSchemaContract } from '../ir-to-schema';

const minimalFormInput = {
  data: {
    formData: JSON.stringify({
      fields: [
        {
          __vModel__: 'name',
          __config__: {
            label: '姓名',
            tag: 'JnpfInput',
            jnpfKey: 'JnpfInput',
            required: true,
          },
        },
        {
          __vModel__: 'age',
          __config__: {
            label: '年龄',
            tag: 'JnpfInputNumber',
            jnpfKey: 'JnpfInputNumber',
          },
        },
      ],
      tabs: {},
      virtualFieldList: [],
    }),
  },
};

describe('irToSchema — Round-trip', () => {
  it('Schema → IR → Schema 字段数量一致', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = formIRToSchema(ir);
    const ir2 = schemaToFormIR(schema);
    expect(ir2).not.toBeNull();
    expect(ir2!.fields.length).toBe(ir.fields.length);
  });

  it('字段名 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = formIRToSchema(ir);
    const ir2 = schemaToFormIR(schema);
    expect(ir2!.fields[0].model).toBe(ir.fields[0].model);
    expect(ir2!.fields[1].model).toBe(ir.fields[1].model);
  });

  it('组件映射 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = formIRToSchema(ir);
    const ir2 = schemaToFormIR(schema);
    expect(ir2!.fields[0].component.jnpfKey).toBe(ir.fields[0].component.jnpfKey);
    expect(ir2!.fields[1].component.jnpfKey).toBe(ir.fields[1].component.jnpfKey);
  });

  it('required 标记 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = formIRToSchema(ir);
    const ir2 = schemaToFormIR(schema);
    expect(ir2!.fields[0].config.required).toBe(true);
    expect(ir2!.fields[1].config.required).toBe(false);
  });

  it('无效 Schema 返回 null', () => {
    expect(schemaToFormIR({})).toBeNull();
    expect(schemaToFormIR({ formData: 123 })).toBeNull();
    expect(schemaToFormIR({ formData: 'bad json' })).toBeNull();
  });

  it('空字段 IR 不抛异常', () => {
    const emptyIR = cleanSchema({
      data: {
        formData: JSON.stringify({
          fields: [],
          tabs: {},
          virtualFieldList: [],
        }),
      },
    });
    const schema = formIRToSchema(emptyIR);
    const ir2 = schemaToFormIR(schema);
    expect(ir2).not.toBeNull();
    expect(ir2!.fields.length).toBe(0);
  });

  it('DashboardIR → Schema', () => {
    const mockDashboard = {
      type: 'dashboard',
      id: 'd1',
      name: 'Test',
      size: { width: 1920, height: 1080 },
      background: { type: 'color', value: '#000' },
      theme: 'dark',
      widgets: [],
      dataSources: [],
    };
    const schema = dashboardIRToSchema(mockDashboard);
    expect(schema.dashboardName).toBe('Test');
    expect(schema.widgetCount).toBe(0);
  });

  it('JSON Schema 契约可导出', () => {
    const contract = exportIRSchemaContract();
    expect(contract.version).toBe('1.0.0');
    expect(contract.exports).toBeDefined();
  });
});
