# AI / Human Comparison Schema

> **Phase**: 8 — P8-A.3 → P8-A.4
> **Status**: READY (will be executed after Track B complete)
> **Date**: 2026-08-30

---

## 1. Purpose

Define the comparison framework between AI Track A and Human Track B outputs to:
1. Identify AGREEMENT vs DISAGREEMENT per dimension
2. Classify disagreements beyond simple FP/FN
3. Calculate Safety Gate (4 hard metrics)
4. Establish Productivity Baseline for P8-B

---

## 2. Comparison Methodology

### 2.1 Per-Table Comparison

For each of 5 tables, compare:
- 7 dimensions (A-G) per assessment
- Risk Classification
- 5 Hard Gates
- Recommended Action
- Recommended Closure

### 2.2 Comparison Levels

| Level | Compare |
|---|---|
| L1: Dimension | A-G per table (35 dimensions total) |
| L2: Risk | 5 risk classifications |
| L3: Hard Gate | 25 HG evaluations (5 HG × 5 tables) |
| L4: Action | 5 action recommendations |
| L5: Closure | 5 closure statuses |

---

## 3. Disagreement Classification

Per comparison item, classify as ONE of:

### 3.1 AGREEMENT

```
AI and Human produced equivalent assessment.
No further action needed.
```

### 3.2 SAFE DISAGREEMENT

```
Both AI and Human produced valid but different assessments.
Reasoning may differ but neither is wrong.

Examples:
- AI recommends index; Human says "no index needed"
  → Both are reasonable; index is optional optimization
- AI classifies R2; Human classifies R1
  → Risk level differs by one tier; neither is fundamentally wrong

Action: Record. Continue.
```

### 3.3 AI FALSE POSITIVE

```
AI flagged a Finding that Human determined is NOT a real issue.

Examples:
- AI claims "missing index" but Human verified query patterns don't need it
- AI claims "R2" but Human verified all evidence shows R1

Action: Record for Skill Evolution (Level B — finding logic calibration)
```

### 3.4 AI FALSE NEGATIVE

```
AI missed a Finding that Human identified as real issue.

Examples:
- AI said "no index needed" but Human verified critical query needs it
- AI classified R0/R1 but Human identified R3+ (e.g., aggregate boundary issue)

Action: 
- If P0/P1 → Shadow Gate FAIL
- If R1/R2 → Record for Skill Evolution (Level B)
```

### 3.5 RISK ERROR

```
AI and Human agree there's an issue but disagree on Risk level.
Difference ≥ 2 tiers (e.g., R0/R1 vs R3+, or R2 vs R3+).

Action:
- If classification crosses HG threshold → Shadow Gate FAIL
- Otherwise → Record; non-blocking
```

### 3.6 GATE ERROR

```
AI and Human disagree on whether a Hard Gate is triggered.

Examples:
- AI says HG#5 NOT triggered; Human says HG#5 triggered
- AI says HG#2 triggered; Human says NOT triggered

Action:
- If HG false-negative (AI missed HG) → Shadow Gate FAIL (Hard Gate FN = 1)
- If HG false-positive (AI triggered HG Human disagrees) → Record
```

### 3.7 CLOSURE ERROR

```
AI and Human disagree on Recommended Closure status.

Examples:
- AI says NO-CHANGE; Human says REFACTORED
- AI says REFACTORED; Human says BLOCKED

Action:
- If AI marked TABLE CLOSED but Human says NEEDS_REWORK / ESCALATE → Shadow Gate FAIL
- If AI marked NEEDS_REWORK but Human says NO-CHANGE → Record (not blocking)
```

---

## 4. Comparison Record Template

For each table, fill:

```markdown
# Comparison: {Table Name}

## L1: Dimension Comparison

| Dim | AI | Human | Classification | Notes |
|---|---|---|---|---|
| A Schema | [AI finding] | [Human finding] | AGREEMENT / SAFE DISAGREE / AI FP / AI FN / N/A | |
| B Integrity | | | | |
| C Index | | | | |
| D Lifecycle | | | | |
| E CRUD/Query | | | | |
| F DDD | | | | |
| G Consumer/Target | | | | |

## L2: Risk Comparison

| AI Risk | Human Risk | Classification | Tier Diff |
|---|---|---|---|
| | | AGREEMENT / SAFE DISAGREE / RISK ERROR | |

## L3: Hard Gate Comparison

| HG | AI | Human | Classification | If GATE ERROR: FN or FP |
|---|---|---|---|---|
| HG#1 | | | | |
| HG#2 | | | | |
| HG#3 | | | | |
| HG#4 | | | | |
| HG#5 | | | | |

## L4: Action Comparison

| AI Action | Human Action | Classification |
|---|---|---|
| | | AGREEMENT / SAFE DISAGREE / N/A |

## L5: Closure Comparison

| AI Closure | Human Closure | Classification |
|---|---|---|
| | | AGREEMENT / SAFE DISAGREE / CLOSURE ERROR |

## Aggregate Per-Table

- Total dimensions assessed: 7
- Agreements: ___
- Safe disagreements: ___
- AI false positives: ___
- AI false negatives: ___
- Risk errors: ___
- Gate errors: ___
- Closure errors: ___

## Per-Table Shadow Gate Sub-Check

- Hard Gate FN (this table): 0 = required for PASS
- P0/P1 decision error (this table): 0 = required for PASS
- Core contamination (this table): 0 = required for PASS
- TABLE CLOSED decision error (this table): 0 = required for PASS
```

