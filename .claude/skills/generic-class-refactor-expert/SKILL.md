---
name: generic-class-refactor-expert
description: Use when diagnosing or refactoring any .NET class for lifetime/memory/async/concurrency/exception/performance/type/extensibility/observability/architecture risks, or when an AI agent must decide whether a class-level optimization is justified
---

# Generic Class Refactoring Expert — Evidence-Driven (v4.0)

> **Core principle**：Any advanced technique must pass Evidence → Root Cause → Minimal Solution → Risk → Verification → Benefit loop. No technique for its own sake.

## When to Use

- Need to judge if a class is God-class, leaky, contended, sync-over-async, or over-engineered
- Considering Span/ArrayPool/ValueTask/ObjectPool/SourceGenerator/Expression/SIMD/WeakEvent/ConditionalWeakTable/Strategy/Record
- Need Risk / Impact decision (fix now vs later vs never)
- Must produce P0 evidence, risk matrix, benchmark, and regression report
- JNPF or plain ASP.NET Core / generic .NET

**Do NOT use** when task is pure feature coding, simple bugfix, or already covered by cleaner domain plan. For those, keep existing flow.

## Hard Gates (constitution)

1. **P0 missing → P1..P10 blocked.** No P0 Evidence Pack = no business code write.
2. **Performance Gate 7Q missing → advanced optimization blocked.** See `references/Performance-Change-Gate-checklist.md`.
3. **Finding ≠ Fix.** Finding alone does not auto-modify code; needs Decision.
4. **Three safety valves** (must answer No):
   - No evidence yet did advanced optimization? Must be No.
   - No benchmark yet claimed performance gain? Must be No.
   - Finding auto-fixed code? Must be No.

## Execution Protocol (one class, one dimension per chat)

```
Target class → Baseline snapshot → P0 5-dim evidence (code/runtime/arch/test/risk)
  → Findings (16 dims, deduped) → Risk/Impact Matrix → Gate Decision (go / no-go / defer)
  → Minimal solution (lowest complexity that works) → Test/Coverage → Benchmark if perf → Arch tests → Observability → Regression → Final Report
```

- **Granularity**: One Chat = one demonstrable result (e.g., one class × one dimension). No bulk.
- **Branch**: `docs/AI原生开发/1、多用户多任务并行/1、阶段A.md · 2、阶段B.md · 3、阶段C.md` is still the only construction basis for product code; this skill governs **class-level judgment**, not product flow.
- **Rollback**: Characterization red → `git revert`; Benchmark not meeting threshold → revert solution.
- **Irreversible** (delete class / change public signature / change transaction boundary) → STOP for human.

## P0 — Evidence & Risk (must come first)

- **P0.1 Code facts**: size/methods/fields/CC, deps, cycles, DI lifetime, static mutable state, callers. Tool: Roslyn + `arch-module-dependency-scan.ps1` + scan list I/L/H + Serena/CodeGraph.
- **P0.2 Runtime facts** (≥2 required before any perf work): CPU/Memory/Allocation, GC Gen2, ThreadPool, latency P95/P99, exceptions, DB slow/N+1. Tool: dotnet-counters/trace/gcmon + BDN/k6.
- **P0.3 Arch facts**: direction (`.Interfaces → .Entitys → Service`), cycles, lifetime vs boundary.
- **P0.4 Test facts**: characterization ≥30, unit≥3 per aggregate, BDN baseline if perf.
- **P0.5 Risk**: Critical/High/Medium/Low. JNPF N1/N2/N3/N4 = Critical immediate.

Template: `references/P0-Evidence-Pack-template.md`
Risk: `references/Risk-Matrix-template.md`

## Toolkit Corrections (v3.0 → v4.0)

| Area | v3.0 pitfall | v4.0 rule |
|------|--------------|-----------|
| WeakEvent | default for all events | Only when publisher outlives subscriber and cannot unsubscribe naturally; prefer explicit Dispose/Unsubscribe |
| ConditionalWeakTable | for string TenantId | Business cache = IMemoryCache/IDistributedCache/Redis; CWT only for GC-bound object identity |
| HttpClient | DisposeAsync demo | Use IHttpClientFactory / Typed Client; six-tuple audit: ownership+DI+Factory+Dispose+CT+background |
| Async | ".Result deadlocks, GetAwaiter fixes" | Essence = Sync-over-Async → ThreadPool starvation; path: async? → full async; else isolate + ADR |
| ValueTask | Task replacement | Default Task; only if high-freq + many sync completions + BDN proven |
| Concurrency | ConcurrentDict = safe | Check Atomicity, use GetOrAdd/AddOrUpdate |
| Exceptions | dozens of types, Try for all | Three layers + ErrorCode + expected vs exceptional; Try only for normal branch |
| Pooling | perf only | Ownership (P1) + return responsibility; never return-then-Return |
| Record | for all | Type Semantic Fit: Entity=class, DTO/VO=record |
| Strategy | for 2 branches | Complexity Budget: if→map→Strategy→Factory→Plugin, stepwise |
| Observability | tag anything | Trace+Metrics+Logs+Privacy+Cardinality+Cost; no PII/high-cardinality, sample, tenant ctx required |

## Evidence → Modify / Stop / Need-Evidence Gates (v4.0 calibrated)

