/**
 * R2 Context-Acquisition Trace Validator — 冻结 R1 Contract (Patch v2) 的机械复验器
 * 规范源: .claude/evidence/skill-evolution-review-20260828/r2/Level-0-1-Validation.md §3
 * 纪律: 只实现 R1 v2 已有语义, 不新增规则; 计数口径含 A-§4 解释假定 (见 R2-GAP-01)
 */
import * as fs from 'node:fs';
import * as path from 'node:path';

export type Violation = { code: string; msg: string };

const CT_SET = ['Call', 'DI', 'Ownership', 'DataFlow', 'CrossLayer'];
// R1 Patch v2 §1.2 Risk×Nature 分档表 [depth, artifact, iteration, scope] — 权威源, 不得在此改动
const ALLOC: Record<string, Record<string, [number, number, number, number] | 'FORBID'>> = {
  Critical: { Local: [1, 3, 1, 0], Regional: [2, 6, 2, 1], Systemic: [3, 10, 3, 2] },
  High:     { Local: [1, 3, 1, 0], Regional: [2, 6, 2, 1], Systemic: [3, 8, 2, 1] },
  Medium:   { Local: [1, 2, 1, 0], Regional: [2, 4, 1, 1], Systemic: [2, 6, 2, 1] },
  Low:      { Local: [1, 1, 1, 0], Regional: [1, 2, 1, 0], Systemic: 'FORBID' },
};
const NATURE_PREFIX: Record<string, string[]> = {
  Local: ['Local'],
  Regional: ['Local', 'Regional'],
  Systemic: ['Local', 'Regional', 'Systemic'],
};
const STOP_ORDER: Array<['STOP4' | 'STOP5' | 'STOP1' | 'STOP2' | 'STOP3', string]> = [
  ['STOP4', 'STOP-4'], ['STOP5', 'STOP-5'], ['STOP1', 'STOP-1'], ['STOP2', 'STOP-2'], ['STOP3', 'STOP-3'],
];
// V-4: 判停/影响/主张字段禁时间成本话术 (R1 已删成本判停; meta 观测字段除外)
const FORBIDDEN = /(分钟|耗时|小时|太贵|不值得|成本\s*[>＞]|收益\s*[>＞]|\bminutes?\b|\bhours?\b|time spent|\bcost\b|\bbenefit\b)/i;

