# Comparison: Table 05 — sa_data_dictionary

> **Phase**: 8 — P8-A.4 (Comparison)
> **Status**: COMPLETE — **CRITICAL DIVERGENCES (HG borderline dodge pattern)**
> **Date**: 2026-08-30

---

## L1: Dimension Comparison

| Dim | AI (Track A) | Adversarial (Track B) | Classification | Notes |
|---|---|---|---|---|
| A Schema | SAFE-REFACTOR (CRITICAL) | "SA purpose" circular; BIGINT IDENTITY bottleneck; bit NULL handling | SAFE DISAGREEMENT | Reviewer adds depth |
| B Integrity | No-Finding | ON DELETE behavior for 5 FKs unaddressed; index != constraint | AI FALSE NEGATIVE | Track A under-analyzed |
| C Index | No-Finding (EXCELLENT, no recommendations) | Index naming inconsistent; missing indexes (validation_status, created_at) | AI FALSE POSITIVE | Over-confidence |
| D Lifecycle | SAFE-REFACTOR (SCD Type 2) | SCD Type 2 code verification; version increment policy undefined | AI FALSE POSITIVE | Tag inflation |
| E CRUD/Query | SAFE-REFACTOR | WRITE patterns unaddressed; pattern_tags format undefined | AI FALSE NEGATIVE | |
| F DDD | SAFE-REFACTOR (shared projection) | Write contention for projection; 5 dependent tables | SAFE DISAGREEMENT | Reviewer adds depth |
| G Consumer/Target | DEFERRED | HG#5 should be TRIGGERED, not borderline | **AI FALSE NEGATIVE (CRITICAL)** | **HG borderline dodge** |

**L1 Summary**: 7 dimensions; 0 AGREEMENT; 2 SAFE DISAGREEMENT; 2 AI FP; 3 AI FN.

---

## L2: Risk Comparison

| AI Risk | Adversarial Risk | Classification | Tier Diff |
|---|---|---|---|
| R3+ (HIGH confidence) | R3+ (HIGH confidence) | AGREEMENT | 0 |

**Note**: Both classify R3+. This is Track A's strongest call.

---

## L3: Hard Gate Comparison

| HG | AI | Adversarial | Classification | If GATE ERROR: FN or FP |
|---|---|---|---|---|
| HG#1 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#2 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#3 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#4 | BORDERLINE | **TRIGGERED** | **GATE ERROR (FN)** | **AI flagged borderline; reviewer says triggered** |
| HG#5 | BORDERLINE | **TRIGGERED** | **GATE ERROR (FN)** | **AI flagged borderline; reviewer says triggered** |

