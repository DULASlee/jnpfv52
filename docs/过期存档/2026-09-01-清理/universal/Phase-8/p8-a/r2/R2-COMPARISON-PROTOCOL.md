# R2 — Comparison Protocol (8 Metrics + 4 Safety Gates + 6 Disagreement Classes)

> **Phase**: 8 — P8-A.5 (R2)
> **Status**: 🟢 **READY FOR USE**
> **Date**: 2026-08-30
> **Purpose**: Define exact computation rules for comparing Skill (Result A) vs Independent AI Expert (Result B)

---

## 1. Input

After both A and B are committed for a Round:

```
Result A files: p8-a/r2/round-N/skill/[NN]-[table_name].md  (5 files)
Result B files: p8-a/r2/round-N/expert/[NN]-[table_name].md (5 files)
```

Comparison is done **per table** then **cumulative**.

---

## 2. Per-Table Comparison Schema

For each Table Unit, produce a comparison block.

### 2.1 The 8 Metrics

#### Metric 1: Dimension Agreement

**Definition**: Per-dimension (A–G) match rate between Skill and Expert.

**Computation**:
```
For each dimension (A, B, C, D, E, F, G):
    - Extract Skill's finding (substantive content)
    - Extract Expert's finding (substantive content)
    - Score: MATCH (substantive agreement) / PARTIAL (overlap but different emphasis) / DIVERGE (different conclusion)
    
Dimension Agreement Rate = (count of MATCH × 1 + count of PARTIAL × 0.5) / 7
```

**Pass threshold**: ≥ 0.75 (5.25 out of 7 full equivalent)

**Output**:
| Dim | Skill | Expert | Score |
|-----|-------|--------|-------|
| A Schema | ... | ... | MATCH/PARTIAL/DIVERGE |
| B Integrity | ... | ... | ... |
| ... | ... | ... | ... |

---

#### Metric 2: Finding Agreement

**Definition**: Substantive findings (specific issues identified) overlap.

**Computation**:
```
For each Finding identified by EITHER Skill or Expert:
    - Check whether the OTHER party also identified it (by substance, not wording)
    - Score: SHARED / SKILL-ONLY / EXPERT-ONLY
    
Finding Agreement Rate = SHARED / (SHARED + SKILL-ONLY + EXPERT-ONLY)
```

**Pass threshold**: ≥ 0.60 (60% of findings shared)

**Critical findings** (Hard Gate triggers, P0/P1 risks, scope violations) are weighted 2x.

**Output**:
| Finding | Skill? | Expert? | Weight |
|---------|--------|---------|--------|
| ... | YES | YES | 1 / 2 |
| ... | YES | NO | 1 / 2 |

---

#### Metric 3: Risk Agreement

**Definition**: Risk level (R0/R1/R2/R3+) matches.

**Computation**:
```
Skill Risk: RS (one of R0, R1, R2, R3+)
Expert Risk: RE (one of R0, R1, R2, R3+)

Distance = |RS_index - RE_index|  where index is 0/1/2/3

EXACT     = 0
ADJACENT  = 1 (e.g., R1 vs R2)
REMOTE    = 2 (e.g., R1 vs R3+)
EXTREME   = 3 (e.g., R0 vs R3+)
```

**Pass threshold**: EXACT or ADJACENT
**Fail**: REMOTE or EXTREME

**Output**:
```
Skill Risk:    R2
Expert Risk:   R2
Distance:      0
Result:        MATCH
```

---

#### Metric 4: Hard Gate Agreement

**Definition**: HG triggers match exactly (no "borderline" leniency).

**Computation**:
```
For each HG (HG#1–HG#5):
    - Skill verdict: TRIGGERED / NOT / BORDERLINE
    - Expert verdict: TRIGGERED / NOT / BORDERLINE
    
    MATCH if both same verdict
    DIVERGE otherwise
    
    CRITICAL DIVERGE if one says TRIGGERED and other says NOT (or BORDERLINE vs TRIGGERED)
```

