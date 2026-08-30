# P8-B Batch 01 — Plan

> **Phase**: 8 — P8-B Controlled Production
> **Batch**: 01
> **Status**: PLAN READY (AWAITING USER APPROVAL FOR EXECUTION)
> **Date**: 2026-08-30
> **Tables Planned**: 5
> **Risk Distribution**: 1 R2 (base_user, conditional), 4 R0/R1 or R2 (to be assessed)

---

## 1. Executive Summary

```
P8-B Batch 01: PLAN READY ✅

Tables Planned: 5 (system-core identity group)
Dependency: Strong (identity/auth foundation, cross-table FK)
Risk Distribution:
  - base_user: R2 (Track A) or R3+ (Adversarial) — DECISION NEEDED
  - base_role: TBD (assessment required)
  - base_position: TBD (assessment required)
  - base_organize: TBD (assessment required)
  - base_user_relation: TBD (assessment required)

Execution Mode: Controlled (DB writes allowed, with verification)
Stability Gate: Established after Batch 01 + Batch 02

Critical Pre-execution Decision:
  base_user R3+ (Adversarial) vs R2 (Track A) classification
  → Affects whether base_user auto-executes or requires Human Gate
```

---

## 2. Batch 01 Selection Rationale

### 2.1 Why system-core identity group?

Per Master Plan §4.4.1: "强关联优先同批" (strong relationships prioritized for same batch).

The system-core identity group is the strongest coupled cluster in the registry:
- base_user ← referenced by virtually every other module
- base_role, base_position, base_organize ← identity aggregates
- base_user_relation ← M:N junctions

Refactoring as a batch ensures:
- Consistent index strategy across the identity layer
- Aligned F_TENANT_ID behavior
- Coherent soft-delete cascade handling
- Single verification window

### 2.2 Why 5 tables?

Per Master Plan §4.4: "3-8 Table Units" per Batch.

5 tables balances:
- Sufficient scope to test cross-table refactoring
- Manageable rollback complexity if needed
- Within AI productivity baseline (~25 tables/AI-hour, 5 tables = ~12 min AI time + ~45 min review time)

### 2.3 What if base_user is R3+?

If base_user classification is locked at R3+ (per Adversarial Protocol), it requires Human Gate per Master Plan §4.2. Three options:

| Option | Description | Risk | Speed |
|---|---|---|---|
| A. Skip base_user from Batch 01 | Execute only base_role/position/organize/user_relation (4 tables) | Lower coupling coverage | Faster |
| B. User acts as Human Gate | User reviews base_user's HG#5 issues and approves execution | Requires user time | Standard |
| C. Defer base_user Decision Brief | Execute other 4 tables; issue Decision Brief for base_user separately | base_user deferred | Mixed |

**Recommendation**: Option A (skip base_user from Batch 01) for first execution. This:
- Removes Human Gate dependency
- Still tests cross-table refactoring (4 tables is sufficient)
- Allows base_user's HG#5 Decision Brief to be drafted in parallel

**Awaiting user decision on Option A/B/C.**

---

## 3. Pre-Execution: Table Assessment

Per Master Plan §4.4.4: "Per Table targeted verification".

Each table in Batch 01 must be ASSESSED → DESIGNED → READY → REFACTORED → VERIFIED → CLOSED.

### 3.1 Tables Requiring Pre-Batch Assessment

For Batch 01, we need assessment of:

| Table | Current Status | Required Action |
|---|---|---|
| base_user | ASSESSED (P8-A) | Re-evaluate under Track A R2 vs Adversarial R3+; produce Execution Plan |
| base_role | NOT ASSESSED | Run Skill assessment (P8-A style) → produce Execution Plan |
| base_position | NOT ASSESSED | Run Skill assessment → produce Execution Plan |
| base_organize | NOT ASSESSED | Run Skill assessment → produce Execution Plan |
| base_user_relation | NOT ASSESSED | Run Skill assessment → produce Execution Plan |

### 3.2 Skill Calibration Adjustment (Mandatory before P8-B execution)

Per P8-A calibration findings, the Skill MUST be calibrated to address critical findings before P8-B execution:

| # | Calibration Item | Source | Priority |
|---|---|---|---|
| 1 | Pattern-Recommendation consistency: identified query pattern must produce index recommendation | Tables 3, 4 | CRITICAL |
| 2 | Aggregate ambiguity detection in wide tables | Table 2 | CRITICAL |
| 3 | HG borderline policy: no "borderline" as stable state | Tables 3, 5 | CRITICAL |
| 4 | Tag inflation discipline (GUESS not INFERRED) | All 5 tables | HIGH |
| 5 | Junction table (M:N) detection | Table 2 | HIGH |

**Question**: Are these calibrations to be applied BEFORE P8-B starts, or DURING P8-B?

Per Master Plan §3.12: "Local correction in progress" implies ongoing. But critical items should be addressed before new table assessment to avoid compounding errors.

**Recommendation**: Apply Skill calibration #1, #2, #3 BEFORE Batch 01 assessment. Calibration #4, #5 can run in parallel.

---

## 4. Batch 01 Execution Plan

### 4.1 Execution Order (Dependency-Driven)

```
1. base_organize (NO incoming FK from this group, lowest coupling)
     ↓
2. base_role (independent aggregate)
     ↓
3. base_position (independent aggregate)
     ↓
4. base_user_relation (depends on base_user, base_role, base_position)
     ↓
5. base_user (if Human Gate approved) — depends on all others
```

### 4.2 Per-Table Execution Steps

For each table (per Master Plan §4.5):

