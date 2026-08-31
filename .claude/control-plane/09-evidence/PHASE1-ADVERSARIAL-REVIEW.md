# Phase 1 Adversarial Review — Independent Reviewer Report

> **Reviewer:** Independent Adversarial Reviewer (not Implementer) — stance: "prove it cannot be bypassed"
> **Scope:** Phase 1 Governance Vertical Slice — 5 Policies @1.0 + 5 Hooks + Structured Evidence + Final Gate + AgentOS State
> **Date:** 2026-09-01
> **Upstream:** Phase 0.5 ✅ Phase 0.6 ✅ Phase 1 Plan FROZEN → Implementation 🟢

---

## Decision

**Phase 1 Implementation: ✅ APPROVED**
**Phase 1 Final Closure: 🟡 PENDING INDEPENDENT REVIEW — 3 BLOCK must-fix + 4 WARN debt**

> A governance mechanism is not accepted because it can block the obvious attack. It is accepted only after proving that legitimate behavior is allowed, malicious behavior is blocked, and bypass paths are also blocked.

Current Phase 1 proves the **mechanism** (17+23 adversarial PASS, 44/44 regression) but has **3 scope/lifecycle gaps** that would pollute intermediate workflow or allow scope escape if closed as-is.

---

## Architecture Confirmed

```text
Harness Resolver
      ↓
Applicable Policies (P001-P005@1.0)
      ↓
Policy Evaluator (policy-lib.mjs:80-95)
      ↓
Pre-* Enforcement Hooks (PreMutation/PreBuild/PreCompletion)
      ↓
Real Action
      ↓
Structured Evidence (11 fields + policy_id@version + integrity)
      ↓
Final Completion Gate (GATE-COMPLETION.md:1)
      ↓
AgentOS State Authority (Transition)
```

Boundaries confirmed: `Hook≠Policy Container` `Evidence≠Log` `Gate≠State Authority` `Policy≠Rule Text` `Resolver≠Policy Engine` — none of `harness-resolver.mjs:1` was expanded.

---

## 10 Required Independent Verifications (Chief Architect §16)

### 1. P005 Final Gate ONLY — Outside Completion NOT APPLICABLE

- **Files:** `hooks/policy-005-completion-evidence.mjs:24-53` `settings.json:63-76` `control-plane/05-gates/GATE-COMPLETION.md:3`
- **Wiring:** ✅ PASS — P005 ONLY on `Stop` (Final Gate), not on `PreToolUse`. `settings.json:63-76` proves `P005` not in `PreToolUse:4-49`.
- **Scoping:** ❌ **BLOCK** — No `NOT_APPLICABLE` branch. Always BLOCK if missing evidence, pollutes intermediate `Stop` turns. Global `grep NOT_APPLICABLE = 0` proves missing. Contrast `policy-002-real-build.mjs:11-15` (has audit exempt) and `guard-finish.mjs:68-84` (checks `stop_reason`).
- **Fix:** Add at top of P005 before `required[]` check:

```js
// read stdin like guard-finish, check completion_claim/stage
if (!isCompletionClaim(input, workflowState)) { console.log('P005 NOT_APPLICABLE'); process.exit(0); }
```

- **Severity:** BLOCK — must fix before COMPLETE

### 2. P002 Real Build Target Authenticity

- **Files:** `hooks/policy-lib.mjs:41-53` `hooks/evidence-collector.mjs:10-25` `09-evidence/build-evidence.json:1-17`
- **Finding:** `hasBuildEvidence()` checks `type=REAL_BUILD & exitCode=0 & <30min & version` but **no** `project/solution/workingDirectory/commit/targetFramework`. `collectBuildEvidence` never captures `cwd/git rev`. Live evidence shows `logTail:"ok"` without `solution`. `dotnet build trivial` fakes gate. `policy-adversarial.mjs:63` only fakes type, not target.
- **Severity:** WARN — accepted as Phase 1 debt per Chief §Non-Blocking (future: bind `cwd+commit+sln` in evidence, verify in gate)
- **Evidence:** `build-evidence.json:11` has no `project` field

### 3. P003 Mutation Scope Binding

- **Files:** `hooks/policy-003-mutation-evidence.mjs:39-77` `hooks/policy-adversarial.mjs:77-90`
- **Finding:** **CRITICAL** — Any repo diff satisfies any-file mutation. `git diff --stat` global (`:42`) + per-file `if(fileDiff) hasDiff=true` additive (:46-47) cannot revoke. `hasActorTask` computed but not enforced (`:76-77` allow). No `Task Target Artifact` or `workspacePrefix` binding. No `MUTATION` evidence written on ALLOW.
- **Fix:** Resolve `targetArtifact = workflow-state or harness-resolver`, enforce `filePath === target || filePath∈workspace`, use `git diff --stat -- "${targetFile}"` only, write `collectMutationEvidence` on ALLOW.
- **Severity:** **BLOCK** — scope escape