**Pass threshold**: 0 CRITICAL DIVERGE (any HG where one side triggered and other didn't)

**Output**:
| HG | Skill | Expert | Verdict |
|----|-------|--------|---------|
| HG#1 | NOT | NOT | MATCH |
| HG#4 | NOT | TRIGGERED | **CRITICAL DIVERGE** |
| ... | ... | ... | ... |

---

#### Metric 5: Action Agreement

**Definition**: Recommended action matches.

**Computation**:
```
Skill Action:    A_S (one of 6 gates)
Expert Action:   A_E

EXACT MATCH         = same gate
EQUIVALENT MATCH    = (AUTO-CLOSE, AUTO-APPLY, EVIDENCE-DRIVEN) all equivalent at R0/R1
                     (HUMAN APPROVAL, CROSS-TABLE, DESTRUCTIVE) all equivalent at R3+
                     NO-CHANGE = AUTO-CLOSE in closure terms
DIVERGE             = otherwise
```

**Pass threshold**: EXACT or EQUIVALENT

---

#### Metric 6: Closure Agreement

**Definition**: Final closure (NO-CHANGE / REFACTOR / DEFERRED / ACCEPT-AS-IS) matches.

**Computation**:
```
Skill Closure:    C_S
Expert Closure:   C_E

MATCH       = same
SEMANTIC    = REFACTOR and ACCEPT-AS-IS often interchangeable
DIVERGE     = otherwise
```

**Pass threshold**: MATCH or SEMANTIC

---

#### Metric 7: Evidence Sufficiency Agreement

**Definition**: Both met stop condition at appropriate time.

**Computation**:
```
Both YES    = AGREE (both stopped appropriately)
Both NO     = AGREE (both continued searching, possibly over-searching)
Mismatch    = DISAGREE (one over-searched or under-searched)
```

**Pass threshold**: AGREE

---

#### Metric 8: Scope/Boundary Agreement

**Definition**: Both correctly identified table's scope boundary (in-scope vs out-of-scope).

**Computation**:
```
Both say IN_SCOPE            = AGREE
Both say OUT_OF_SCOPE         = AGREE
Both say OUT_OF_SCOPE w/      = AGREE if disposition matches
   ACCEPT-AS-IS/RETAIN-AS-EXCEPTION
Disagree on scope             = DISAGREE (one missed the boundary)
```

**Pass threshold**: AGREE (with documented disposition if both out-of-scope)

---

### 2.2 Per-Table Verdict

```
VERDICT = PASS    if all 8 metrics pass
        = FAIL    if any of Metrics 4 (Hard Gate CRITICAL DIVERGE) fails
                or any safety gate (S1-S4) triggered
        = REVIEW  if Metric 1-3, 5-8 fail but no safety gate triggered
```

---

## 3. The 4 Safety Gates

### Safety Gate S1: Hard Gate False Negative

**Definition**: Expert triggered an HG that Skill missed (where Expert is correct).

**Detection**:
```
For each table:
    Expert HG verdict = TRIGGERED
    AND
    Skill HG verdict = NOT (or BORDERLINE)
    AND
    Post-hoc review (or comparison evidence) confirms Expert correct
    
→ S1 fires
```

**Threshold**: 0 across 10 tables

**Escalation**: Human Governance Review (Chief Architect sign-off required)

---

### Safety Gate S2: P0/P1 Decision Error

**Definition**: Skill or Expert made a P0/P1 wrong decision.

**Detection**:
```
For each table:
    Either side classified Risk as R0/R1 (P0/P1)
    AND
    Post-hoc review confirms this classification was WRONG
    (e.g., high-risk issue was actually low-risk, or vice versa)
    
→ S2 fires
```

**Threshold**: 0 across 10 tables

**Escalation**: Human Governance Review

---

### Safety Gate S3: Scope Error

**Definition**: Out-of-scope table marked in-scope (or vice versa).

**Detection**:
```
For each table:
    Expert says OUT_OF_SCOPE (e.g., SVR-001 case)
    AND
    Skill says IN_SCOPE
    
    OR
    
    Both say IN_SCOPE but Post-hoc review identifies Scope Error
    
→ S3 fires
```

**Threshold**: 0 across 10 tables

**Escalation**: Human Governance Review (S3 errors can leak demo/test data to production)

---

### Safety Gate S4: Closure Error

**Definition**: Table marked CLOSED but actually has unresolved critical Finding.

**Detection**:
```
For each table:
    Skill OR Expert recommends CLOSED (NO-CHANGE / REFACTOR / ACCEPT-AS-IS)
    AND
    Comparison reveals unresolved critical Finding
    (Hard Gate triggered but not addressed, OR major evidence gap)
    
→ S4 fires (MAJOR if Hard Gate unresolved; MINOR if evidence gap only)
```

**Threshold**:
- 0 MAJOR Closure Errors
- ≤ 2 MINOR Closure Errors (acceptable)

**Escalation**: 
- MAJOR → Human Governance Review
- MINOR → Document, continue

---

## 4. The 6 Disagreement Classes

When Skill and Expert disagree, classify each disagreement instance.

### 4.1 Class Definitions

| Class | Definition | Default Resolution |
|-------|------------|-------------------|
| **AGREEMENT** | Both produce same conclusion | Record as evidence of stability |
| **SAFE DISAGREEMENT** | Different but neither wrong (e.g., R2 vs R3+, NO-CHANGE vs REFACTOR on minor indexes) | Record; not blocking |
| **REAL SKILL MISS** | Expert correct, Skill missed critical finding (Hard Gate, P0/P1 issue) | Skill calibration item |
| **INDEPENDENT JUDGE ERROR** | Skill correct, Expert made error | Expert feedback; not Skill issue |
| **EVIDENCE DIFFERENCE** | Both used different evidence bases (one read more code, other stopped earlier) | Resolve via Master Spec (which is authoritative) |
| **RUBRIC DIFFERENCE** | Both correctly applied but different rubric interpretation (e.g., one says "borderline" counts as triggered) | Document; may need spec clarification |

### 4.2 Classification Method

For each disagreement:

```
Step 1: Is one side objectively wrong?
        YES → REAL SKILL MISS or INDEPENDENT JUDGE ERROR
        NO  → continue

Step 2: Did they use different evidence?
        YES → EVIDENCE DIFFERENCE
        NO  → continue

Step 3: Is this a matter of interpretation (e.g., "borderline")?
        YES → RUBRIC DIFFERENCE
        NO  → SAFE DISAGREEMENT (default fallback)
```

### 4.3 Distribution Analysis

After all 10 tables, compute disagreement distribution:

```
AGREEMENT             = count
SAFE DISAGREEMENT     = count
REAL SKILL MISS       = count  → Skill calibration items
INDEPENDENT JUDGE ERROR = count → Expert feedback (not Skill issue)
EVIDENCE DIFFERENCE   = count  → Routing resolution
RUBRIC DIFFERENCE     = count  → Spec clarification item

If REAL SKILL MISS ≥ 3 with same pattern → SYSTEMATIC CALIBRATION NEEDED
```

---

## 5. Cumulative Comparison Output

### 5.1 Per-Round Cumulative Schema

For Round N, produce `p8-a/r2/round-N/comparison/cumulative-comparison.md`:

```markdown
# R2 — Round N — Cumulative Comparison

## 1. Coverage Verification
- Round N selected tables: [list]
- Coverage matrix satisfied: YES/NO

## 2. Per-Table Summary
| # | Table | Verdict | HG Critical? | Safety Gates | Disagreement Class |
|---|-------|---------|--------------|--------------|---------------------|
| 01 | ... | PASS/FAIL/REVIEW | YES/NO | S1-S4 status | ... |
| ... | ... | ... | ... | ... | ... |

## 3. 8 Metrics Cumulative
| Metric | Threshold | Round N Score |
|--------|-----------|---------------|
| 1. Dimension Agreement | ≥ 0.75 | ... |
| 2. Finding Agreement | ≥ 0.60 | ... |
| 3. Risk Agreement | EXACT/ADJACENT | ... |
| 4. Hard Gate Agreement | 0 CRITICAL | ... |
| 5. Action Agreement | EXACT/EQUIV | ... |
| 6. Closure Agreement | MATCH/SEMANTIC | ... |
| 7. Evidence Suff Agreement | AGREE | ... |
| 8. Scope Agreement | AGREE | ... |

## 4. 4 Safety Gates Cumulative
| Safety Gate | Threshold | Round N Status |
|-------------|-----------|----------------|
| S1 Hard Gate FN | 0 | ... |
| S2 P0/P1 Error | 0 | ... |
| S3 Scope Error | 0 | ... |
| S4 Closure Error | 0 major / ≤2 minor | ... |

## 5. Disagreement Distribution
| Class | Count | Notes |
|-------|-------|-------|
| AGREEMENT | ... | ... |
| SAFE DISAGREEMENT | ... | ... |
| REAL SKILL MISS | ... | ... |
| INDEPENDENT JUDGE ERROR | ... | ... |
| EVIDENCE DIFFERENCE | ... | ... |
| RUBRIC DIFFERENCE | ... | ... |

## 6. Skill Calibration Items
[List of REAL SKILL MISS items to route to skill evolution]

## 7. Round Verdict
[Round N Verdict: PASS / FAIL / CONDITIONAL / REVIEW]
```

### 5.2 Cross-Round Cumulative (After Round 2)

Same schema but spans both rounds (10 tables total).

---

## 6. Output Files

### 6.1 Per-Table Comparison

`p8-a/r2/round-N/comparison/[NN]-[table_name]-comparison.md`

### 6.2 Per-Round Cumulative

`p8-a/r2/round-N/comparison/cumulative-comparison.md`

### 6.3 Cross-Round Cumulative (Final)

`p8-a/r2/COVERAGE-MATRIX.md` (or similar final report)

---

## 7. Special Cases

### 7.1 Both Wrong

If both Skill and Expert miss the same HG, that's still an issue but **not S1** (no Expert vs Skill disagreement). This is logged separately as **BOTH MISSED** and escalates to Human Governance Review.

### 7.2 Undefined Situation

If Skill or Expert encounters an undefined situation (per Skill §2.2), both should escalate. If one escalates and the other makes a decision, that's a RUBRIC DIFFERENCE.

### 7.3 Scope Boundary Disputes

If both say IN_SCOPE but disposition differs (e.g., one says SAFE-REFACTOR, other says HUMAN APPROVAL), this is Action Agreement (Metric 5) — typically EQUIVALENT unless Risk differs.

---

**Document version**: 1.0
**Prepared by**: AI Engineer
**Date**: 2026-08-30
**Status**: Ready for comparison execution
