import { describe, it, expect } from 'vitest';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { validateTrace } from './trace-validator';

/**
 * R2 36-runs 机械验收层（A-1）：验证 r2/traces/**\/run-*.json 全部 trace。
 * 只读目录，不依赖 golden；traces 为空时输出提示并跳过（不视为失败）。
 */
const TRACES = path.resolve(__dirname, '../../.claude/evidence/skill-evolution-review-20260828/r2/traces');
const SCEN = path.resolve(__dirname, '../../.claude/evidence/skill-evolution-review-20260828/r2/scenarios');

function collect(): Array<{ caseId: string; run: number; file: string }> {
  if (!fs.existsSync(TRACES)) return [];
  const out: Array<{ caseId: string; run: number; file: string }> = [];
  for (const d of fs.readdirSync(TRACES)) {
    const dir = path.join(TRACES, d);
    if (!fs.statSync(dir).isDirectory()) continue;
    for (const f of fs.readdirSync(dir)) {
      const m = /^run-(\d+)\.json$/.exec(f);
      if (m) out.push({ caseId: d, run: Number(m[1]), file: path.join(dir, f) });
    }
  }
  return out.sort((a, b) => (a.caseId + a.run).localeCompare(b.caseId + b.run));
}

describe('R2 live traces — mechanical invariant gate (A-1)', () => {
  const traces = collect();
  if (traces.length === 0) {
    it('no traces yet (36-runs not started) — gate idle', () => { expect(traces).toEqual([]); });
  } else {
    it(`all ${traces.length} traces pass V-0..V-7 with zero violations`, () => {
      const report: string[] = [];
      for (const t of traces) {
        const trace = JSON.parse(fs.readFileSync(t.file, 'utf8'));
        const v = validateTrace(trace, path.join(SCEN, t.caseId));
        const line = `${t.caseId}/run-${t.run}: decision=${trace?.final?.decision ?? '?'} stop=${trace?.final?.stop_triggered ?? '?'}` + (v.length ? ` VIOLATIONS=[${v.map(x => x.code + ' ' + x.msg).join(' | ')}]` : ' CLEAN');
        report.push(line);
        if (v.length) report.push('');
      }
      // eslint-disable-next-line no-console
      console.info('\n=== R2 LIVE TRACE GATE ===\n' + report.join('\n'));
      const failed = traces.filter(t => validateTrace(JSON.parse(fs.readFileSync(t.file, 'utf8')), path.join(SCEN, t.caseId)).length > 0);
      expect(failed.map(f => f.caseId + '/run-' + f.run)).toEqual([]);
    });
  }
});