function norm(s: string): string { return String(s ?? '').replace(/\s+/g, ' ').trim(); }
function readJson(f: string): any { return JSON.parse(fs.readFileSync(f, 'utf8')); }
// v1 harness 修正：接受 baseline/ 前缀与场景绝对路径别名归一到 manifest repo 相对路径（不放宽任何内容校验）
function aliasPath(p: string): string {
  let q = String(p ?? '').replace(/\\/g, '/');
  q = q.replace(/^.*?\/r2\/scenarios\/[^/]+\//, '');
  q = q.replace(/^baseline\//, '');
  return q;
}

export function validateTrace(trace: any, scenarioDir: string): Violation[] {
  const V: Violation[] = [];
  const add = (code: string, msg: string) => V.push({ code, msg });

  // ---------- V-0 schema ----------
  const f = trace?.finding, alloc = trace?.budget_allocation, iters: any[] = trace?.iterations ?? [];
  if (trace?.schema !== 'r2-trace/1' || !f || !alloc || !Array.isArray(iters) || !trace?.final || !trace?.five_tuple) {
    add('V-0', 'missing/invalid top-level fields'); return V;
  }
  if (!['Critical', 'High', 'Medium', 'Low'].includes(f.risk)) add('V-0', 'risk enum');
  if (!['Local', 'Regional', 'Systemic'].includes(f.nature)) add('V-0', 'nature enum');
  if (!Array.isArray(f.nature_order_checked)) add('V-0', 'nature_order_checked');
  if (typeof trace.claim_gate?.fq1 !== 'boolean' || typeof trace.claim_gate?.fq2 !== 'boolean' || typeof trace.claim_gate?.fq3 !== 'boolean') add('V-0', 'claim_gate');
  iters.forEach((it, i) => {
    if (it.round !== i + 1) add('V-0', `round numbering at index ${i}`);
    if (!CT_SET.includes(it.context_type)) add('V-0', `context_type ${it.context_type}`);
    if (!['Level0', 'Level1'].includes(it.level)) add('V-0', `level ${it.level}`);
    if (!it.stop_check) add('V-0', `stop_check missing r${i + 1}`);
    else if (it.stop_check.hit !== null && !['STOP-1', 'STOP-2', 'STOP-3', 'STOP-4', 'STOP-5'].includes(String(it.stop_check.hit))) add('V-0', `hit enum r${i + 1}`);
  });

  // ---------- manifest ----------
  const manifest = readJson(path.join(scenarioDir, 'manifest.json'));
  const byPath: Record<string, string> = {};
  for (const e of manifest.files) byPath[e.path.replace(/\\/g, '/')] = e.project;
  const findingPath = aliasPath(String(f.file));
  const projectOf = (t?: string): string => {
    const p = (t ?? '').replace(/\\/g, '/');
    if (p === findingPath) return f.project;
    return byPath[p] ?? 'UNKNOWN';
  };

  // ---------- V-1b allocation vs frozen table ----------
  const expect = (ALLOC as any)[f.risk]?.[f.nature];
  if (expect === 'FORBID') {
    if (iters.length > 0) add('V-1b', 'Low×Systemic must not Expand');
  } else if (Array.isArray(expect)) {
    const got = [alloc.depth, alloc.artifact, alloc.iteration, alloc.scope].join(',');
    if (got !== expect.join(',')) add('V-1b', `allocation ${got} ≠ table ${expect.join(',')}`);
  }

  // ---------- V-1c nature 判定顺序 (禁跳档) ----------
  const pre = NATURE_PREFIX[f.nature];
  if (pre && f.nature_order_checked.join(',') !== pre.join(',')) add('V-1c', `order ${f.nature_order_checked.join('>')} ≠ ${pre.join('>')}`);

  // ---------- V-1e claim gate 前置 ----------
  const cg = trace.claim_gate;
  if (iters.length > 0 && (!cg || !(cg.fq1 && cg.fq2 && cg.fq3))) add('V-1e', 'iterations started with unfalsifiable claim');

  // ---------- V-1a budget 重算 — 锁定 A-§4 语义（R2-GAP-01 ACCEPTED）----------
  // 定点 grep / symbol lookup / exact file-line = 证据定位：计 Artifact + Depth，**免 Scope**。
  // body read 跨 project = 扩大调查对象集合：计 Artifact + Depth + Scope。
  // broad repository discovery → 触碰多个 manifest 文件 → Artifact 预算线性卡死（禁"grep 不算账就能扫全仓"）。
  // 非 manifest 目标（外部/nuget/未命中）= 取证未得，不产证据也不计账。
  const cumTo = (upto: number) => {
    const A = new Set<string>(); const P = new Set<string>(); let D = 0;
    for (const it of iters.slice(0, upto)) for (const a of it.actions ?? []) {
      const p = aliasPath(String(a.target ?? ''));
      if (!p || p === findingPath) continue;            // finding 文件 = P0 基线，永不计账
      if (!(p in byPath)) continue;                     // 非本场景 manifest 文件 = 未获得取证，不计
      if (!A.has(p)) A.add(p);                          // 任何模式（含定点）→ Artifact
      D = Math.max(D, a.hop ?? 1);                       // 定点也推进 Depth
      if (a.mode === 'body') { const pr = byPath[p]; if (pr !== f.project) P.add(pr); } // 仅 body 跨库 → Scope
    }
    return { artifact: A.size, scope: P.size, depth: D, iteration: upto };
  };
  const used = cumTo(iters.length);
  if (used.artifact > alloc.artifact) add('V-1a', `artifact ${used.artifact}>${alloc.artifact}`);
  if (used.scope > alloc.scope) add('V-1a', `scope ${used.scope}>${alloc.scope}`);
  if (used.depth > alloc.depth) add('V-1a', `depth ${used.depth}>${alloc.depth}`);
  if (used.iteration > alloc.iteration) add('V-1a', `iteration ${used.iteration}>${alloc.iteration}`);
  // V-1d: 自报 counters_after 必须等于重算值 (防捏造 actions 假账)
  for (const it of iters) {
    const c: any = it.counters_after;
    if (c) {
      const exp = cumTo(it.round);
      for (const k of ['artifact', 'scope', 'depth', 'iteration']) {
        if (c[k] !== undefined && c[k] !== exp[k]) add('V-1d', `r${it.round} self-report ${k}=${c[k]} ≠ recomputed ${exp[k]}`);
      }
    }
  }

  // ---------- V-6 stop priority (4→5→1→2→3 首个 true 即 hit) ----------
  for (const it of iters) {
    const sc = it.stop_check;
    let expectHit: string | null = null;
    for (const [k, name] of STOP_ORDER) if (sc[k] === true) { expectHit = name; break; }
    const got = sc.hit ?? null;
    if (String(got ?? 'null') !== String(expectHit ?? 'null')) add('V-6', `r${it.round} hit=${got} ≠ priority-derived ${expectHit}`);
  }

  // ---------- V-3 stable matrix ----------
  const needMatrix = trace.final.stop_triggered === 'STOP-2' || iters.some(it => it.stop_check?.STOP2 === true);
  if (needMatrix) {
    const m: any[] | null = trace.stable_matrix ?? null;
    if (!Array.isArray(m) || m.length !== 5) add('V-3', 'STOP-2 without complete 5-row matrix');
    else {
      if (m.map(r => r.ct).sort().join(',') !== [...CT_SET].sort().join(',')) add('V-3', 'matrix rows ≠ CT set');
      for (const r of m) {
        if (r.obtainable === 'no' && !norm(r.capped_by)) add('V-3', `row ${r.ct} obtainable=no without capped_by`);
        if (r.obtainable === 'yes' && (norm(r.worst_case_if_obtained).length < 10 || !norm(r.decision_after_replay))) add('V-3', `row ${r.ct} missing worst-case simulation`);
        if (r.flips === 'yes') add('V-3', `row ${r.ct} flips=yes contradicts Stability claim`);
      }
    }
  }

  // ---------- V-4 closed doors + forbidden cost/time language ----------
  if (!['GO', 'STOP', 'NEED_EVIDENCE'].includes(trace.final.decision)) add('V-4', `decision "${trace.final.decision}" not in three gates`);
  const scan = [trace.final.stop_reason, trace.five_tuple.impact, trace.five_tuple.claim, f.claim].map(norm).join(' ');
  if (FORBIDDEN.test(scan)) add('V-4', 'cost/time language in decision fields');

  // ---------- V-2 escalation invariants ----------
  const esc = trace.escalation ?? null;
  const stop5 = trace.final.stop_triggered === 'STOP-5';
  if (!!esc !== stop5) add('V-2', `escalation(${!!esc}) vs STOP-5(${stop5}) mismatch`);
  if (stop5) {
    if (trace.final.decision !== 'NEED_EVIDENCE') add('V-2', 'STOP-5 without NEED_EVIDENCE freeze');
    if (!esc || !['E1', 'E2', 'E3', 'E4', 'E5'].includes(esc.escalation_type)) add('V-2', 'STOP-5 without E1-E5 pack');
    else {
      if (esc.finding_decision_record !== 'NEED_EVIDENCE') add('V-2', 'pack finding_decision_record ≠ NEED_EVIDENCE');
      if (!norm(esc.missing_information)) add('V-2', 'pack missing_information empty');
      if (!Array.isArray(esc.candidate_decisions) || esc.candidate_decisions.length < 1) add('V-2', 'pack candidate_decisions empty');
    }
  }
  if (iters.some(it => it.stop_check?.STOP5 === true) && !stop5) add('V-2', 'STOP-5 mid-trace but final not STOP-5');

  // ---------- V-7 terminal consistency ----------
  const last = iters[iters.length - 1];
  if (iters.length > 0 && !last?.stop_check?.hit) add('V-7', 'final without stop in last iteration');
  if (last?.stop_check?.hit && trace.final.stop_triggered !== last.stop_check.hit) add('V-7', `final.stop ${trace.final.stop_triggered} ≠ last hit ${last.stop_check.hit}`);
  if (trace.final.decision !== trace.five_tuple.decision) add('V-7', 'final.decision ≠ five_tuple.decision');
  if (trace.five_tuple.decision === 'ESCALATE') add('V-4', 'ESCALATE used as decision (fourth gate)');

  // ---------- V-5 evidence replay ----------
  for (const it of iters) for (const ev of it.evidence ?? []) {
    if (ev.source === 'human-statement') {
      if (ev.confidence === 'H') { add('V-5', 'human-statement cannot be High (Patch §2.2)'); continue; }
      const cardFile = path.join(scenarioDir, 'human-cards', `${ev.card_id ?? 'HR-01'}.json`);
      if (!fs.existsSync(cardFile) && !manifest.simulated_human) { add('V-5', 'human-statement without card & human unavailable'); continue; }
      if (fs.existsSync(cardFile) && !norm(fs.readFileSync(cardFile, 'utf8')).includes(norm(ev.snippet))) add('V-5', 'human snippet not in card verbatim');
      continue;
    }
    // file:line / tool-output → 必须在 manifest 内且 snippet 是"单行锚点"逐字命中真实源 (R2-V5 Patch)
    const p = aliasPath(String(ev.path ?? ''));
    if (!p || !(p in byPath || p === findingPath)) { add('V-5', `evidence path not in scenario: ${p || '(empty)'}`); continue; }
    const m = /^(\d+)(?:-(\d+))?$/.exec(String(ev.lines ?? ''));
    if (!m) { add('V-5', `evidence lines invalid: ${ev.lines}`); continue; }
    const abs = path.join(scenarioDir, 'baseline', ...p.split('/'));
    if (!fs.existsSync(abs)) { add('V-5', `baseline file absent: ${p}`); continue; }
    const allLines = fs.readFileSync(abs, 'utf8').split(/\r?\n/);
    const a = Number(m[1]), b = Number(m[2] ?? m[1]);
    if (a < 1 || b > allLines.length || a > b) { add('V-5', `line range out of bounds: ${p}:${ev.lines} (file ${allLines.length} lines)`); continue; }
    const raw = String(ev.snippet ?? '');
    if (!norm(raw)) { add('V-5', `empty snippet ${p}:${ev.lines}`); continue; }
    if (raw.indexOf('\n') >= 0) { add('V-5', `snippet must be single-line (no \\n): ${p}:${ev.lines}`); continue; }
    if (norm(raw).length > 80) { add('V-5', `snippet exceeds 80-char anchor bound (${norm(raw).length}): ${p}:${ev.lines}`); continue; }
    // V-5 Evidence Anchor Contract：锚点必须逐字出现在所引单行区间内（重读源文件比对，禁止 paraphrase/编造）
    const rangeNorm = norm(allLines.slice(a - 1, b).join(' '));
    if (!rangeNorm.includes(norm(raw))) add('V-5', `snippet not found at ${p}:${ev.lines} (fabricated evidence)`);
  }

  return V;
}
