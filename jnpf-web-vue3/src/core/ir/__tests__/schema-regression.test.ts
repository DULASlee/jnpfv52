/**
 * Schema 回归测试 — Sprint 0-A Day 2 遗留
 *
 * 验证核心 IR 管线：cleanSchema → validateIR → formIRToSchema → schemaToFormIR
 */

import { describe, it, expect } from 'vitest';
import { cleanSchema } from '../schema-cleaner';
import { validateIR } from '../validator';
import { formIRToSchema, schemaToFormIR } from '../ir-to-schema';

// ─── 生产级 Schema fixtures ───

const fixtures = [
  {
    name: 'minimal-form',
    schema: {
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
          ],
          tabs: {},
          virtualFieldList: [],
        }),
      },
    },
  },
  {
    name: 'multi-field',
    schema: {
      data: {
        formData: JSON.stringify({
          fields: [
            {
              __vModel__: 'userName',
              __config__: {
                label: '用户名',
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
            {
              __vModel__: 'active',
              __config__: {
                label: '是否激活',
                tag: 'JnpfSwitch',
                jnpfKey: 'JnpfSwitch',
              },
            },
            {
              __vModel__: 'gender',
              __config__: {
                label: '性别',
                tag: 'JnpfSelect',
                jnpfKey: 'JnpfSelect',
                options: [
                  { value: 'M', label: '男' },
                  { value: 'F', label: '女' },
                ],
              },
            },
          ],
          tabs: {},
          virtualFieldList: [],
        }),
      },
    },
  },
  {
    name: 'with-search-fields',
    schema: {
      data: {
        formData: JSON.stringify({
          fields: [
            {
              __vModel__: 'orderNo',
              __config__: {
                label: '订单号',
                tag: 'JnpfInput',
                jnpfKey: 'JnpfInput',
                required: true,
              },
            },
          ],
          tabs: {},
          virtualFieldList: [],
        }),
        columnData: JSON.stringify({
          searchList: [
            {
              __vModel__: 'orderNo',
              __config__: { label: '订单号' },
            },
          ],
          columnList: [],
        }),
      },
    },
  },
  {
    name: 'with-date-picker',
    schema: {
      data: {
        formData: JSON.stringify({
          fields: [
            {
              __vModel__: 'startDate',
              __config__: {
                label: '开始日期',
                tag: 'JnpfDatePicker',
                jnpfKey: 'JnpfDatePicker',
                required: true,
              },
            },
            {
              __vModel__: 'endDate',
              __config__: {
                label: '结束日期',
                tag: 'JnpfDatePicker',
                jnpfKey: 'JnpfDatePicker',
              },
            },
          ],
          tabs: {},
          virtualFieldList: [],
        }),
      },
    },
  },
  {
    name: 'with-checkbox',
    schema: {
      data: {
        formData: JSON.stringify({
          fields: [
            {
              __vModel__: 'hobbies',
              __config__: {
                label: '爱好',
                tag: 'JnpfCheckbox',
                jnpfKey: 'JnpfCheckbox',
                options: [
                  { value: 'reading', label: '阅读' },
                  { value: 'sports', label: '运动' },
                ],
                multiple: true,
              },
            },
          ],
          tabs: {},
          virtualFieldList: [],
        }),
      },
    },
  },
];

// ============================================================
// 回归测试
// ============================================================

describe('Schema 回归测试', () => {
  for (const fixture of fixtures) {
    describe(fixture.name, () => {
      it('cleanSchema 不抛异常', () => {
        expect(() => cleanSchema(fixture.schema)).not.toThrow();
      });

      it('validateIR 无 error', () => {
        const ir = cleanSchema(fixture.schema);
        const issues = validateIR(ir);
        const errors = issues.filter(i => i.level === 'error');
        expect(errors).toHaveLength(0);
      });

      it('round-trip: IR → Schema → IR 字段数量一致', () => {
        const ir = cleanSchema(fixture.schema);
        const schema = formIRToSchema(ir);
        const ir2 = schemaToFormIR(schema);

        expect(ir2).not.toBeNull();
        expect(ir2!.fields.length).toBe(ir.fields.length);

        // 核心字段 model/label/jnpfKey 保留
        for (let i = 0; i < ir.fields.length; i++) {
          expect(ir2!.fields[i].model).toBe(ir.fields[i].model);
          expect(ir2!.fields[i].label).toBe(ir.fields[i].label);
          expect(ir2!.fields[i].component.jnpfKey).toBe(ir.fields[i].component.jnpfKey);
        }
      });
    });
  }
});