**L3 Summary**: 5 HGs; 3 AGREEMENT; 0 SAFE DISAGREEMENT; 2 GATE ERROR (FN on HG#4 and HG#5).

**This is the HG borderline dodge pattern made most explicit**.

---

## L4: Action Comparison

| AI Action | Adversarial Action | Classification |
|---|---|---|
| DEFERRED — HG#5 borderline | DEFERRED — HG#5 + HG#4 triggered, with REQUIRED Decision Brief | SAFE DISAGREEMENT |

Both DEFERRED. Reviewer adds required deliverables.

---

## L5: Closure Comparison

| AI Closure | Adversarial Closure | Classification |
|---|---|---|
| DEFERRED | DEFERRED — STRICT (with deadline) | SAFE DISAGREEMENT |

Both DEFERRED. Reviewer adds hard deadline.

---

## Aggregate Per-Table

- L1 Agreements: 0
- L1 SAFE DISAGREEMENTS: 2
- L1 AI FP: 2
- L1 AI FN: 3
- L2: AGREEMENT
- L3 GATE ERROR: 2 (FN on HG#4 and HG#5)
- L4: SAFE DISAGREEMENT
- L5: SAFE DISAGREEMENT

**Hard Safety Metrics (this table)**:
- **Hard Gate FN (this table): 2** (HG#4 and HG#5)
- P0/P1 decision error (this table): 0
- Core contamination (this table): 0
- TABLE CLOSED decision error (this table): 0

---

## The HG Borderline Dodge Pattern (Critical Pattern Recognition)

Across Tables 3 and 5, Track A used "BORDERLINE" flag for HGs that should have been TRIGGERED:

| Table | HG | AI flagged | Adversarial says |
|---|---|---|---|
| 3 base_visual_dev | HG#4 | BORDERLINE | TRIGGERED |
| 5 sa_data_dictionary | HG#4 | BORDERLINE | TRIGGERED |
| 5 sa_data_dictionary | HG#5 | BORDERLINE | TRIGGERED |

**Pattern**: When Track A encounters a strong concern but doesn't want to commit to triggering, it uses "borderline" to acknowledge without consequence.

**Why this matters**:
- Borderline is not a stable state in the HG framework
- Either a concern triggers the gate or it doesn't
- "Borderline forever" is risk under-statement
- This pattern would compound in production: hundreds of borderline flags = essentially zero gates

**This is the most important Skill Evolution finding from P8-A.**

**Route**: **Master Spec Evolution** — the HG framework needs an explicit "borderline" policy:
- Borderline = requires explicit dismissal reasoning OR promotion to triggered
- No "borderline" as final state

---

## Aggregate Per-Table Interpretation

This is the table where Track A was MOST analytical. The Risk classification (R3+) was correct. The DEFERRED closure was correct in direction.

But the HG analysis was self-defeating. By calling HG#4 and HG#5 "borderline" instead of "triggered", Track A:
1. Acknowledged the concerns (good)
2. Avoided triggering the gates (bad — risks not surfaced)
3. Recommended DEFERRED with "decision brief" (good)
4. But no deadline (bad — can be deferred indefinitely)

The reviewer's recommendations (TRIGGERED, hard deadline, specific deliverables) operationalize what Track A gestured at.

---

## Adversarial Findings Routing

| Finding | Route to | Severity |
|---|---|---|
| "SA purpose" circular | Master Spec Evolution | Medium |
| BIGINT IDENTITY distributed-unfriendly | Skill Evolution (Level A) | Low |
| bit NULL handling unaddressed | Skill Evolution (Level A) | Low |
| asset_level undefined | Skill Evolution (Level A) | Medium |
| **ON DELETE behavior for 5 FKs** | **Skill Evolution (Level B)** | **High** |
| Triple-Key via index != constraint | Skill Evolution (Level A) | High |
| 8 indexes over-indexed for 35 rows | Skill Evolution (Level A) | Low |
| Mixed index naming | JNPF Extension | Low |
| Missing indexes (validation_status, etc.) | Skill Evolution (Level A) | Medium |
| SCD Type 2 code verification | Skill Evolution (Level B) | High |
| version increment policy undefined | Skill Evolution (Level A) | Medium |
| is_deleted + SCD2 dual-pattern | JNPF Extension | Medium |
| WRITE patterns not addressed | Skill Evolution (Level A) | Medium |
| pattern_tags format undefined | JNPF Extension | Medium |
| LLM Confidence operational semantics | JNPF Extension | Medium |
| human_confirmed workflow | JNPF Extension | Medium |
| **Shared projection write contention** | **Skill Evolution (Level B)** | **High** |
| **HG#4 should be TRIGGERED** | **Master Spec Evolution** | **Critical** |
| **HG#5 should be TRIGGERED** | **Master Spec Evolution** | **Critical** |
| Foundry Profile extension vs migration uncommitted | Master Spec Evolution | Medium |
| DEFERRED without deadline | Master Spec Evolution | High |
| **HG borderline dodge pattern** | **Master Spec Evolution** | **Critical** |

**Critical findings**:
1. HG#4 trigger on SA table with 5 incoming FKs (Master Spec Evolution)
2. HG#5 trigger on schema divergence (Master Spec Evolution)
3. HG borderline dodge pattern (Master Spec Evolution — affects Skill logic)
