# Comparison: Table 03 — base_visual_dev

> **Phase**: 8 — P8-A.4 (Comparison)
> **Status**: COMPLETE
> **Date**: 2026-08-30

---

## L1: Dimension Comparison

| Dim | AI (Track A) | Adversarial (Track B) | Classification | Notes |
|---|---|---|---|---|
| A Schema | SAFE-REFACTOR | Mixed case (f_) unexplained; f_interface_param ambiguous; 30-col coverage gap | AI FALSE POSITIVE | |
| B Integrity | No-Finding | Self-ref cycle prevention unaddressed; F_DB_LINK_ID target unknown | AI FALSE NEGATIVE | |
| C Index | SAFE-REFACTOR (3 indexes) | **F_EN_CODE index MISSED** (critical hot path) | **AI FALSE NEGATIVE (CRITICAL)** | |
| D Lifecycle | SAFE-REFACTOR (custom state machine) | State machine asserted, not verified | AI FALSE POSITIVE | Tag inflation |
| E CRUD/Query | No-Finding | F_EN_CODE as hot path unaddressed | **AI FALSE NEGATIVE (CRITICAL)** | |
| F DDD | No-Finding | JSON-blob aggregate boundary unclear; cross-aggregate JSON refs dangling | SAFE DISAGREEMENT | |
| G Consumer/Target | SAFE-REFACTOR | JSON-blobs need Foundry Profile EXTENSION (not "careful mapping") | AI FALSE POSITIVE | Under-rated |

**L1 Summary**: 7 dimensions; 0 AGREEMENT; 1 SAFE DISAGREEMENT; 3 AI FP; 3 AI FN.

---

## L2: Risk Comparison

| AI Risk | Adversarial Risk | Classification | Tier Diff |
|---|---|---|---|
| R2 (HIGH confidence) | R2 (MEDIUM confidence) | SAFE DISAGREEMENT | 0 (same tier, different confidence) |

**Note**: Both classify as R2. Reviewer's confidence is lower due to operational gaps.

---

## L3: Hard Gate Comparison

| HG | AI | Adversarial | Classification | If GATE ERROR: FN or FP |
|---|---|---|---|---|
| HG#1 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#2 | NOT triggered | BORDERLINE | SAFE DISAGREEMENT | |
| HG#3 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#4 | BORDERLINE | **TRIGGERED** | **GATE ERROR (FN)** | **AI flagged borderline; reviewer says triggered** |
| HG#5 | NOT triggered | NOT triggered | AGREEMENT | — |

**L3 Summary**: 5 HGs; 3 AGREEMENT; 1 SAFE DISAGREEMENT; 1 GATE ERROR (FN on HG#4).

---

## L4: Action Comparison

| AI Action | Adversarial Action | Classification |
|---|---|---|
| SAFE-REFACTOR (3 indexes) | SAFE-REFACTOR (5 indexes + HG#4 Brief + JSON docs) | SAFE DISAGREEMENT |

Both recommend SAFE-REFACTOR. Reviewer adds 2 critical indexes and documentation requirements.

---

## L5: Closure Comparison

| AI Closure | Adversarial Closure | Classification |
|---|---|---|
| NO-CHANGE | NO-CHANGE (conditional) | AGREEMENT (with caveats) |

Both recommend NO-CHANGE. Reviewer adds conditions.

---

## Aggregate Per-Table

- L1 Agreements: 0
- L1 SAFE DISAGREEMENTS: 1
- L1 AI FP: 3
- L1 AI FN: 3
- L2: SAFE DISAGREEMENT
- L3 GATE ERROR: 1 (FN on HG#4)
- L4: SAFE DISAGREEMENT
- L5: AGREEMENT

**Hard Safety Metrics (this table)**:
- **Hard Gate FN (this table): 1** (HG#4)
- P0/P1 decision error (this table): 0
- Core contamination (this table): 0
- TABLE CLOSED decision error (this table): 0

---

## Critical Finding: F_EN_CODE Index Gap

This is the most operationally critical finding across all 5 tables.

**Track A said**:
- Dim E: "Read by en_code (business key) for runtime form loading"
- Dim C: Did NOT recommend an index on F_EN_CODE

**Impact**:
- Every form load in the low-code platform = table scan on BASE_VISUAL_DEV
- Current 48 rows: fast
- Production with 1000+ forms: unacceptable

**This is a genuine Skill gap**: the Skill identified the query pattern but dropped the index recommendation. **Pattern-Recommendation disconnect.**

**Route**: Skill Evolution (Level A) — finding-recommendation consistency.

---

## Adversarial Findings Routing

| Finding | Route to | Severity |
|---|---|---|
| Mixed case (f_) unexplained | Skill Evolution (Level A) | Low |
| f_interface_param ambiguous | Skill Evolution (Level A) | Low |
| 30-col coverage gap | Skill Evolution (Level A) | Medium |
| Self-ref cycle prevention | Skill Evolution (Level B) | Medium |
| F_DB_LINK_ID target unknown | Skill Evolution (Level A) | Medium |
| **F_EN_CODE index MISSED** | **Skill Evolution (Level A)** | **Critical** |
| F_TYPE index recommendation MISSED | Skill Evolution (Level A) | Medium |
| JSON-blob search behavior | Skill Evolution (Level B) | Medium |
| State machine asserted, not verified | Skill Evolution (Level B) | High |
| F_EN_CODE nvarchar(400) anomaly | Skill Evolution (Level A) | Low |
| JSON-blob aggregate boundary unclear | Master Spec Evolution | Medium |
| Cross-aggregate JSON refs dangling | Skill Evolution (Level B) | Medium |
| JSON-blobs need Foundry Profile Extension | Master Spec Evolution | Medium |
| **HG#4 should be TRIGGERED** | **Master Spec Evolution** | **Critical** |

**Critical findings**:
1. F_EN_CODE index gap (Skill Level A — finding-recommendation)
2. HG#4 trigger on cross-module metadata table (Master Spec Evolution)
