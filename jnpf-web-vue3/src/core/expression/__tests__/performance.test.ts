import { describe, it, expect } from 'vitest';
import { ExpressionEngine } from '../engine';

describe('Performance', () => {
  it('1000 expressions evaluated in < 30ms (PC baseline)', () => {
    const engine = new ExpressionEngine();
    const ctx = { formData: { name: 'test', age: 18, amount: 100000 } };
    const exprs = ['formData.name == "test"', 'formData.age > 10', 'formData.amount >= 50000', 'true && formData.age > 0', 'formData.name ?? "default"'];

    const start = performance.now();
    for (let i = 0; i < 200; i++) {
      for (const expr of exprs) {
        engine.evaluate(expr, ctx);
      }
    }
    const elapsed = performance.now() - start;

    console.log(`[perf] 1000 求值耗时: ${elapsed.toFixed(2)}ms`);
    expect(elapsed).toBeLessThan(30);
  });
});
