import { describe, it, expect } from 'vitest';
import { cleanSchema } from '../schema-cleaner';
import { classifyExpression } from '../expression-classifier';

// 最小化测试 fixture（当外部 JSON 文件不可用时使用）
const minimalFormSchema = {
  data: {
    formData: JSON.stringify({
      fields: [
        {
          __vModel__: 'name',
          __config__: { label: '姓名', tag: 'JnpfInput', jnpfKey: 'JnpfInput', required: true },
          placeholder: '请输入姓名',
          on: { change: '({ data, formData }) => {}' },
        },
        {
          __vModel__: 'age',
          __config__: {
            label: '年龄',
            tag: 'JnpfInputNumber',
            jnpfKey: 'JnpfInputNumber',
          },
          on: { change: '({ data, formData }) => { formData.name = data.value; }' },
        },
      ],
      funcs: {
        onLoad: '({ data, formData, setFormData }) => {}',
        beforeSubmit: '({ data, formData }) => { return new Promise((resolve) => { resolve(1); }) }',
      },
      virtualFieldList: [
        { field: 'name', type: 'varchar', length: 50 },
        { field: 'age', type: 'int', length: null },
      ],
    }),
  },
};

describe('Schema Cleaner', () => {
  describe('Basic cleaning', () => {
    it('cleans minimal schema successfully', () => {
      const ir = cleanSchema(minimalFormSchema);
      expect(ir.type).toBe('form');
      expect(ir.config).toBeDefined();
      expect(ir.fields).toBeDefined();
      expect(ir.expressions).toBeDefined();
    });

    it('extracts correct field count', () => {
      const ir = cleanSchema(minimalFormSchema);
      expect(ir.fields.length).toBe(2);
    });

    it('maps components correctly', () => {
      const ir = cleanSchema(minimalFormSchema);
      expect(ir.fields[0].component.pc).toBe('a-input');
      expect(ir.fields[0].component.app).toBe('uni-easyinput');
      expect(ir.fields[0].component.jnpfKey).toBe('JnpfInput');
      expect(ir.fields[1].component.pc).toBe('a-input-number');
    });

    it('extracts database fields', () => {
      const ir = cleanSchema(minimalFormSchema);
      expect(ir.databaseFields.length).toBe(2);
      expect(ir.databaseFields[0].name).toBe('name');
      expect(ir.databaseFields[0].type).toBe('varchar');
    });

    it('extracts lifecycle expressions', () => {
      const ir = cleanSchema(minimalFormSchema);
      const lifecycle = ir.expressions.filter(e => e.type === 'form-lifecycle');
      expect(lifecycle.length).toBe(2);
      expect(lifecycle.some(e => e.name === 'onLoad')).toBe(true);
      expect(lifecycle.some(e => e.name === 'beforeSubmit')).toBe(true);
    });
  });

  describe('Expression classification', () => {
    it('marks empty function as empty', () => {
      const result = classifyExpression('({ data, formData }) => {}');
      expect(result.level).toBe('empty');
      expect(result.intentHints).toEqual([]);
    });

    it('marks simple assignment as simple', () => {
      const result = classifyExpression('({ data, formData }) => { formData.name = data.value; }');
      expect(result.level).toBe('simple');
    });

    it('marks Promise wrapper as medium', () => {
      const result = classifyExpression('({ data }) => { return new Promise((resolve) => { resolve(); }) }');
      expect(result.level).toBe('medium');
    });

    it('marks eval-containing code as complex', () => {
      const result = classifyExpression('({ data }) => { eval("alert(1)") }');
      expect(result.level).toBe('complex');
    });

    it('marks forEach/loop code as complex', () => {
      const result = classifyExpression('({ data, setFormData }) => { data.list.forEach(item => { setFormData(item); }) }');
      expect(result.level).toBe('complex');
    });

    it('detects business intent for amount', () => {
      const result = classifyExpression('({ data, formData }) => { formData.totalAmount = data.price * data.quantity; }');
      expect(result.intentHints).toContain('金额计算');
    });
  });

  describe('Malformed input', () => {
    it('handles missing __config__ without crashing', () => {
      const malformed = {
        data: {
          formData: JSON.stringify({
            fields: [{ __vModel__: 'test' }],
          }),
        },
      };
      const ir = cleanSchema(malformed);
      expect(ir.fields.length).toBe(1);
      expect(ir.fields[0].model).toBe('test');
    });

    it('marks eval in functions as complex without executing', () => {
      const malicious = {
        data: {
          formData: JSON.stringify({
            fields: [
              {
                __vModel__: 'x',
                __config__: { label: 'X', tag: 'JnpfInput' },
                on: { change: "() => { eval('alert(1)') }" },
              },
            ],
            funcs: {},
          }),
        },
      };
      const ir = cleanSchema(malicious);
      const expr = ir.expressions.find(e => e.type === 'field-event');
      expect(expr).toBeDefined();
      expect(expr!.level).toBe('complex');
      expect(expr!.originalCode).toContain('eval');
    });

    it('handles empty input', () => {
      const ir = cleanSchema({});
      expect(ir.type).toBe('form');
      expect(ir.fields).toEqual([]);
    });

    it('handles string-only input', () => {
      const ir = cleanSchema('not a json object');
      expect(ir.type).toBe('form');
      expect(ir.fields).toEqual([]);
    });
  });

  describe('AI probe injection', () => {
    it('infers semantic role from field name', () => {
      const schema = {
        data: {
          formData: JSON.stringify({
            fields: [
              {
                __vModel__: 'email',
                __config__: { label: '邮箱', tag: 'JnpfInput' },
              },
              {
                __vModel__: 'phoneNumber',
                __config__: { label: '手机号', tag: 'JnpfInput' },
              },
              {
                __vModel__: 'totalPrice',
                __config__: { label: '总金额', tag: 'JnpfInputNumber' },
              },
            ],
            funcs: {},
          }),
        },
      };
      const ir = cleanSchema(schema);
      expect(ir.fields[0].aiHints?.semantic).toBe('email');
      expect(ir.fields[1].aiHints?.semantic).toBe('phone');
      expect(ir.fields[2].aiHints?.semantic).toBe('currency');
    });
  });
});
