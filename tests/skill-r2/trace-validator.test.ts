import { describe, it, expect } from 'vitest';
import { validateTrace } from './trace-validator';
import { GOLDENS, NEGATIVES, dir } from './golden-traces';

describe('R2 Validator — golden traces must pass every invariant', () => {
  for (const [caseId, trace] of Object.entries(GOLDENS)) {
    it(`${caseId}: 0 violations`, () => {
      const v = validateTrace(trace, dir(caseId));
      expect(v.map(x => `${x.code}:${x.msg}`)).toEqual([]);
    });
  }
});

describe('R2 Validator — negative traces must be rejected by the right invariant', () => {
  for (const n of NEGATIVES) {
    it(`${n.name} → ${n.expectCode}`, () => {
      const v = validateTrace(n.trace, dir(n.case));
      const codes = v.map(x => x.code);
      expect(codes, `expected ${n.expectCode}, got ${JSON.stringify(v)}`).toContain(n.expectCode);
    });
  }
});

describe('R2 Validator — Trust-but-Verify: recomputed counters override self-report', () => {
  it('inflated self-report of a fabricated budget still bounded by real actions', () => {
    const t = JSON.parse(JSON.stringify(GOLDENS['RB-X5']));
    // 声称只用 1 scope 却偷偷多 body-read 第二个外部 project 的文件 → 重算应抓到越界
    const extra = { tool: 'Read', mode: 'body', target: 'backend/framework/JNPF/DataEncryption/Encryptions/DESCEncryption.cs', hop: 2, purpose: 'sneak' };
    t.iterations[0].actions.push(extra);
    t.iterations[0].counters_after = { artifact: 1, scope: 1, depth: 1, iteration: 1 }; // 假账
    const codes = validateTrace(t, dir('RB-X5')).map(x => x.code);
    expect(codes).toContain('V-1d'); // 自报与重算不符
  });
});

describe('R2 Validator — schema gate', () => {
  it('rejects malformed trace', () => {
    expect(validateTrace({ schema: 'x' }, dir('RB-01')).map(v => v.code)).toContain('V-0');
    expect(validateTrace(null, dir('RB-01')).map(v => v.code)).toContain('V-0');
  });
});
