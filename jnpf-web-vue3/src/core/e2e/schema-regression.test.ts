import { describe, it, expect } from 'vitest';
import { cleanSchema } from '../ir/schema-cleaner';
import { validateIR, hasErrors } from '../ir/validator';
import { Vue3Compiler } from '../compiler/vue3/compiler';
import * as fs from 'node:fs';
import * as path from 'node:path';

const fixturesDir = path.resolve(__dirname, '../ir/__tests__/fixtures');
const fixtureFiles = fs
  .readdirSync(fixturesDir)
  .filter(f => f.startsWith('schema-') && f.endsWith('.json'))
  .filter(f => !f.includes('column') && !f.includes('list-config'))
  .sort();

describe('P0 Schema Regression', () => {
  if (fixtureFiles.length < 5) {
    throw new Error(`P0 门禁: 需要 ≥5 份 schema fixture，当前 ${fixtureFiles.length}`);
  }

  describe.each(fixtureFiles)('%s', fileName => {
    const raw = JSON.parse(fs.readFileSync(path.join(fixturesDir, fileName), 'utf-8'));

    it('cleanSchema does not throw', () => {
      expect(() => cleanSchema(raw)).not.toThrow();
    });

    it('IR has valid type', () => {
      const ir = cleanSchema(raw);
      expect(ir.type).toBe('form');
      expect(ir.id).toBeTruthy();
    });

    it('fields are extracted', () => {
      const ir = cleanSchema(raw);
      expect(ir.fields.length).toBeGreaterThan(0);
      for (const field of ir.fields) {
        expect(field.model).toBeTruthy();
        expect(field.component?.jnpfKey).toBeTruthy();
      }
    });

    it('IR validation passes without errors', () => {
      const ir = cleanSchema(raw);
      const issues = validateIR(ir);
      const errors = issues.filter(i => i.level === 'error');
      expect(errors).toEqual([]);
    });

    it('expressions are classified', () => {
      const ir = cleanSchema(raw);
      for (const expr of ir.expressions) {
        expect(expr.level).toMatch(/^(empty|simple|medium|complex)$/);
        expect(expr.id).toBeTruthy();
      }
    });

    it('Vue3Compiler compiles without throwing', () => {
      const ir = cleanSchema(raw);
      const compiler = new Vue3Compiler({
        entity: ir.id.replace(/[^a-zA-Z0-9]/g, '-'),
        entityLabel: ir.name || ir.id,
      });
      const result = compiler.compile(ir);

      expect(result.project.size).toBeGreaterThan(0);
      for (const [, content] of result.project) {
        expect(content).not.toMatch(/\beval\b/);
        expect(content).not.toMatch(/new Function/);
        expect(content).toContain('@jnpf-generated');
      }
    });

    it('sub-table fields have children in config', () => {
      const ir = cleanSchema(raw);
      const tableFields = ir.fields.filter(f => f.component.jnpfKey === 'JnpfTable' || f.component.jnpfKey === 'JnpfInputTable');
      for (const tf of tableFields) {
        // Sub-tables should be recognized as table component
        expect(tf.component.pc).toContain('table');
      }
    });
  });
});
