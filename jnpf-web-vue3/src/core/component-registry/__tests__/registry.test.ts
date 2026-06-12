import { describe, it, expect } from 'vitest';
import { ComponentRegistry } from '../registry';
import { BUILTIN_COMPONENTS } from '../builtin';

describe('ComponentRegistry', () => {
  describe('Register and resolve', () => {
    it('registers and resolves a component', () => {
      const reg = new ComponentRegistry();
      reg.register({
        type: 'Test',
        name: '测试',
        category: 'other',
        pc: 'div',
        app: 'view',
      });
      const entry = reg.resolve('Test');
      expect(entry.type).toBe('Test');
      expect(entry.pc).toBe('div');
    });

    it('returns fallback for unknown type', () => {
      const reg = new ComponentRegistry();
      const entry = reg.resolve('UnknownComponent');
      expect(entry.pc).toBe('a-input');
      expect(entry.app).toBe('uni-easyinput');
    });

    it('registers batch', () => {
      const reg = new ComponentRegistry();
      reg.registerBatch(BUILTIN_COMPONENTS);
      expect(reg.list().length).toBe(BUILTIN_COMPONENTS.length);
    });
  });

  describe('Category lookup', () => {
    it('filters by category', () => {
      const reg = new ComponentRegistry();
      reg.registerBatch(BUILTIN_COMPONENTS);
      const inputs = reg.getByCategory('form-input');
      expect(inputs.length).toBeGreaterThan(0);
      expect(inputs.every(i => i.category === 'form-input')).toBe(true);
    });
  });

  describe('Deprecation', () => {
    it('resolves deprecated component', () => {
      const reg = new ComponentRegistry();
      reg.register({
        type: 'OldInput',
        name: '旧输入框',
        category: 'form-input',
        pc: 'old-input',
        app: 'old-easyinput',
        deprecated: true,
        replacedBy: 'JnpfInput',
      });
      const entry = reg.resolve('OldInput');
      expect(entry.type).toBe('OldInput');
    });
  });

  describe('Builtin completeness', () => {
    it('all builtin components are registered', () => {
      const reg = new ComponentRegistry();
      reg.registerBatch(BUILTIN_COMPONENTS);
      for (const comp of BUILTIN_COMPONENTS) {
        expect(reg.has(comp.type)).toBe(true);
      }
    });

    it('all builtin components have pc and app mapping', () => {
      const reg = new ComponentRegistry();
      reg.registerBatch(BUILTIN_COMPONENTS);
      for (const comp of BUILTIN_COMPONENTS) {
        const entry = reg.resolve(comp.type);
        expect(entry.pc).toBeTruthy();
        expect(entry.app).toBeTruthy();
      }
    });

    it('stats are correct', () => {
      const reg = new ComponentRegistry();
      reg.registerBatch(BUILTIN_COMPONENTS);
      const stats = reg.stats();
      expect(stats.total).toBe(BUILTIN_COMPONENTS.length);
      expect(stats.byCategory['form-input']).toBe(3);
      expect(stats.byCategory['chart']).toBe(4);
    });

    it('resolveMapping returns simplified mapping', () => {
      const reg = new ComponentRegistry();
      reg.registerBatch(BUILTIN_COMPONENTS);
      const mapping = reg.resolveMapping('JnpfInput');
      expect(mapping.pc).toBe('a-input');
      expect(mapping.app).toBe('uni-easyinput');
    });
  });
});
