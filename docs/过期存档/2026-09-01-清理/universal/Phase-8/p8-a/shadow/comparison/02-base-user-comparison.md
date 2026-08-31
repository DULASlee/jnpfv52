# Comparison: Table 02 — base_user

> **Phase**: 8 — P8-A.4 (Comparison)
> **Status**: COMPLETE — **CRITICAL DIVERGENCES**
> **Date**: 2026-08-30

---

## L1: Dimension Comparison

| Dim | AI (Track A) | Adversarial (Track B) | Classification | Notes |
|---|---|---|---|---|
| A Schema | SAFE-REFACTOR (standard JNPF) | 68-col width unjustified; F_OPENID/F_INTE_ASSISTANT/GUESS tag inflation | AI FALSE POSITIVE | Track A's "standard" is unverified |
| B Integrity | SAFE-REFACTOR (app-level FK) | Junction tables unknown; orphan risk = HG#2 candidate | AI FALSE NEGATIVE | Track A's "correctly" is unverified |
| C Index | SAFE-REFACTOR (3 indexes) | 3 of 4 patterns without index; F_QUICK_QUERY dropped | AI FALSE POSITIVE | Pattern-Recommendation disconnect |
| D Lifecycle | No-Finding (independent booleans) | Multi-boolean state machine undocumented | AI FALSE NEGATIVE | Track A assumed independence |
| E CRUD/Query | SAFE-REFACTOR | "Highest query volume" unverified; SELECT * unaddressed | SAFE DISAGREEMENT | |
| F DDD | No-Finding (clear aggregate root, NO ambiguity) | 68-col = likely aggregate ambiguity | AI FALSE NEGATIVE | **CRITICAL** |
| G Consumer/Target | SAFE-REFACTOR | Soft-delete cascade unaddressed; "tenant isolation present" shallow | AI FALSE POSITIVE | |

**L1 Summary**: 7 dimensions; 0 AGREEMENTS; 1 SAFE DISAGREEMENT; 3 AI FP; 3 AI FN.

---

## L2: Risk Comparison

| AI Risk | Adversarial Risk | Classification | Tier Diff |
|---|---|---|---|
| R2 (HIGH confidence) | R3+ (MEDIUM confidence) | **RISK ERROR** | 1 tier (R2 → R3+) |

**Critical**: Risk tier crosses R2/R3+ boundary. This is **P0/P1 Decision Error** candidate.

---

## L3: Hard Gate Comparison

| HG | AI | Adversarial | Classification | If GATE ERROR: FN or FP |
|---|---|---|---|---|
| HG#1 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#2 | NOT triggered | BORDERLINE | SAFE DISAGREEMENT | Reviewer says borderline, AI says not triggered. Track A's conclusion matches reviewer's borderline, so neither is wrong per se. |
| HG#3 | NOT triggered | NOT triggered | AGREEMENT | — |
| HG#4 | NOT triggered | BORDERLINE | SAFE DISAGREEMENT | Same as HG#2 — both flag concerns |
| HG#5 | NOT triggered | **TRIGGERED** | **GATE ERROR (FN)** | **AI missed HG that reviewer triggered** |

**L3 Summary**: 5 HGs; 2 AGREEMENT; 2 SAFE DISAGREEMENT; 1 GATE ERROR (FN on HG#5).

---

## L4: Action Comparison

| AI Action | Adversarial Action | Classification |
|---|---|---|
| SAFE-REFACTOR | DEFERRED — pending HG#5 Decision Brief | **ACTION ERROR** |

Different action paths.

---

## L5: Closure Comparison

| AI Closure | Adversarial Closure | Classification |
|---|---|---|
| NO-CHANGE | DEFERRED | **CLOSURE ERROR** |

AI marked NO-CHANGE; reviewer says DEFERRED. This is **TABLE CLOSED Decision Error** candidate.

---

## Aggregate Per-Table

- L1 Agreements: 0
- L1 SAFE DISAGREEMENTS: 1
- L1 AI FP: 3
- L1 AI FN: 3
- L2 RISK ERROR: 1
- L3 GATE ERROR: 1 (FN on HG#5)
- L4 ACTION ERROR: 1
- L5 CLOSURE ERROR: 1

**Hard Safety Metrics (this table)**:
- **Hard Gate FN (this table): 1** (HG#5)
- **P0/P1 decision error (this table): 1** (R2 → R3+)
- Core contamination (this table): 0
- **TABLE CLOSED decision error (this table): 1** (NO-CHANGE → DEFERRED)

---

## Interpretation: Is This a Real Skill Failure or Calibration Data?

This is the most important question for base_user.

**Argument for "Calibration data" (expected in Adversarial review)**:
- Track A classified R2; reviewer says R3+. This is a 1-tier difference.
- HG#5 trigger is debatable — Track A may have evidence I don't have.
- Closure disagreement follows from risk disagreement.

**Argument for "Real Skill failure"**:
- 68 columns on the identity table with NO aggregate analysis = genuine Skill gap
- HG#5 trigger is well-supported (multiple ambiguous fields, no state machine)
- Junction tables absence is critical — if they don't exist, M:N is encoded wrong; if they do, Track A didn't mention

**My judgment**: This is a **MIXED case**:
- Risk classification (R2 vs R3+) is partially calibration (different reviewers can legitimately disagree)
- HG#5 trigger has STRONG evidence (multiple ambiguous fields)
- Aggregate ambiguity finding is GENUINE — Skill should flag wide tables for aggregate review
- Junction tables gap is GENUINE — Skill should at least mention M:N patterns

**Routing decision**:
- HG#5 trigger: **GENUINE Skill gap** → Skill Evolution (Level B)
- Risk reclassification: **CALIBRATION** → Skill Evolution (Level A) for confidence calibration
- Aggregate analysis gap: **GENUINE Skill gap** → Skill Evolution (Level B) — Skill should detect wide tables
- Junction tables gap: **GENUINE Skill gap** → Skill Evolution (Level B) — Skill should detect M:N patterns

---

## Adversarial Findings Routing

| Finding | Route to | Severity |
|---|---|---|
| "Standard JNPF user model" unverified | Skill Evolution (Level A) | Medium |
| F_INTE_ASSISTANT GUESS tag | Skill Evolution (Level A) | Low |
| 68-col width not justified | Skill Evolution (Level B) | High |
| App-level FK management unverified | Skill Evolution (Level B) | High |
| Junction tables unaddressed | Skill Evolution (Level B) | High |
| F_QUICK_QUERY index recommendation dropped | Skill Evolution (Level A) | Medium |
| F_ORGANIZE_ID index strategy incomplete | Skill Evolution (Level A) | Medium |
| Multi-boolean state machine not documented | Skill Evolution (Level B) | High |
| "Pilot-2 finding pattern" unsupported | Skill Evolution (Level A) | Medium |
| Aggregate ambiguity in 68-col table | **Skill Evolution (Level B)** | **Critical** |
| Soft-delete cascade unaddressed | Skill Evolution (Level B) | High |
| **HG#5 trigger on critical identity table** | **Master Spec Evolution** | **Critical** |
| HG#2 borderline (orphan FK risk) | Skill Evolution (Level A) | Medium |
| HG#4 borderline (cross-module) | Skill Evolution (Level A) | High |

**Critical findings**:
1. Aggregate ambiguity detection in wide tables (Skill Level B)
2. HG#5 trigger on identity table (Master Spec Evolution)
