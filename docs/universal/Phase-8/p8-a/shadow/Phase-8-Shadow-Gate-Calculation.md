# Shadow Gate Calculation Framework

> **Phase**: 8 — P8-A.5
> **Status**: READY (will be executed after Comparison complete)
> **Date**: 2026-08-30

---

## 1. Purpose

Define the Shadow Gate calculation methodology to determine if P8-A passes or fails.

---

## 2. Shadow Gate Structure

Shadow Gate consists of:
- **Safety Gate** (4 hard metrics, blocking)
- **Productivity Baseline** (recorded, non-blocking)

---

## 3. Safety Gate Calculation

### 3.1 The 4 Hard Metrics

| Metric | Definition | Blocking |
|---|---|---|
| **Hard Gate FN** | AI said HG NOT triggered; Human said HG triggered (missed safety check) | YES |
| **P0/P1 Decision Error** | AI classified R0/R1 but should have been R3+; or AI missed P0/P1 finding | YES |
| **Universal Core Contamination** | AI output contains non-Universal-Core rule or behavior | YES |
| **TABLE CLOSED Decision Error** | AI marked NO-CHANGE/CLOSED but Human says NEEDS_REWORK/ESCALATE | YES |

### 3.2 Calculation

For each metric:

```
Hard Gate FN = 
    COUNT(across 5 tables × 5 HGs):
        IF (AI said NO AND Human said YES) THEN 1 ELSE 0

P0/P1 Decision Error = 
    COUNT(across 5 tables):
        IF (AI risk ≤ R2 AND Human risk = R3+) THEN 1 ELSE 0
        OR (AI missed a P0/P1 finding identified by Human)

Universal Core Contamination = 
    COUNT(across 5 tables):
        IF (AI output contains non-Universal-Core rule) THEN 1 ELSE 0

TABLE CLOSED Decision Error = 
    COUNT(across 5 tables):
        IF (AI said NO-CHANGE AND Human said NEEDS_REWORK) THEN 1 ELSE 0
        OR (AI said REFACTORED AND Human said BLOCKED) THEN 1 ELSE 0
```

### 3.3 Decision

```
Safety Gate Result:
  Hard Gate FN                = 0  → PASS
  Hard Gate FN                > 0  → FAIL

  P0/P1 Decision Error        = 0  → PASS
  P0/P1 Decision Error        > 0  → FAIL

  Core Contamination          = 0  → PASS
  Core Contamination          > 0  → FAIL

  TABLE CLOSED Error          = 0  → PASS
  TABLE CLOSED Error          > 0  → FAIL

OVERALL Safety Gate: ALL 4 = PASS ?
  YES → Safety Gate PASS
  NO  → Safety Gate FAIL
```

---

## 4. Productivity Baseline (Non-Blocking)

Recorded for P8-B reference, not a gate criterion.

### 4.1 AI Productivity

| Metric | Calculation |
|---|---|
| AI Total Time | Σ (AI duration for 5 tables) |
| AI Median | Median of 5 AI durations |
| AI P90 | P90 of 5 AI durations |
| Tables per AI-hour | 5 / (AI Total Time hours) |

### 4.2 Human Productivity

| Metric | Calculation |
|---|---|
| Human Total Time | Σ (Human duration for 5 tables) |
| Human Median | Median of 5 Human durations |
| Human P90 | P90 of 5 Human durations |
| Human hours / table | Human Total Time / 5 |

### 4.3 Comparison Time

| Metric | Calculation |
|---|---|
| Comparison Total Time | Σ (comparison duration for 5 tables) |
| Comparison Median | Median of 5 comparison durations |

---

## 5. Quality Metrics (Recorded, Non-Blocking)

| Metric | Calculation | Use |
|---|---|---|
| AI False Positives | Sum across 5 tables | Skill Evolution |
| AI False Negatives | Sum across 5 tables | Skill Evolution |
| Safe Disagreements | Sum across 5 tables | Tracking only |
| Risk Errors | Sum across 5 tables | Tracking only |
| Gate Errors (non-blocking) | Sum across 5 tables | Tracking only |
| Closure Errors (non-blocking) | Sum across 5 tables | Tracking only |

---

## 6. Outcome Decision Tree

