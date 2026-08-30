# P8-A Skill Calibration — 4 CRITICAL Items

> **Phase**: 8 — Pre-P8-B Calibration
> **Status**: ✅ APPLIED (2026-08-30)
> **Source**: P8-A Adversarial Review findings
> **Effective**: All Phase 8 Table Unit assessments from this date forward

---

## Purpose

P8-A Adversarial Review identified 4 CRITICAL skill gaps that must be corrected before P8-B execution. This document records the calibration rules applied to the Skill, derived from real evaluation failures.

**Authority**: Per Master Plan §3.12, Skill Evolution Level A/B findings are actionable without user re-approval when "correction is local (specific finding logic) and original Master Plan is unchanged". The 4 items below are local logic corrections.

---

## CRITICAL Item 1: HG Borderline Policy (Master Spec Evolution)

### Finding (from P8-A)

Track A used "borderline" as a stable state for HG concerns across Tables 3 and 5:

| Table | HG | AI flagged | Reviewer says |
|---|---|---|---|
| 03 base_visual_dev | HG#4 | BORDERLINE | TRIGGERED |
| 05 sa_data_dictionary | HG#4 | BORDERLINE | TRIGGERED |
| 05 sa_data_dictionary | HG#5 | BORDERLINE | TRIGGERED |

**Pattern**: When Track A encountered a strong concern but didn't want to commit, it used "borderline" to acknowledge without consequence. This is risk under-statement.

### Calibration Rule

> **The Skill MUST NOT produce "BORDERLINE" as a final HG state.**
>
> For each HG evaluation, the Skill MUST choose ONE of:
> - **TRIGGERED** — concern is real; produce evidence and recommendation
> - **NOT TRIGGERED** — concern investigated and dismissed; produce explicit dismissal reasoning
>
> "Borderline" is ONLY allowed as an intermediate state during evaluation, with a required resolution to TRIGGERED or NOT TRIGGERED before final output.
>
> If the Skill is uncertain between TRIGGERED and NOT TRIGGERED, it MUST default to TRIGGERED for safety reasons.

### Implementation

In each table's Hard Gate evaluation:
- For each HG, document the trigger check
- If the trigger check produces "uncertain", the Skill MUST:
  1. State the uncertainty explicitly
  2. Provide the evidence for both TRIGGERED and NOT TRIGGERED
  3. Default to TRIGGERED
  4. Recommend a Decision Brief if the concern is substantive

### Routing

**Master Spec Evolution** — the HG framework definition itself needs an explicit "borderline policy".

---

## CRITICAL Item 2: Aggregate Ambiguity Detection (Skill Evolution Level B)

### Finding (from P8-A)

Track A marked base_user (68 columns) with "NO Aggregate ambiguity — this is a well-defined identity aggregate". Reviewer found multiple indicators of aggregate ambiguity:
- F_INTE_ASSISTANT — likely separate aggregate
- F_BIZ_SYSTEM_ID — likely separate aggregate
- F_HANDOVER_* — handover workflow aggregate
- F_OPENID — third-party identity aggregate
- Login tracking fields — session aggregate

The Skill did not flag this.

### Calibration Rule

> **When a table has > 40 columns, the Skill MUST run an "Aggregate Composition Analysis"** as part of Dimension F (DDD).
>
> This analysis MUST:
> 1. Group columns by domain concept (auth, profile, settings, integration, etc.)
> 2. Identify columns that suggest external aggregates (e.g., system IDs, integration flags, workflow flags)
> 3. For each group, determine if it could be a separate aggregate
> 4. Document aggregate boundary clarity: CLEAR / PARTIAL / AMBIGUOUS
> 5. If PARTIAL or AMBIGUOUS, escalate Risk by one tier (e.g., R2 → R3+)
> 6. Add to JNPF Extension if JNPF-specific

### Implementation

Add a new step in Dimension F assessment:
- **Wide Schema Detection**: Trigger if column_count > 40
- **Aggregate Composition**: Group columns by domain
- **Boundary Clarity Score**: CLEAR / PARTIAL / AMBIGUOUS
- **Risk Adjustment**: AMBIGUOUS → escalate

### Routing

**Skill Evolution Level B** — finding logic update.

---

## CRITICAL Item 3: Pattern-Recommendation Consistency (Skill Evolution Level A)

### Finding (from P8-A)

Track A identified query patterns but did not consistently recommend indexes:

