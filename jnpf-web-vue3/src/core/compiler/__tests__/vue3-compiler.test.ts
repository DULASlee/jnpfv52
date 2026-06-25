import { describe, it, expect } from 'vitest';
import { Vue3Compiler } from '../vue3/compiler';
import { cleanSchema } from '../../ir/schema-cleaner';

const minimalSchema = {
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
          placeholder: '请输入姓名',
          on: { change: '({ data }) => {}' },
        },
        {
          __vModel__: 'age',
          __config__: {
            label: '年龄',
            tag: 'JnpfInputNumber',
            jnpfKey: 'JnpfInputNumber',
          },
          on: {},
        },
        {
          __vModel__: 'status',
          __config__: {
            label: '状态',
            tag: 'JnpfSelect',
            jnpfKey: 'JnpfSelect',
          },
          options: [
            { label: '启用', value: 1 },
            { label: '禁用', value: 0 },
          ],
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
        { field: 'status', type: 'int' },
      ],
      labelWidth: 120,
      popupType: 'general',
      generalWidth: '800px',
    }),
  },
};

describe('Vue3Compiler', () => {
  const ir = cleanSchema(minimalSchema);
  const compiler = new Vue3Compiler({
    entity: 'student',
    entityLabel: '学生',
    apiBasePath: '/api/student',
  });
  const result = compiler.compile(ir);

  it('generates correct number of files', () => {
    expect(result.project.size).toBe(7);
  });

  it('generates types file with interface', () => {
    const types = result.project.get('src/types/student.ts');
    expect(types).toBeDefined();
    expect(types).toContain('StudentEntity');
    expect(types).toContain('name');
    expect(types).toContain('age');
    expect(types).toContain('status');
  });

  it('generates api file with CRUD functions', () => {
    const api = result.project.get('src/api/student.ts');
    expect(api).toBeDefined();
    expect(api).toContain('getStudentList');
    expect(api).toContain('getStudentDetail');
    expect(api).toContain('createStudent');
    expect(api).toContain('updateStudent');
    expect(api).toContain('deleteStudent');
    expect(api).toContain('batchDeleteStudent');
  });

  it('generates list page with template and script', () => {
    const listPage = result.project.get('src/views/student/index.vue');
    expect(listPage).toBeDefined();
    expect(listPage).toContain('<template>');
    expect(listPage).toContain('a-table');
    expect(listPage).toContain('script setup');
    expect(listPage).toContain('loadData');
  });

  it('generates form page with modal and form', () => {
    const formPage = result.project.get('src/views/student/form.vue');
    expect(formPage).toBeDefined();
    expect(formPage).toContain('a-modal');
    expect(formPage).toContain('a-form');
    expect(formPage).toContain('formData');
  });

  it('generates columns with action column', () => {
    const cols = result.project.get('src/views/student/columns.ts');
    expect(cols).toBeDefined();
    expect(cols).toContain('操作');
  });

  it('generates hook with composable', () => {
    const hook = result.project.get('src/composables/useStudent.ts');
    expect(hook).toBeDefined();
    expect(hook).toContain('useStudentList');
    expect(hook).toContain('loadData');
  });

  it('all files contain @jnpf-generated marker', () => {
    for (const [path, content] of result.project) {
      expect(content).toContain('@jnpf-generated');
      expect(content).toContain('entity=student');
    }
  });

  it('zero eval or new Function in generated code', () => {
    for (const [, content] of result.project) {
      expect(content).not.toMatch(/\beval\b/);
      expect(content).not.toMatch(/new Function/);
    }
  });

  it('insert-point placeholders present in list page', () => {
    const listPage = result.project.get('src/views/student/index.vue')!;
    expect(listPage).toContain('@jnpf-gen:insert-point=custom-actions');
    expect(listPage).toContain('@jnpf-gen:insert-point=custom-logic');
  });

  it('insert-point placeholders present in form page', () => {
    const formPage = result.project.get('src/views/student/form.vue')!;
    expect(formPage).toContain('@jnpf-gen:insert-point=custom-form-fields');
    expect(formPage).toContain('@jnpf-gen:insert-point=custom-imports');
    expect(formPage).toContain('@jnpf-gen:insert-point=custom-logic');
  });
});
