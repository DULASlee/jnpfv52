import { describe, it, expect, afterAll } from 'vitest';
import { cleanSchema } from '../ir/schema-cleaner';
import { Vue3Compiler } from '../compiler/vue3/compiler';
import { validateIR, hasErrors } from '../ir/validator';
import * as fs from 'node:fs';
import * as path from 'node:path';

const outputDir = path.resolve(__dirname, '../../.tmp/e2e-student');

afterAll(() => {
  if (fs.existsSync(outputDir)) {
    fs.rmSync(outputDir, { recursive: true, force: true });
  }
});

describe('End-to-End Pipeline', () => {
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

  it('Schema → IR → Compiler → GeneratedProject complete pipeline', () => {
    const ir = cleanSchema(minimalSchema);
    expect(ir.type).toBe('form');
    expect(ir.fields.length).toBeGreaterThan(0);

    const issues = validateIR(ir);
    expect(hasErrors(issues)).toBe(false);

    const compiler = new Vue3Compiler({ entity: 'student', entityLabel: '学生管理' });
    const result = compiler.compile(ir);

    expect(result.project.size).toBeGreaterThan(0);
    expect(result.project.has('src/types/student.ts')).toBe(true);
    expect(result.project.has('src/api/student.ts')).toBe(true);
    expect(result.project.has('src/views/student/index.vue')).toBe(true);
    expect(result.project.has('src/views/student/columns.ts')).toBe(true);
    expect(result.project.has('src/views/student/search.ts')).toBe(true);
    expect(result.project.has('src/views/student/form.vue')).toBe(true);
    expect(result.project.has('src/composables/useStudent.ts')).toBe(true);
  });

  it('generated code quality checks', () => {
    const ir = cleanSchema(minimalSchema);
    const compiler = new Vue3Compiler({ entity: 'student', entityLabel: '学生管理' });
    const result = compiler.compile(ir);

    for (const [filePath, content] of result.project) {
      expect(content).not.toMatch(/\beval\s*\(/);
      expect(content).not.toMatch(/new\s+Function\s*\(/);
      expect(content).toContain('@jnpf-generated');

      if (filePath.endsWith('.vue')) {
        expect(content).toContain('@jnpf-gen:insert-point');
      }

      if (filePath.endsWith('.ts') && filePath.includes('types/')) {
        expect(content).toContain('export interface');
      }
    }
  });

  it('generated project writes to disk', () => {
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
  });

  it('zero warnings for simple schema', () => {
    const ir = cleanSchema(minimalSchema);
    const compiler = new Vue3Compiler({ entity: 'student', entityLabel: '学生管理' });
    const result = compiler.compile(ir);
    // Simple schema has empty expressions = 0 warnings
    expect(result.complexExpressions.length).toBe(0);
  });
});