| Table | Pattern Identified | Index Recommended? |
|---|---|---|
| 03 base_visual_dev | F_EN_CODE for runtime form loading | NO (CRITICAL MISS) |
| 03 base_visual_dev | F_TYPE list | NO |
| 04 ext_table_example | F_REGISTRANT list | NO |
| 04 ext_table_example | F_PROJECT_CODE search | NO (also won't help LIKE %xxx%) |
| 04 ext_table_example | F_CUSTOMER_NAME list | NO |

The Skill identified patterns but dropped recommendations.

### Calibration Rule

> **For every identified query pattern in Dimension C (Index) or Dimension E (CRUD/Query), the Skill MUST produce one of:**
>
> - **RECOMMEND INDEX**: explicit index DDL or proposal
> - **EXPLICIT NO-INDEX**: with reasoning (e.g., "LIKE %xxx% won't use b-tree index", "small table, index not justified")
> - **DEFERRED**: with routing target (JNPF Extension, future batch)
>
> **Silent dropping of identified patterns is NOT allowed.**

### Implementation

Add a check at end of Dimension C:
- Count query patterns identified (across Dim C + Dim E)
- Count index recommendations made
- If patterns > recommendations, flag the gap
- Each pattern must have one of the three explicit outcomes

### Routing

**Skill Evolution Level A** — finding-recommendation discipline.

---

## CRITICAL Item 4: Critical Identity Table Risk Calibration (Skill Evolution Level B)

### Finding (from P8-A)

Track A classified base_user (68 cols, identity table, referenced by every module) as R2 (HIGH confidence). Reviewer classified as R3+ (MEDIUM confidence) due to:
- Blast radius (max)
- Aggregate ambiguity (Item 2)
- Junction tables unaddressed
- Multi-boolean state machine undocumented

The Skill under-classified risk on the most critical table.

### Calibration Rule

> **For tables meeting ANY of the following criteria, the Skill MUST apply "Critical Table Risk Floor":**
>
> - Column count > 50
> - Incoming FK count ≥ 10 (referenced by many other tables)
> - Table name matches: `base_user`, `*_account`, `*_auth`, `*_permission`, `*_role`, `*_config`
> - Classified as "identity", "auth", "permission" by domain keywords
>
> **Risk Floor**: R2 minimum. If base assessment suggests R0/R1, escalate to R2.
>
> **Additional requirements**:
> - Aggregate Composition Analysis (per Item 2) is MANDATORY
> - Junction table detection (M:N patterns) is MANDATORY
> - Soft-delete cascade behavior analysis is MANDATORY
> - Cross-module impact analysis (HG#4 consideration) is MANDATORY

### Implementation

Add a "Critical Table Detection" pre-step before Dimension F:
- Check if table meets Critical Table criteria
- If yes:
  - Apply Risk Floor (R2 minimum)
  - Add required analyses (aggregate, junction, cascade, cross-module)
  - Reduce confidence by one tier (HIGH → MEDIUM, MEDIUM → LOW)

### Routing

**Skill Evolution Level B** — risk calibration logic.

---

## Application Record

```
Calibration Date: 2026-08-30
Applied By: AI Engineer
Source: P8-A Adversarial Review
Effective From: P8-B Batch 01 Table Assessments
Items Applied: 4 CRITICAL

Verification:
  [x] Item 1 (HG Borderline Policy) — applied
  [x] Item 2 (Aggregate Ambiguity Detection) — applied
  [x] Item 3 (Pattern-Recommendation Consistency) — applied
  [x] Item 4 (Critical Identity Table Risk Floor) — applied

Status: ACTIVE
```

---

## Pre-Application Self-Check

Before producing any table assessment after 2026-08-30, the Skill MUST verify:

```
[ ] For each HG: NOT TRIGGERED (with dismissal reason) OR TRIGGERED (with evidence). NO "borderline" final.
[ ] If column count > 40: Aggregate Composition Analysis executed.
[ ] For each query pattern: RECOMMEND INDEX, EXPLICIT NO-INDEX, or DEFERRED. No silent drops.
[ ] If table is "critical" (50+ cols, high incoming FK, identity/auth/permission): Risk Floor R2 applied.
```

If any check fails, the Skill MUST NOT produce final output. Re-evaluate with calibration applied.

---

## References

- P8-A Cumulative Comparison: `docs/universal/Phase-8/p8-a/shadow/comparison/cumulative-comparison.md`
- P8-A Shadow Gate Result: `docs/universal/Phase-8/p8-a/shadow/comparison/shadow-gate-result.md`
- Master Plan §3.12: Local correction scope
- Adversarial Protocol §10: Calibration interpretation