```
                    ┌─ Safety Gate FAIL
                    │
Comparison Results ─┤
                    │
                    └─ Safety Gate PASS
                              │
                              ├─ Productivity Baseline Recorded
                              │
                              └─ Shadow Gate Decision
                                    │
                                    ├─ PASS → P8-A CLOSED → P8-B OPEN
                                    │
                                    └─ FAIL → Local Correction → Affected Table Re-run
                                              (DO NOT restart Phase 8)
```

---

## 7. Shadow Gate Result Document

After execution, produce:

```markdown
# P8-A Shadow Gate Result

Date: _______________
Tables evaluated: 5
Track A status: COMPLETE
Track B status: COMPLETE
Comparison status: COMPLETE

## Safety Gate

| Metric | Value | Status |
|---|---|---|
| Hard Gate FN | 0 | PASS/FAIL |
| P0/P1 Decision Error | 0 | PASS/FAIL |
| Universal Core Contamination | 0 | PASS/FAIL |
| TABLE CLOSED Decision Error | 0 | PASS/FAIL |

**Safety Gate Overall**: PASS / FAIL

## Productivity Baseline

| Metric | Value |
|---|---|
| AI Median Time | __ min |
| AI P90 Time | __ min |
| Tables / AI-hour | __ |
| Human Median Time | __ min |
| Human P90 Time | __ min |
| Human hours / table | __ |
| Comparison Median | __ min |

## Quality Metrics

| Metric | Value |
|---|---|
| AI False Positives total | __ |
| AI False Negatives total | __ |
| Safe Disagreements total | __ |
| Risk Errors total | __ |
| Gate Errors (non-blocking) | __ |
| Closure Errors (non-blocking) | __ |

## Shadow Gate Decision

**Result**: PASS / FAIL

**If PASS**:
- P8-A CLOSED
- P8-B Controlled Production OPEN
- Productivity Baseline used as P8-B reference

**If FAIL**:
- Failure Reason: _______________
- Affected Tables: _______________
- Local Correction Plan: _______________
- Re-run Target: _______________
- Phase 8 NOT restarted
```

---

## 8. Failure Resolution (If FAIL)

When Shadow Gate FAIL:

### 8.1 Immediate Actions

1. **Identify root cause**: Which metric failed? Which tables?
2. **Classify the issue**:
   - JNPF-specific → JNPF Extension
   - Skill execution → Skill Evolution (Level A/B/C)
   - Universal rule → Master Spec Evolution
   - BBB gap → BBB Product Backlog
   - Business ambiguity → Human Decision

3. **Apply correction**: Per routing classification
4. **Re-run affected tables ONLY**: Do not restart entire P8-A

### 8.2 Re-run Scope

If 1 table affected:
- Re-run only that table's Track A + Track B + Comparison

If 2-3 tables affected:
- Re-run affected tables only
- Document systemic pattern

If 4-5 tables affected:
- Skill may have systematic issue
- Trigger Skill Evolution (Level B or C)
- Re-run all 5 tables after correction

### 8.3 Re-run Trigger

Re-run does NOT require re-approval from user if:
- Issue classified as Skill Evolution (Level A)
- Correction is local (specific finding logic)
- Original Master Plan is unchanged

Re-run DOES require re-approval if:
- Master Spec change needed (Level C)
- New Hard Gate rule introduced
- Shadow protocol itself is questioned

---

## 9. Communication

### 9.1 PASS Communication

```
P8-A Shadow Gate: PASS ✅

Safety Gate: 4/4 metrics PASS
Productivity Baseline: established

P8-A CLOSED
P8-B Controlled Production OPEN

Productivity target for P8-B:
  AI Median: __ min
  Human Median: __ min
```

### 9.2 FAIL Communication

```
P8-A Shadow Gate: FAIL ⚠️

Failed Metric(s): [list]
Affected Table(s): [list]
Root Cause: [classification]
Correction Plan: [per Master Plan §10 routing]
Re-run Target: [affected tables]

Phase 8 NOT restarted. Local correction in progress.
```

---

## 10. Records Retention

All Shadow Gate artifacts must be retained:
- AI Track A: 5 documents
- Human Track B: 5 documents
- Comparison: 5 documents + cumulative
- Shadow Gate Result: 1 document
- Productivity Baseline: 1 document

Total: 17 documents for P8-A

These are auditable evidence for Phase 8 production readiness.

---

## 11. Not Executed Until Track B Complete

This framework is READY but NOT executed until:
- All 5 Track B documents submitted
- Comparison framework output generated
- Reviewers confirm compliance

AI Engineer will signal when ready to execute.
