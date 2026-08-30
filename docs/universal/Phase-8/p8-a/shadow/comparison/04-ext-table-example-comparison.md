# Comparison: Table 04 — ext_table_example

> **Phase**: 8 — P8-A.4 (Comparison)
> **Status**: COMPLETE
> **Date**: 2026-08-30

---

## L1: Dimension Comparison

| Dim | AI (Track A) | Adversarial (Track B) | Classification | Notes |
|---|---|---|---|---|
| A Schema | No-Finding (standard JNPF) | decimal(9) precision insufficient; 28-col coverage gap; "standard" circular | AI FALSE POSITIVE | |
| B Integrity | No-Finding | No-FK + sample table ambiguity | SAFE DISAGREEMENT | |
| C Index | SAFE-REFACTOR (1 index) | 3 of 4 query patterns without index; LIKE search won't use index | AI FALSE POSITIVE | Pattern-Recommendation disconnect (same as Table 3) |
| D Lifecycle | No-Finding | No state machine for project management | AI FALSE NEGATIVE | |
| E CRUD/Query | No-Finding | "Standard CRUD" vague; "No N+1" trivially true | SAFE DISAGREEMENT | |
| F DDD | No-Finding (self-contained aggregate) | "Example" suffix = template, not reference | SAFE DISAGREEMENT | Methodological concern |
| G Consumer/Target | No-Finding (no special mapping) | JSON-blobs need mapping; decimal precision needs specification | AI FALSE NEGATIVE | |

**L1 Summary**: 7 dimensions; 0 AGREEMENT; 3 SAFE DISAGREEMENT; 2 AI FP; 2 AI FN.

---

## L2: Risk Comparison

| AI Risk | Adversarial Risk | Classification | Tier Diff |
|---|---|---|---|
| R2 (HIGH confidence) | R2 (HIGH confidence) | AGREEMENT | 0 |

**Note**: Both classify R2 with HIGH confidence. Risk classification is not the issue.

---

## L3: Hard Gate Comparison

| HG | AI | Adversarial | Classification | If GATE ERROR: FN or FP |
|---|---|---|---|---|
| HG#1 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#2 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#3 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#4 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#5 | NOT triggered | BORDERLINE | SAFE DISAGREEMENT | Reviewer flags decimal(9); AI says not triggered. Both valid positions. |

**L3 Summary**: 5 HGs; 4 AGREEMENT; 1 SAFE DISAGREEMENT; 0 GATE ERROR.

---

## L4: Action Comparison

| AI Action | Adversarial Action | Classification |
|---|---|---|
| SAFE-REFACTOR (1 index) | SAFE-REFACTOR (3 indexes + decimal docs) | SAFE DISAGREEMENT |

Both SAFE-REFACTOR. Reviewer adds documentation.

---

## L5: Closure Comparison

| AI Closure | Adversarial Closure | Classification |
|---|---|---|
| NO-CHANGE | NO-CHANGE (conditional) | AGREEMENT (with caveats) |

Both NO-CHANGE.

---

## Aggregate Per-Table

- L1 Agreements: 0
- L1 SAFE DISAGREEMENTS: 3
- L1 AI FP: 2
- L1 AI FN: 2
- L2: AGREEMENT
- L3: 0 GATE ERROR
- L4: SAFE DISAGREEMENT
- L5: AGREEMENT

**Hard Safety Metrics (this table)**:
- Hard Gate FN (this table): 0
- P0/P1 decision error (this table): 0
- Core contamination (this table): 0
- TABLE CLOSED decision error (this table): 0

**This is the cleanest comparison** — both reviewers agree on Risk, Closure, and most HGs.

---

## Critical Finding: decimal(9) Precision

**Track A said**: "Decimal(9) for amounts — appropriate precision"

**Reality check**:
- decimal(9,2) caps at 9,999,999.99 (~10M)
- decimal(9,4) caps at 99,999.9999 (~100K)
- Enterprise project costs can easily exceed 10M

**This is a real schema concern** that Track A marked as "appropriate" without verification.

**Severity**: Real but isolated. Does not escalate risk.

**Route**: JNPF Extension — financial precision documentation.

---

## Methodological Finding: "Example" Suffix

**Track A used ext_table_example as the "baseline for what JNPF-standard looks like"**.

**Issue**: The "Example" suffix strongly suggests this is a SAMPLE/TEMPLATE table, not a production reference.

**Impact on Skill calibration**: If the Skill uses this table as a reference pattern, it's learning from a template, not a real production table.

**Route**: Skill Evolution (Level A) — calibration baseline should use production tables, not templates.

---

## Adversarial Findings Routing

| Finding | Route to | Severity |
|---|---|---|
| "Standard JNPF CLDS pattern" circular | Skill Evolution (Level A) | Low |
| **decimal(9) precision insufficient** | **JNPF Extension** | **Medium** |
| 28-col coverage gap | Skill Evolution (Level A) | Low |
| 3 of 4 query patterns without index | Skill Evolution (Level A) | Medium |
| LIKE search won't use b-tree index | Skill Evolution (Level A) | Medium |
| F_CUSTOMER_NAME index overkill for 28 rows | Skill Evolution (Level A) | Low |
| Lifecycle state machine missing | Skill Evolution (Level B) | Medium |
| "Standard CRUD" / "No N+1" vacuous | Skill Evolution (Level A) | Low |
| **"Example" suffix = template, not baseline** | **Skill Evolution (Level A)** | **Medium** |
| JSON-blobs need Foundry mapping | Skill Evolution (Level A) | Medium |
| Decimal precision needs specification | JNPF Extension | Low |
| HG#5 borderline on decimal(9) | Skill Evolution (Level A) | Low |

**Critical findings**:
1. decimal(9) precision (JNPF Extension — financial)
2. "Example" suffix as baseline (Skill Level A — calibration)
