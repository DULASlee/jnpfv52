import { describe, it, expect } from 'vitest';
import { cleanSchema } from '../schema-cleaner';
import { irToSchema } from '../ir-to-schema';

// 复用 schema-cleaner 测试的带完整功能的 fixture
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
            trigger: 'blur',
            regList: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
          },
          placeholder: '请输入姓名',
          on: { change: '({ data, formData, setFormData }) => {}' },
        },
        {
          __vModel__: 'age',
          __config__: {
            label: '年龄',
            tag: 'JnpfInputNumber',
            jnpfKey: 'JnpfInputNumber',
            trigger: 'change',
            regList: [{ pattern: '/^\\d+$/', message: '请输入正确的数字', trigger: 'blur' }],
          },
          on: { change: '({ data, formData }) => { formData.name = data.value; }' },
        },
      ],
      funcs: {
        onLoad: '({ data, formData, setFormData }) => {}',
        beforeSubmit: '({ data, formData }) => { return new Promise((resolve) => { resolve(1); }) }',
      },
      labelPosition: 'left',
      labelWidth: 100,
      size: 'default',
      popupType: 'general',
      virtualFieldList: [
        { field: 'name', type: 'varchar', length: 50, nullable: false, defaultValue: null, description: '姓名' },
        { field: 'age', type: 'int', length: null, nullable: true, defaultValue: null, description: '年龄' },
      ],
    }),
  },
};

/** 解析 JNPF 双层 JSON 包装，返回 formData 对象 */
function unwrapSchemaOutput(output: any): Record<string, any> {
  const formDataStr = output?.data?.formData;
  if (typeof formDataStr === 'string') {
    return JSON.parse(formDataStr);
  }
  return formDataStr ?? {};
}

describe('irToSchema — Round-trip', () => {
  it('Schema → IR → Schema 字段数量一致', () => {
    const ir = cleanSchema(minimalFormInput);
    expect(ir.fields.length).toBe(2);

    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);

    expect(formData.fields).toBeDefined();
    expect(formData.fields.length).toBe(2);
  });

  it('字段名 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);

    const fieldNames = formData.fields.map((f: any) => f.__vModel__);
    expect(fieldNames).toContain('name');
    expect(fieldNames).toContain('age');
  });

  it('组件映射 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);

    const nameField = formData.fields.find((f: any) => f.__vModel__ === 'name');
    expect(nameField.__config__.tag).toBe('JnpfInput');
    expect(nameField.__config__.jnpfKey).toBe('JnpfInput');

    const ageField = formData.fields.find((f: any) => f.__vModel__ === 'age');
    expect(ageField.__config__.tag).toBe('JnpfInputNumber');
  });

  it('required 标记 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);

    const nameField = formData.fields.find((f: any) => f.__vModel__ === 'name');
    expect(nameField.__config__.required).toBe(true);

    const ageField = formData.fields.find((f: any) => f.__vModel__ === 'age');
    expect(ageField.__config__.required).toBe(false);
  });

  it('表单级配置 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);

    expect(formData.labelPosition).toBe('left');
    expect(formData.labelWidth).toBe(100);
    expect(formData.size).toBe('default');
    expect(formData.popupType).toBe('general');
  });

  it('生命周期函数 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);

    expect(formData.funcs).toBeDefined();
    expect(formData.funcs.onLoad).toBeTruthy();
    expect(formData.funcs.beforeSubmit).toBeTruthy();
  });

  it('字段事件 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);

    const nameField = formData.fields.find((f: any) => f.__vModel__ === 'name');
    expect(nameField.on).toBeDefined();
    expect(nameField.on.change).toBeTruthy();
  });

  it('数据库字段 round-trip 保持', () => {
    const ir = cleanSchema(minimalFormInput);
    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);

    expect(formData.virtualFieldList).toBeDefined();
    expect(formData.virtualFieldList.length).toBe(2);
    expect(formData.virtualFieldList[0].field).toBe('name');
    expect(formData.virtualFieldList[1].field).toBe('age');
  });

  it('空输入不抛异常', () => {
    const ir = cleanSchema({});
    expect(() => irToSchema(ir)).not.toThrow();
  });

  it('无字段的 IR 不抛异常', () => {
    const irInput = { data: { formData: JSON.stringify({ fields: [], funcs: {} }) } };
    const ir = cleanSchema(irInput);
    const schema = irToSchema(ir);
    const formData = unwrapSchemaOutput(schema);
    expect(formData.fields).toEqual([]);
  });
});
