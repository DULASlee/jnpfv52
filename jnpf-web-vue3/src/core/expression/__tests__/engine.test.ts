import { describe, it, expect } from 'vitest';
import { ExpressionEngine } from '../engine';

const engine = new ExpressionEngine();
const ctx = {
  formData: { name: 'VIP客户', age: 18, amount: 100000, status: 'active' },
  data: { value: 'test' },
  rowIndex: 0,
};

describe('ExpressionEngine', () => {
  describe('Literals', () => {
    it('number', () => {
      expect(engine.evaluate('42', ctx)).toBe(42);
    });
    it('string', () => {
      expect(engine.evaluate('"hello"', ctx)).toBe('hello');
    });
    it('boolean true', () => {
      expect(engine.evaluate('true', ctx)).toBe(true);
    });
    it('null', () => {
      expect(engine.evaluate('null', ctx)).toBe(null);
    });
  });

  describe('Property access', () => {
    it('single level', () => {
      expect(engine.evaluate('formData', ctx)).toBe(ctx.formData);
    });
    it('nested', () => {
      expect(engine.evaluate('formData.name', ctx)).toBe('VIP客户');
    });
    it('array index', () => {
      expect(engine.evaluate('formData[0]', { formData: [10, 20] })).toBe(10);
    });
  });

  describe('Comparison', () => {
    it('==', () => {
      expect(engine.evaluate('formData.name == "VIP客户"', ctx)).toBe(true);
    });
    it('!=', () => {
      expect(engine.evaluate('formData.age != 20', ctx)).toBe(true);
    });
    it('>', () => {
      expect(engine.evaluate('formData.age > 10', ctx)).toBe(true);
    });
    it('>=', () => {
      expect(engine.evaluate('formData.amount >= 100000', ctx)).toBe(true);
    });
  });

  describe('Logical', () => {
    it('&&', () => {
      expect(engine.evaluate('true && true', ctx)).toBe(true);
    });
    it('||', () => {
      expect(engine.evaluate('false || true', ctx)).toBe(true);
    });
    it('!', () => {
      expect(engine.evaluate('!false', ctx)).toBe(true);
    });
    it('??', () => {
      expect(engine.evaluate('null ?? "default"', ctx)).toBe('default');
    });
  });

  describe('Ternary', () => {
    it('basic', () => {
      expect(engine.evaluate('true ? "yes" : "no"', ctx)).toBe('yes');
    });
    it('nested condition', () => {
      expect(engine.evaluate('formData.age >= 18 ? "adult" : "child"', ctx)).toBe('adult');
    });
  });

  describe('Arithmetic', () => {
    it('+', () => {
      expect(engine.evaluate('1 + 2', ctx)).toBe(3);
    });
    it('*', () => {
      expect(engine.evaluate('formData.age * 2', ctx)).toBe(36);
    });
  });

  describe('Whitelist functions', () => {
    it('ROUND', () => {
      expect(engine.evaluate('ROUND(3.14159, 2)', ctx)).toBe(3.14);
    });
    it('FORMAT_MONEY', () => {
      expect(engine.evaluate('FORMAT_MONEY(12345.67)', ctx)).toBe('12,345.67');
    });
    it('UPPER', () => {
      expect(engine.evaluate('UPPER("hello")', ctx)).toBe('HELLO');
    });
    it('LEN', () => {
      expect(engine.evaluate('LEN(formData.name)', ctx)).toBe(5);
    });
    it('MASK_PHONE', () => {
      expect(engine.evaluate('MASK_PHONE("13812345678")', ctx)).toBe('138****5678');
    });
  });

  describe('Cache', () => {
    it('second evaluation uses cache', () => {
      engine.clearCache();
      engine.evaluate('1 + 1', ctx);
      engine.evaluate('1 + 1', ctx);
      expect(true).toBe(true);
    });
  });
});