- **Allow Modify** (6 conjunctive): Finding evidenced ∧ Contract violation ∧ single-point boundary ∧ gates pass ∧ regression path exists ∧ no Contract expansion. See `references/Evidence-to-Modify-Gate.md`.
- **Must Stop** (10 disjunctive — sufficient evidence to decide NOT to do): guess without evidence / capability not Contract / test gap only / not a defect / needs Contract expansion / needs new arch / needs perf without evidence / cannot keep single-point / cross-module contagion / cannot regress. See `references/Evidence-to-Stop-Gate.md`. **STOP = Evidence proves should NOT do now.**
- **NEED EVIDENCE** (distinct — insufficient evidence to decide): Finding may be real (e.g., N+1 shape) but runtime/benefit proof missing and env blocked, cannot claim GO nor STOP. Must freeze as `NEED EVIDENCE / BLOCKED` and be re-decided only after evidence补充. 禁止：`NEED EVIDENCE → pressure → GO`，也禁止无重决策直接转为 STOP. Principle: `unknown → Stop, not guess`; `insufficient → Need Evidence, not guess`.

## Golden Examples (v4.0 基线)

```
v4.0 类级专家重构 Skill
│
├── Exception Semantics
│   └── Golden #1 — Preserve Cause (e45f724a)
│
├── Resource Lifetime
    ├── Golden #2 — UploadFileByType `using var` (d6117dce)
    └── Golden #3 — FileDown `using var` (acc6f5d0)
│
└── Business Transaction
    └── Golden #4 — UnitOfWork Boundary (339689af, +3 lines, Runtime=DEFERRED)
```

- `references/Golden-Example-01.md` — Exception Semantics / Preserve Cause (`e45f724a`, EmailService Delete, 2+2 lines). Frozen decision pattern: multiple Findings → keep one → gate → single-point → behavior preserved → InnerException → regression.
- `references/Golden-Example-02.md` — Resource Lifetime / UploadFileByType `using var` (`d6117dce`, FileService UploadFileByType, 1+1 lines). Upload path resource lifecycle.
- `references/Golden-Example-03.md` — Resource Lifetime / FileDown `using var` (`acc6f5d0`, FileService FileDown, 3+4 lines). Download path resource lifecycle. #2 与 #3 属于同一技术性质的**异场景双样本**，证明 Skill 在 Resource Lifetime 原则下的可重复性.
- `references/Golden-Example-04.md` — Business Transaction / UnitOfWork Boundary (`339689af`, OrderService Save/Delete `+1 using+2 [UnitOfWork]`, 6要素全满足). 价值：异质技术性质（事务边界）。标记：`Implementation/Build/Code-semantics=VERIFIED, Runtime rollback=DEFERRED / ENVIRONMENT BLOCKED — no claim`.

**注意**：不要为了凑 Golden Example 数量而连续修同类 Finding。下一步应回到"类级剩余风险盘点/选择"，从剩余 Stop Findings 中按证据、风险、收益和改造半径重新选择最值得推进的一个。

## Performance Change Gate

See `references/Performance-Change-Gate-checklist.md` — 7 questions + ValueTask/Pool bans.

## Fix Budget — Semantic Change Budget (M1 calibrated)

Budget is `Semantic Scope + Physical Diff + Dependency Expansion`, not line-count alone (proven by OrderService `+1 using+2 [UnitOfWork]` CS0246 case):
- **Semantic Scope**: what semantics may change (DB consistency vs external side-effect vs API Contract)
- **Physical Diff**: files/lines changed (prefer +3 minimal over FullyQualified workaround)
- **Dependency Expansion**: new cross-class/cross-layer deps introduced?
A `using` import needed for compilation is semantic-budget-neutral if no new dependency; must still be explicitly approved. See `references/Complexity-Budget-scale.md`.

## Class-Level Convergence Rule (M3 calibrated)

Not every Finding should be fixed. Declare `Class-level convergence` when:
- remaining Findings risk/benefit diminishing, or require cross-class/arch boundary, or evidence insufficient, or local fix risk rising
Then **actively converge** the class (no more local Finding dig). This is `Expert Refactoring Completion Criterion`, not laziness. See `references/Evidence-to-Stop-Gate.md` STOP vs NEED EVIDENCE.

## Observability Boundary

See spec v4.0 §4.10 — no PII, no high-cardinality, tenant context required.

## Deliverables per class

- `P0-Evidence-Pack.md` (gate)
- GC/Async/Concurrency/BDN reports (if touched)
- Risk matrix
- Arch tests pass, characterization green
- Final Report with Evidence → Finding → Risk → Decision chain, no business code change if Phase 0 pilot

## References

- Spec: `docs/superpowers/specs/通用类级重构专家Skill规格-v4.0.md`
- Plan: `docs/superpowers/plans/通用类级重构专家Skill实施计划-v4.0.md`
- Scan list: `docs/superpowers/specs/JNPF后端类级代码审计扫描清单-v1.1.md` (16 dims ×79 rules, N/O = JNPF iron)
- L2 SOP: `docs/superpowers/plans/L2-类级螺旋专家级重构方案-v2.0.md`
- L1 input: `docs/superpowers/plans/L1-表级螺旋执行手册-v1.0.md`

## Boss Report (≤10 lines, no class/method names in body)

```
【状态】已修好/卡住/等你批
【人话】发生了什么+现在怎样+你点头后会怎样（各一句）
【你怎么验】命令一行+产物路径
【要你做】继续/通过/打回/重开（单选）
```


