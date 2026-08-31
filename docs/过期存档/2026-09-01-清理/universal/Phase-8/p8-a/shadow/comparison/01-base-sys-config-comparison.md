# Comparison: Table 01 — base_sys_config

> **Phase**: 8 — P8-A.4 (Comparison)
> **Status**: COMPLETE
> **Date**: 2026-08-30
> **Protocol**: Adversarial Track B (Blind Review unavailable)

---

## L1: Dimension Comparison

| Dim | AI (Track A) | Adversarial (Track B) | Classification | Notes |
|---|---|---|---|---|
| A Schema | No-Finding | Arithmetic error (16 vs 17); F_ZX_DATATYPE coverage gap | AI FALSE POSITIVE on completeness | Track A asserted 16-col Entity without verification; the [KNOWN] tag is over-rated |
| B Integrity | No-Finding | HG#1 depth shallow (F_TENANT_ID nullable) | SAFE DISAGREEMENT | Both correct in conclusion; reviewer deeper |
| C Index | SAFE-REFACTOR (1 index) | Same + completeness concerns | SAFE DISAGREEMENT | Both agree on recommendation; reviewer adds preconditions |
| D Lifecycle | No-Finding | "Independent boolean" unverified | SAFE DISAGREEMENT | Minor depth difference |
| E CRUD/Query | No-Finding | List-by-tenant not analyzed | AI FALSE NEGATIVE | Track A missed the actual hot path |
| F DDD | No-Finding (Singleton aggregate) | Terminology imprecise | SAFE DISAGREEMENT | Reviewer catches vocabulary issue |
| G Consumer/Target | SAFE-REFACTOR | "likely related" = GUESS not INFERRED | AI FALSE POSITIVE | Tag inflation identified |

**L1 Summary**: 7 dimensions assessed; 0 agreements; 4 SAFE DISAGREEMENTS; 2 AI FP; 1 AI FN.

---

## L2: Risk Comparison

| AI Risk | Adversarial Risk | Classification | Tier Diff |
|---|---|---|---|
| R0/R1 (HIGH confidence) | R0/R1 (MEDIUM confidence) | SAFE DISAGREEMENT | 0 (same tier, different confidence) |

**Note**: Both classify as R0/R1. Disagreement is on confidence level, not risk tier.

---

## L3: Hard Gate Comparison

| HG | AI | Adversarial | Classification | If GATE ERROR: FN or FP |
|---|---|---|---|---|
| HG#1 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#2 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#3 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#4 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#5 | NOT triggered | NOT triggered | AGREEMENT | — |

**L3 Summary**: 5 HGs; 5 AGREEMENT; 0 GATE ERROR.

---

## L4: Action Comparison

| AI Action | Adversarial Action | Classification |
|---|---|---|
| SAFE-REFACTOR (1 index) | SAFE-REFACTOR (1 index + 4 preconditions) | SAFE DISAGREEMENT |

Both agree on direction. Reviewer adds operational preconditions.

---

## L5: Closure Comparison

| AI Closure | Adversarial Closure | Classification |
|---|---|---|
| NO-CHANGE | NO-CHANGE (conditional on preconditions) | AGREEMENT (with caveats) |

Both recommend NO-CHANGE. Reviewer's conditions are operational tracking, not closure change.

---

## Aggregate Per-Table

- Total dimensions assessed: 7
- Agreements: 2 (HG#1-5 not 5 since this is one row, but HG row agreements are tracked separately)

**Re-statement**: 
- L1 Dimension Agreements: 0 (no dimension-level agreement)
- L1 SAFE DISAGREEMENTS: 4
- L1 AI FP: 2
- L1 AI FN: 1
- L2 Risk: SAFE DISAGREEMENT (confidence diff)
- L3 HG: 5 AGREEMENT
- L4 Action: SAFE DISAGREEMENT
- L5 Closure: AGREEMENT (with caveats)

**Hard Safety Metrics (this table)**:
- Hard Gate FN (this table): 0
- P0/P1 decision error (this table): 0
- Core contamination (this table): 0
- TABLE CLOSED decision error (this table): 0

---

## Adversarial Findings Routing

| Finding | Route to |
|---|---|
| Schema arithmetic (16 vs 17) | Skill Evolution (Level A) — finding precision |
| F_ZX_DATATYPE coverage gap | Skill Evolution (Level A) — column enumeration completeness |
| Tenant isolation depth | Skill Evolution (Level A) — evidence tag discipline |
| Index recommendation without query evidence | Skill Evolution (Level A) — recommendation requires evidence |
| F_CATEGORY query path missing | Skill Evolution (Level A) |
| "Singleton aggregate" terminology | Master Spec Evolution (DDD vocabulary) |
| F_ZX_SYSTEM_ID GUESS not INFERRED | Skill Evolution (Level A) — tag calibration |
| Confidence over-rated | Skill Evolution (Level A) — confidence calibration |

**8 findings routed to Skill Evolution (Level A)** — calibration concerns, not safety failures.