---

## 5. Cumulative Metrics (Across 5 Tables)

### 5.1 Safety Gate Calculation

| Metric | Calculation | Threshold |
|---|---|---|
| Hard Gate FN | Sum of (AI said NO, Human said YES) across all HG × 5 tables | = 0 |
| P0/P1 decision error | AI marked NO-CHANGE but Human says R3+ / HG triggered | = 0 |
| Universal Core contamination | Any AI output contains non-Universal-Core rule | = 0 |
| TABLE CLOSED decision error | AI marked CLOSED but Human says NEEDS_REWORK / ESCALATE | = 0 |

**Hard Safety Rule**: Any of the 4 metrics > 0 → **Shadow Gate FAIL**

### 5.2 Quality Metrics (Cumulative)

| Metric | Calculation |
|---|---|
| AI False Positives total | Sum of L1 "AI FP" + L3 "HG FP" across 5 tables |
| AI False Negatives total | Sum of L1 "AI FN" + L3 "HG FN" across 5 tables |
| Safe Disagreements total | Sum of L1-L5 "SAFE DISAGREE" |
| Risk Errors total | Sum of L2 "RISK ERROR" |
| Gate Errors total | Sum of L3 "GATE ERROR" |
| Closure Errors total | Sum of L5 "CLOSURE ERROR" |

### 5.3 Productivity Metrics (P8-B Baseline)

| Metric | Source |
|---|---|
| AI Total Time | Sum of AI duration for 5 tables (from Track A timestamps) |
| AI Median Time per Table | Median AI duration |
| AI P90 Time | P90 AI duration |
| Human Total Time | Sum of Human duration for 5 tables (from Track B review records) |
| Human Median Time per Table | Median Human duration |
| Human P90 Time | P90 Human duration |
| Tables / AI-hour | 5 / (AI Total Time hours) |
| Human hours / Table | Human Total Time / 5 |
| Comparison Time | Total time to execute AI/Human comparison (per AI engineer) |

---

## 6. Comparison Execution Order

After Track B is complete for ALL 5 tables:

```
1. For each table, execute comparison
2. Aggregate to 5-table cumulative
3. Calculate 4 hard safety metrics
4. Calculate productivity baseline
5. Execute Shadow Gate decision
6. Report results
```

---

## 7. Shadow Gate Decision

```
Safety Gate:
  Hard Gate FN                = 0  ?  PASS / FAIL
  P0/P1 decision error        = 0  ?  PASS / FAIL
  Core contamination          = 0  ?  PASS / FAIL
  TABLE CLOSED decision error = 0  ?  PASS / FAIL

Overall Safety Gate: ALL 4 = 0 ?

Productivity Baseline:
  Median AI time: _____ min
  P90 AI time: _____ min
  Tables / AI-hour: _____
  Median Human review time: _____ min
  P90 Human review time: _____ min
  Human hours / table: _____

Decision:
  IF Safety Gate ALL PASS:
      → Shadow Gate PASS → P8-A CLOSED → P8-B OPEN
  ELSE:
      → Shadow Gate FAIL → identify systemic cause → local correction → affected table re-run
      → DO NOT restart Phase 8
```

---

## 8. Edge Cases

### 8.1 Both AI and Human Wrong

If both AI and Human produce equivalent but incorrect assessments:
- This is NOT counted in FP/FN metrics
- However, it indicates skill calibration gap → Skill Evolution (Level C)
- May require Master Spec Evolution if pattern is systematic

### 8.2 Reviewer Declares Track B Void

If reviewer violates blind rule:
- Affected tables' Track B is voided
- Voided tables require fresh review
- Other tables' comparison can proceed if reviewer didn't view Track A for those

### 8.3 Multi-Reviewer Disagreement

If multiple reviewers cover different tables:
- Each table's comparison is independent
- Aggregated metrics include all 5 tables' comparisons

---

## 9. Output Document

After comparison:

```
docs/universal/Phase-8/p8-a/shadow/comparison/
  01-base-sys-config-comparison.md
  02-base-user-comparison.md
  03-base-visual-dev-comparison.md
  04-ext-table-example-comparison.md
  05-sa-data-dictionary-comparison.md
  cumulative-comparison.md
  shadow-gate-result.md
  productivity-baseline.md
```

---

## 10. Comparison NOT Executed Until Track B Complete

This schema is READY but NOT executed until:
- All 5 Track B documents are submitted
- Reviewers confirm blind review compliance
- AI Engineer is signaled to proceed with comparison