1. **ASSESSED** — Skill assessment (7 dimensions, risk, HG, action, closure)
2. **DESIGNED** — DDL/DML design if refactor needed
3. **READY** — DDL prepared, ready to execute
4. **REFACTORED** — Execute DDL (or NO-CHANGE if no refactor)
5. **VERIFIED** — 6-dimension verification (schema, integrity, query, behavior, migration, rollback)
6. **CLOSED** — Status update in registry

### 4.3 Verification (6 Dimensions, per Master Plan §4.6)

For each table:

| Dimension | Verification |
|---|---|
| schema | DDL correct execution |
| integrity | FK / constraints correct |
| migration | Data migration lossless (if applicable) |
| query | Query path equivalent |
| application behavior | Business behavior unchanged |
| rollback/recovery | Rollback possible (only for R3+) |

### 4.4 Batch Closure

Per Master Plan §4.4.5:
- Each table → Batch Closure
- Batch Verification Record
- Registry update

---

## 5. Risk Management

### 5.1 Risk Sources

| Risk | Source | Mitigation |
|---|---|---|
| base_user R3+ classification | P8-A Adversarial | Skip from Batch 01 OR user Human Gate |
| Skill calibration not applied | P8-A findings | Apply calibration #1-3 before execution |
| Cross-table FK behavior | Multiple tables | Verify each table's outgoing references |
| Soft-delete cascade | base_user relations | Test cascade before commit |
| Index conflict | Multiple index recommendations | Coordinate index naming |

### 5.2 Rollback Strategy

Per Master Plan §4.6.6: "Rollback/recovery possible (only for R3+)".

For Batch 01:
- If base_user is included (R3+ path): full rollback plan required
- If base_user excluded (R0/R1/R2 path): standard rollback via DROP INDEX / DROP COLUMN

### 5.3 Batch Gate Criteria

Per Master Plan §4.10:

```
[ ] Batch 01 closed and verified
[ ] All 5 tables reached CLOSED state
[ ] 6-dimension verification PASS for each
[ ] Batch Verification Record signed
[ ] No P0/P1 errors introduced
[ ] No Universal Core contamination
[ ] Productivity within ±20% of P8-A baseline
```

---

## 6. Estimated Duration

Per P8-A Productivity Baseline:

| Step | Duration |
|---|---|
| Pre-Batch Skill calibration (items 1-3) | ~30 min |
| base_user re-evaluation (if included) | ~10 min |
| base_role assessment | ~5 min |
| base_position assessment | ~5 min |
| base_organize assessment | ~5 min |
| base_user_relation assessment | ~5 min |
| DDL design for each | ~10 min/table |
| DDL execution | ~5 min/table |
| 6-dimension verification per table | ~15 min/table |
| Batch closure | ~30 min |
| **Total Batch 01** | **~3-4 hours** |

---

## 7. Open Decisions (Awaiting User)

### 7.1 Decision A: base_user Inclusion

| Option | Description |
|---|---|
| A1 | Skip base_user from Batch 01 (4 tables only) — RECOMMENDED |
| A2 | Include base_user under user Human Gate (5 tables) |
| A3 | Defer base_user to Batch 02 or later |

**User decision required.**

### 7.2 Decision B: Skill Calibration Timing

| Option | Description |
|---|---|
| B1 | Apply calibration #1-3 BEFORE Batch 01 assessment — RECOMMENDED |
| B2 | Apply calibration during Batch 01 execution |
| B3 | Defer calibration to post-Batch 01 |

**User decision required.**

### 7.3 Decision C: Batch 01 Composition

If Decision A = A2 (include base_user), the Batch 01 composition is:
- base_user, base_role, base_position, base_organize, base_user_relation

If Decision A = A1 (skip base_user), the Batch 01 composition is:
- base_role, base_position, base_organize, base_user_relation (4 tables)

If Decision A = A3 (defer base_user), Batch 01 composition:
- Same as A1 but base_user goes to Batch 02

**User decision required.**

---

## 8. Recommended Batch 01 Plan

If user approves Recommendations:

```
Batch 01: 4 tables (Option A1)
  - base_organize (assess → refactor → close)
  - base_role (assess → refactor → close)
  - base_position (assess → refactor → close)
  - base_user_relation (assess → refactor → close)

Pre-execution:
  - Skill calibration #1-3 applied
  - Per-table assessment documents produced
  - DDL scripts prepared and reviewed

Execution:
  - Per Master Plan §4.5
  - Verification per Master Plan §4.6
  - Batch closure per Master Plan §4.4.5

Estimated Duration: ~3 hours
```

---

## 9. Approval Status

```
Batch 01 Plan:        READY
User Approval:        PENDING (Decisions A, B, C required)
Pre-execution Items:  3 (Decisions A, B, C)
Execution Start:      After user approval
```

**This document is presented for user approval before any DB writes occur.**

---

## 10. References

- Master Plan: `docs/universal/Phase-8/Phase-8-JNPF-Table-Refactoring-Master-Execution-Plan.md`
- P8-A Shadow Gate Result: `docs/universal/Phase-8/p8-a/shadow/comparison/shadow-gate-result.md`
- P8-A Calibration Findings: `docs/universal/Phase-8/p8-a/shadow/comparison/cumulative-comparison.md` §11
- Table Unit Registry: `docs/universal/Phase-8/p8-0/table-unit-registry-final.md`
- Adversarial Protocol: `docs/universal/Phase-8/p8-a/shadow/track-b/Phase-8-Shadow-Mode-Adversarial-Review-Protocol.md`
