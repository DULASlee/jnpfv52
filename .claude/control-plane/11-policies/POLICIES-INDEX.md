# Policies Index — Phase 1 Vertical Slice

> **Hook-per-Policy = Phase 1 implementation strategy, NOT permanent architecture.** Hook is Enforcement Point, not Policy Container.
> Future stable Enforcement Points: `PreMutationHook / PreBuildHook / PreTestHook / PreCompletionHook` — Policy Engine evaluates there. `30 Policies ≠ 30 Hooks`.

| Policy | Type | Scope | Enforcement Point | Requires (Gate Structured) | onViolation |
|--------|------|-------|-------------------|----------------------------|-------------|
| P001@1.0 No Fake Green | **Hard** | refactoring, feature, bugfix (README-only exempt) | PreMutationHook (PreToolUse Write/Edit) | evidence.type=P001_ASSERT_INTEGRITY & policy_version=1.0 & result!=BLOCK | **BLOCK exit 2** |
| P002@1.0 Real Build | **Conditional** | refactoring, feature, bugfix (audit→AuditOnly exempt) | PreBuildHook + Stop | evidence.type=REAL_BUILD & exitCode=0 & timestamp<30min & policy_version=1.0 | **BLOCK** |
| P003@1.0 Mutation Evidence | **Hard** | all writes | PreMutationHook (PreToolUse) | Before/After/Diff/Actor/Task (5-tuple) | **BLOCK** |
| P004@1.0 Contract Preservation | **Hard** | frozen contracts | PreMutationHook (frozen) | cr-approved | **BLOCK** |
| P005@1.0 Completion Evidence | **Hard** | all completions | PreCompletionHook — **Final Gate ONLY** | Build+Test+Review+Evidence (4 structured evidences) | **BLOCK** |

**Future:** `PreMutation / PreBuild / PreTest / PreCompletion` are stable lifecycle hooks; policies evaluated there by engine.

**Flow:** `Rule → Machine Policy → Evaluator → Enforcement Hook → Evidence (11-field structured) → Gate → AgentOS State Authority (Transition)`

**Evidence Producer:** `evidence-collector.mjs` — structured, not `log.push`. Evidence must contain `EvidenceType/Actor/Task/Stage/Policy/Action/Before/After/Tool/Result/Timestamp/Integrity + policy_id@version`.

**Determinism:** `same(Task,Phase,Context,Evidence,PolicyVersion) → same decision` — unless Policy explicitly time-dependent.

**Versioning:** Evidence must record `policy_id + policy_version + decision` for future Evidence Replay.

**State Boundary:** Phase 1 owns *decision* (ALLOW/BLOCK), AgentOS owns *state* (e.g., `BUILDING→BUILT`). No `state.js` reinvented.
