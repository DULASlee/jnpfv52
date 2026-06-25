/**
 * F-5.1 端到端链路验证
 *
 * 管线：Schema → cleanSchema() → FormPageIR → Vue3Compiler.compile() → GeneratedProject
 *
 * 验证步骤（7 项）：
 *   1. 清洗 → IR type 为 'form'，fields 非空
 *   2. 验证 → validateIR 无 error 级问题
 *   3. 编译 → GeneratedProject 包含 types/api/views/composables
 *   4. 零 eval/Function → 所有生成文件无 eval / new Function
 *   5. 生成标记 → 所有 .vue/.ts 文件含 @jnpf-generated
 *   6. insert-point → 所有 .vue 文件含 @jnpf-gen:insert-point
 *   7. TypeScript 类型 → types 文件含 export interface
 */

import { describe, it, expect, afterAll } from 'vitest';
import { cleanSchema } from '../ir/schema-cleaner';
import { Vue3Compiler } from '../compiler/vue3/compiler';
import { validateIR } from '../ir/validator';
import * as fs from 'node:fs';
import * as path from 'node:path';

const fixturesDir = path.resolve(__dirname, '../ir/__tests__/fixtures');
const outputDir = path.resolve(__dirname, '../../.tmp/e2e-student');

// ============================================================
// 加载 schema-multi-field.json（14 种字段类型全覆盖）
// ============================================================
const multiFieldRaw = JSON.parse(fs.readFileSync(path.join(fixturesDir, 'schema-multi-field.json'), 'utf-8'));

afterAll(() => {
  if (fs.existsSync(outputDir)) {
    fs.rmSync(outputDir, { recursive: true, force: true });
  }
});