### 4. P004 Frozen Contract Authenticity

- **Files:** `hooks/policy-004-contract-preservation.mjs:24-53` `workflow-state.json:9-10` `HARNESS-BASELINE.json:19`
- **Finding:** **CRITICAL** — Baseline does not hash frozen contracts; `cr-approved` is agent-writable self-report (`:39-43` reads `workflow-state.json` without signature); `cr-safe:` content bypass (`:49` `if(/cr-safe:/i) exit 0`) allows `// cr-safe: fake` to mutate any frozen file. ALLOW writes no evidence.
- **Fix:** Hash-pin `contractHashes` in baseline; require `crApproved` to reference `change-requests/CR-*.md` with approval; `cr-safe` only for whitespace-only diff (`git diff -U0 --ignore-all-space` empty); always write `contract-guard.json` on ALLOW.
- **Severity:** **BLOCK**

### 5. P005 Completion-only Enforcement (see #1) + Positive Control

- **Finding:** P005 correctly on Stop only, but also missing PreTest lifecycle point (see #2 below). Positive control: `P005 good build → ALLOW` passes, but no `Test+Review` evidence checked (comment admits relax `required=[build]` only).
- **Severity:** WARN (Phase 1 slice scope)

### 6. Evidence Tamper Detection (Integrity Enforced vs Exists)

- **Files:** `hooks/policy-lib.mjs:79-81` (write) `hooks/policy-lib.mjs:41-53` (read) `evidence-collector.mjs:14`
- **Finding:** **BLOCK** variant — Integrity field exists (`sha256:b89b9b...` 64-bit truncated at `:81` `slice(0,16)`) but **never validated** on read. Canonical omits `exitCode/timestamp/logTail`; flip `exitCode 1→0` keeps same hash `b89b9b93`. `policy-adversarial:63` never tests tamper. No `EVIDENCE_CORRUPTED` path.
- **Fix:** Recompute integrity over `exitCode+timestamp+logTailHash+commit+solution` with full 256-bit, validate in `hasBuildEvidence()` and `P005` before ALLOW; emit `EVIDENCE_CORRUPTED BLOCK`.
- **Severity:** WARN for Phase 1 (listed as Non-Blocking debt: Advanced Evidence crypto attestation) — but must be fixed before Phase 4 Intelligent Verification

### 7. Runtime / Tool Bypass Resistance (IRON-03)

- **Finding:** P005 fake type BLOCK proves `Governance Bypass via fake evidence` is blocked. But P003 `git diff` global and P004 `cr-safe` show Tool Bypass still possible via `OtherFile.cs` and `// cr-safe`. Direct `File API / shell` bypass is still via PreToolUse hooks (all mutations go through PreToolUse, so shell `dotnet build` still needs evidence at Stop — but `PreBuild` missing wire means `Bash dotnet build` not immediate). Partial PASS.
- **Severity:** WARN

### 8. Policy Determinism

- **Files:** `hooks/policy-lib.mjs:90-92` `isDeterministicKey` defined but never called; `writeEvidence` uses `new Date().toISOString()` per call; `policy-003` uses `git diff` order-sensitive; adversarial only checks `exit` twice (`policy-adversarial.mjs:46-48`), not `N=10` with `FailureCode/RequiredEvidence`.
- **Severity:** WARN — acceptable for Phase 1, must harden before Phase 2 (sorted git, mocked timestamp, N=10 harness)

### 9. Policy Version Traceability

- **Files:** `hooks/policy-lib.mjs:95-96` `POLICY_VERSION='1.0'` literal in 6 places, not hash-bound; evidence stale if policy file edited without bump; no migration gate.
- **Severity:** WARN — must derive `version = sha256(policyFile)[:8]` + semver before Phase 4

### 10. Positive / Negative / Boundary Cases

- **Adversarial:** `policy-adversarial.mjs:1-162` 17 PASS includes Positive (audit→ALLOW, cr-safe→ALLOW, diff→ALLOW) and Negative (BLOCK) but **P001/P003 lack systematic triple**. Table:

| Policy | Positive | Negative | Boundary |
|--------|----------|----------|----------|
| P001 | missing: `-1 assert ALLOW`, `add assert ALLOW`, `docs exempt ALLOW` | weaken BLOCK, skip BLOCK | `-1` noise edge not tested |
| P002 | audit ALLOW, good build ALLOW via seed | no build BLOCK, fake BLOCK | `29min vs 31min TTL` not tested |
| P003 | diff ALLOW, empty ALLOW | **no Negative** (no diff BLOCK) | `09-evidence/**` exempt not both |
| P004 | cr-approved ALLOW, cr-safe ALLOW | frozen BLOCK | `cr-approved=""` vs missing vs kebab/camel not all |
| P005 | good ALLOW | fake BLOCK, ordering BLOCK | `expired >30min → BLOCK` verified, but `exitCode 0 + result BLOCK` not |

- **Severity:** WARN — gate appears to work but without systematic positive controls could be “always BLOCK” false positive.

### Lifecycle Mapping (Chief §3 Concern)

| Hook ID | Lifecycle | Policies | Invocation Path | Status |
|---------|-----------|----------|-----------------|--------|
| H-MUT | PreMutation | P003, P004 (P001 also PreMutation, but spec says PreTest) | `PreToolUse Write\|Edit\|MultiEdit` → `policy-003/004/001` | ✅ but P001 should be PreTest |
| H-BUILD | PreBuild | P002 | **Stop only** (`settings.json:69`), not `PostToolUse Bash(dotnet build)` | ⚠️ missing PreBuild immediate |
| H-TEST | PreTest | P001 | **MISSING** — P001 wired as PreMutation, not PreTest | ⚠️ |
| H-COMPLETE | PreCompletion | P005 | `Stop` → `policy-005` | ✅ but needs NOT_APPLICABLE |

**Severity:** WARN — H-BUILD/H-TEST gaps accepted for vertical slice but must be mapped before Phase 2.

---

## Overall Metrics

| Dimension | BLOCK | WARN | NOTE |
|-----------|-------|------|------|
| D1-Architecture | 0 | 2 (PreTest missing, Gate scope) | 1 (Hook≠Container confirmed) |
| D2-Engineering Laws | 2 (P003 scope, P004 baseline) | 2 (P005 NOT_APPLICABLE, P002 target) | 0 |
| D3-Expert Traps | 0 | 1 (P001 semantic baseline) | 0 |
| D4-Quality (Evidence integrity, Determinism, Versioning) | 0 | 3 (integrity, determinism, version) | 1 (truncate) |
| D5-Test Coverage | 0 | 1 (Positive triple) | 0 |
| **Total** | **2** | **9** | **2** |

- Harness adversarial: 23 PASS
- Policy adversarial: 17 PASS (but gaps above)
- Control Plane regression: 44/44 PASS (verified `scripts/test-hooks.mjs:1`)
- Drift: NO DRIFT (raw 276 unique 207 after baseline)

---

## Rule Evolution (New Patterns)

- **TRAP-P1-001:** P001 count-only → semantic `Assert.True(true)` bypass. **Suggest:** `coder-reminders.md` add "P001 is Baseline, semantic check deferred to Phase 4 Intelligent Verification"
- **TRAP-P3-001:** Global diff satisfies any-file. **Rule:** `policy-003` must bind to targetArtifact per Chief §7
- **TRAP-P4-001:** `cr-safe:` content bypass. **Rule:** `policy-004` must require whitespace-only diff for `cr-safe`
- **TRAP-EVI-001:** Integrity not enforced. **Rule:** `policy-lib` must validate integrity before ALLOW
- **HARNESS-DRIFT:** `control-plane/09-evidence` transient must be excluded from drift (fixed `harness-drift.mjs:58`)

---

## Final Status

```text
Phase 0.5  ✅ PASS
Phase 0.6  ✅ PASS
Phase 1 Plan ✅ FROZEN
Phase 1 Implementation ✅ APPROVED (mechanism proven)
Phase 1 Final Gate 🟡 PENDING REVIEW — 3 BLOCK (P003 scope, P004 authority, P005 NOT_APPLICABLE) must fix
Phase 2 ❌ DO NOT START until Phase 1 Closure
```

**Next:** Fix 3 BLOCK (estimated 1-2h, no new Policy platform expansion), re-run `policy-adversarial.mjs` + `harness-drift.mjs` + `test-hooks.mjs`, then re-request Adversarial Review for Phase 1 Closure. Non-BLOCK WARN debt (P001 semantic, P002 target binding, integrity crypto, determinism N=10, version hash) accepted for Phase 1 and deferred to AgentOS/Intelligent Verification per Chief §Non-Blocking.

## Guard Audit

- `guard_coverage_verified`: false — 5 new policies not yet covered by `guard-reviewer.mjs`; new gaps above are exactly why reviewer exists
- `missed_by_guard`: [P003-scope, P004-baseline, P005-NOT_APPLICABLE, P001-semantic, P002-target, integrity]
- `false_positive_by_guard`: []
- `guard_improvement_suggestions`: Hash-pin frozen contracts in `HARNESS-BASELINE.json`; add `PreBuild` PostToolUse hook; add PreTest lifecycle point