// ============================================================
// 通用验证 helper
// ============================================================
function verifyPipeline(schema: unknown, options: { entity: string; entityLabel: string; expectedFiles: string[] }) {
  // Step 1: 清洗
  const ir = cleanSchema(schema);
  expect(ir.type).toBe('form');
  expect(ir.fields.length).toBeGreaterThan(0);

  // Step 2: 验证
  const issues = validateIR(ir);
  const errors = issues.filter(i => i.level === 'error');
  if (errors.length > 0) {
    console.error('IR validation errors:', JSON.stringify(errors, null, 2));
  }
  expect(errors).toEqual([]);

  // Step 3: 编译
  const compiler = new Vue3Compiler({
    entity: options.entity,
    entityLabel: options.entityLabel,
  });
  const result = compiler.compile(ir);
  expect(result.project.size).toBeGreaterThan(0);

  // 检查所有预期文件存在
  for (const filePath of options.expectedFiles) {
    expect(result.project.has(filePath)).toBe(true);
  }

  // Step 4-7: 代码质量检查
  for (const [filePath, content] of result.project) {
    // Step 4: 零 eval/Function
    expect(content).not.toMatch(/\beval\s*\(/);
    expect(content).not.toMatch(/new\s+Function\s*\(/);

    // Step 5: 生成标记
    if (filePath.endsWith('.vue') || filePath.endsWith('.ts')) {
      expect(content).toContain('@jnpf-generated');
    }

    // Step 6: insert-point
    if (filePath.endsWith('.vue')) {
      expect(content).toContain('@jnpf-gen:insert-point');
    }

    // Step 7: TypeScript 类型
    if (filePath.endsWith('.ts') && filePath.includes('types/')) {
      expect(content).toContain('export interface');
    }
  }

  return { ir, result };
}

// ============================================================
// 测试用例
// ============================================================

describe('End-to-End Pipeline (F-5.1)', () => {
  // ── 最小 Schema ──
  const minimalSchema = {
    data: {
      formData: JSON.stringify({
        fields: [
          {
            __vModel__: 'name',
            __config__: { label: '姓名', tag: 'JnpfInput', jnpfKey: 'JnpfInput', required: true },
            placeholder: '请输入姓名',
            on: { change: '({ data }) => {}' },
          },
          {
            __vModel__: 'age',
            __config__: { label: '年龄', tag: 'JnpfInputNumber', jnpfKey: 'JnpfInputNumber' },
            on: {},
          },
        ],
        funcs: {
          onLoad: '({ data }) => {}',
          beforeSubmit: '({ data }) => { return new Promise((r) => r(1)) }',
        },
        virtualFieldList: [
          { field: 'name', type: 'varchar', length: 50 },
          { field: 'age', type: 'int' },
        ],
        labelWidth: 100,
        popupType: 'general',
        generalWidth: '800px',
      }),
    },
  };

  it('F-5.1 最小 Schema 完整 7 步管线', () => {
    verifyPipeline(minimalSchema, {
      entity: 'student',
      entityLabel: '学生管理',
      expectedFiles: [
        'src/types/student.ts',
        'src/api/student.ts',
        'src/views/student/index.vue',
        'src/views/student/columns.ts',
        'src/views/student/search.ts',
        'src/views/student/form.vue',
        'src/composables/useStudent.ts',
      ],
    });
  });

  it('生成项目可写入磁盘', () => {
    const ir = cleanSchema(minimalSchema);
    const compiler = new Vue3Compiler({ entity: 'student', entityLabel: '学生管理' });
    const result = compiler.compile(ir);

    fs.mkdirSync(outputDir, { recursive: true });
    for (const [filePath, content] of result.project) {
      const fullPath = path.join(outputDir, filePath);
      fs.mkdirSync(path.dirname(fullPath), { recursive: true });
      fs.writeFileSync(fullPath, content, 'utf-8');
    }

    expect(fs.existsSync(path.join(outputDir, 'src/types/student.ts'))).toBe(true);
    expect(fs.existsSync(path.join(outputDir, 'src/views/student/index.vue'))).toBe(true);
    expect(fs.readFileSync(path.join(outputDir, 'src/views/student/index.vue'), 'utf-8')).toContain('@jnpf-generated');
  });

  it('简单 Schema 零警告', () => {
    const ir = cleanSchema(minimalSchema);
    const compiler = new Vue3Compiler({ entity: 'student', entityLabel: '学生管理' });
    const result = compiler.compile(ir);
    expect(result.complexExpressions.length).toBe(0);
  });

  // ── 14 种字段类型 Schema (schema-multi-field.json) ──
  const multiFieldExpectedFiles = [
    'src/types/multi-field-employee.ts',
    'src/api/multi-field-employee.ts',
    'src/views/multi-field-employee/index.vue',
    'src/views/multi-field-employee/columns.ts',
    'src/views/multi-field-employee/search.ts',
    'src/views/multi-field-employee/form.vue',
    'src/composables/useMulti-field-employee.ts',
  ];

  it('F-5.1 multi-field Schema (14 字段类型) 完整 7 步管线', () => {
    verifyPipeline(multiFieldRaw, {
      entity: 'multi-field-employee',
      entityLabel: '员工档案',
      expectedFiles: multiFieldExpectedFiles,
    });
  });

  it('multi-field Schema — 14 字段全部出现在生成类型中', () => {
    const ir = cleanSchema(multiFieldRaw);
    const compiler = new Vue3Compiler({ entity: 'multi-field-employee', entityLabel: '员工档案' });
    const result = compiler.compile(ir);

    const typesContent = result.project.get('src/types/multi-field-employee.ts');
    expect(typesContent).toBeTruthy();

    // 验证 14 个字段名全部出现在类型定义中
    const fieldModels = ir.fields.map(f => f.model);
    expect(fieldModels.length).toBe(14);
    for (const model of fieldModels) {
      expect(typesContent).toContain(model);
    }
  });

  it('multi-field Schema — 列表页含 search.ts + columns.ts', () => {
    const ir = cleanSchema(multiFieldRaw);
    const compiler = new Vue3Compiler({ entity: 'multi-field-employee', entityLabel: '员工档案' });
    const result = compiler.compile(ir);

    const indexContent = result.project.get('src/views/multi-field-employee/index.vue');
    expect(indexContent).toBeTruthy();
    expect(indexContent).toContain('@jnpf-gen:insert-point');

    const columnsContent = result.project.get('src/views/multi-field-employee/columns.ts');
    expect(columnsContent).toBeTruthy();
    // 5 columns from columnData
    expect(columnsContent).toContain('employeeName');
    expect(columnsContent).toContain('department');
  });

  it('multi-field Schema — composable 含 API 方法', () => {
    const ir = cleanSchema(multiFieldRaw);
    const compiler = new Vue3Compiler({ entity: 'multi-field-employee', entityLabel: '员工档案' });
    const result = compiler.compile(ir);

    const composable = result.project.get('src/composables/useMulti-field-employee.ts');
    expect(composable).toBeTruthy();
    // Should contain CRUD methods
    expect(composable).toMatch(/getList|getInfo|create|update|delete/);
  });

  it('multi-field Schema — 所有文件零 eval / new Function', () => {
    const ir = cleanSchema(multiFieldRaw);
    const compiler = new Vue3Compiler({ entity: 'multi-field-employee', entityLabel: '员工档案' });
    const result = compiler.compile(ir);

    for (const [filePath, content] of result.project) {
      expect(content).not.toMatch(/\beval\s*\(/);
      expect(content).not.toMatch(/new\s+Function\s*\(/);
      expect(content).toContain('@jnpf-generated');

      if (filePath.endsWith('.vue')) {
        expect(content).toContain('@jnpf-gen:insert-point');
      }
    }
  });

  it('multi-field Schema — 所有组件正确映射到 IR', () => {
    const ir = cleanSchema(multiFieldRaw);
    const expectedJnpfKeys = [
      'JnpfInput',
      'JnpfRadio',
      'JnpfDatePicker',
      'JnpfDepSelect',
      'JnpfPosSelect',
      'JnpfUserSelect',
      'JnpfInputNumber',
      'JnpfSwitch',
      'JnpfCheckbox',
      'JnpfUploadImg',
      'JnpfUploadFile',
    ];

    const actualKeys = [...new Set(ir.fields.map(f => f.component.jnpfKey))];
    for (const key of expectedJnpfKeys) {
      expect(actualKeys).toContain(key);
    }
  });
});
