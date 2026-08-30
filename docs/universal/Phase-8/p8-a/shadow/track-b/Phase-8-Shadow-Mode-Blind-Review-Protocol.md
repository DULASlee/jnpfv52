# Blind Review Protocol

> **Phase**: 8 — P8-A.3
> **Status**: ACTIVE (Updated with Architecture Baseline reference)
> **Date**: 2026-08-30
> **Amendment**: 2026-08-30 — Added shared context references (see §0)

---

## 0. Shared Context (NEW — 2026-08-30)

Both AI Track A and Human Track B MUST be grounded in the same **shared objective context**. This amendment adds the JNPF Database Architecture Baseline to the reviewer's available materials.

### 0.1 Reviewer MAY access

| Material | Path | Purpose |
|---|---|---|
| JNPF Database Architecture Manual | `docs/architecture/JNPF-Database-Architecture-Manual.md` | Module context, naming conventions, design patterns |
| JNPF Complete Table List | `docs/architecture/JNPF-Complete-Table-List.md` | Table Registry navigation by classification |
| JNPF Extension doc | (per Phase 7 frozen artifacts) | JNPF-specific semantics |
| Foundry Target Profile | (per Phase 7 frozen artifacts) | Target infrastructure requirements |
| Actual DB metadata | `INFORMATION_SCHEMA.*`, `sys.*` | Verifiable evidence |

### 0.2 Reviewer MUST NOT access

- ❌ AI Track A document (any part)
- ❌ AI's Risk classification
- ❌ AI's per-dimension Findings
- ❌ AI's Hard Gate judgment
- ❌ AI's Recommended Action
- ❌ AI's Recommended Closure
- ❌ AI's Evidence Ledger

### 0.3 Principle

> **Architecture Baseline = Context** (navigation, naming, patterns)
> **Actual Metadata = Evidence** (column types, indexes, FK)
> **Extension = JNPF Semantics** (domain-specific meaning)
> **Target Profile = Target Requirements** (infrastructure needs)
> **Skill = Decision Method** (how to assess)
>
> These five layers MUST NOT be confused.

### 0.4 Evidence Sufficiency Stop Rule

When reviewing, load only the **minimum sufficient evidence**:

```
Reviewer starts
   ↓
Locate Table in Complete Table List
   ↓
Read relevant Architecture Manual section
   ↓
Read JNPF Extension mapping (if any)
   ↓
Inspect Actual DB Metadata (INFORMATION_SCHEMA)
   ↓
STOP — do not load entire 36.5 KB Manual per table
```

---

## 1. Purpose

Define the operational protocol for Human Track B (Blind Review) to ensure:
1. **Independence**: Reviewer judgments are not influenced by AI Track A
2. **Comparability**: Track B output can be cleanly compared to Track A
3. **Auditability**: All reasoning is captured for future review
4. **Disagreement Discovery**: AI/Human divergence is detectable, not hidden

---

## 2. Reviewer Eligibility

### 2.1 Minimum Qualifications

Reviewer should have:
- Working knowledge of JNPF database architecture
- Understanding of SQL Server table conventions
- Ability to read C# Entity code (or know when to skip)
- Familiarity with risk classification (R0-R5)

### 2.2 NOT Required

- Same person who built the AI
- Knowledge of AI Track A conclusions
- Prior experience with this specific table

### 2.3 Multiple Reviewers

If 1 person conducts all 5 reviews:
- Maintain isolation between reviews (complete one Track B before starting next)
- Do NOT revisit previous Track B after starting next
- Reviewer is encouraged to take breaks between tables

If multiple people participate:
- Each reviewer handles 1+ tables independently
- Cross-contamination of conclusions is a violation of protocol

---

## 3. Isolation Rules (HARD)

### 3.1 Pre-Review Isolation

Before starting Track B for any table:

```
[ ] I have NOT opened AI Track A document for this table
[ ] I have NOT read the AI's Risk classification
[ ] I have NOT read the AI's Findings
[ ] I have NOT read the AI's Hard Gate judgment
[ ] I have NOT read the AI's Recommended Action
[ ] I have NOT read the AI's Recommended Closure
```

### 3.2 During Review

Available resources:
- ✅ DB schema (SELECT * FROM sys.columns WHERE ...)
- ✅ Index metadata (sys.indexes, sys.index_columns)
- ✅ FK metadata (sys.foreign_keys)
- ✅ Entity code (if exists at expected path)
- ✅ Application code patterns (read-only)
- ✅ Domain knowledge

Forbidden resources during this review:
- ❌ AI Track A document (any part)
- ❌ AI's per-dimension Findings
- ❌ AI's Evidence Ledger
- ❌ AI's Risk classification
- ❌ Any summary of AI's conclusions

### 3.3 Post-Submission Isolation

After submitting Track B:
- DO NOT view AI Track A until Comparison phase begins
- If you accidentally view, declare immediately and that table's comparison is voided

---

## 4. Review Process (Per Table)

### Step 1: Discovery (5-10 min)

For each table:
1. Query `sys.columns` for table schema
2. Query `sys.indexes` for index metadata
3. Query `sys.foreign_keys` for relationships
4. Locate Entity code (if exists) — read mapping
5. Check F_TENANT_ID, F_DELETE_MARK, F_ENABLED_MARK presence
6. Note: Row count from `sys.dm_db_partition_stats`

### Step 2: Seven-Dimension Assessment (15-25 min)

For each of A-G dimensions:
- Determine: Finding OR Explicit No-Finding
- Add evidence tag
- Add evidence detail

**Special attention**:
- D (Lifecycle): Look for custom state machine patterns (F_STATE, F_STATUS, etc.)
- F (DDD): Identify aggregate boundary; note projection vs aggregate
- G (Consumer/Target): Check for Foundry Profile mismatches

### Step 3: Risk Classification (5 min)

Choose R0/R1 / R2 / R3+:
- Don't use column count alone
- Don't use FK count alone
- Consider: aggregate clarity, application coupling, query load, pattern divergence

### Step 4: Hard Gate (5 min)

For each HG#1-5:
- Triggered YES / NO
- If YES, document reason

### Step 5: Recommended Action + Closure (5 min)

Action: No-change / Safe Refactor / Human Decision / Deferred
Closure: NO-CHANGE / READY / REFACTORED / DEFERRED / BLOCKED

### Step 6: Submission (5 min)

Save to `docs/universal/Phase-8/p8-a/shadow/track-b/{NN}-{table-name}-track-b.md`
Sign submission confirmation.

---

## 5. Time Budget (Per Table)

| Step | Expected |
|---|---|
| Discovery | 5-10 min |
| 7-dim assessment | 15-25 min |
| Risk classification | 5 min |
| Hard Gate | 5 min |
| Action + Closure | 5 min |
| Submission | 5 min |
| **Total per table** | **40-55 min** |

For 5 tables: ~3.5-4.5 hours total (single reviewer)

---

## 6. Evidence Discipline

### 6.1 Tag Usage

| Tag | Use when |
|---|---|
| `[KNOWN]` | Direct DB / code observation (highest confidence) |
| `[COMPUTED]` | Derived from known data (medium confidence) |
| `[INFERRED]` | Pattern-based inference (lower confidence) |
| `[GUESS]` | Unverified assumption (lowest confidence — avoid if possible) |
| `[DESIGN]` | Recommendation, not observation |

### 6.2 Stop Rule

Stop collecting evidence when:
- You can justify the dimension assessment
- Adding more evidence doesn't change conclusion
- Sufficient for Risk / Hard Gate / Closure decision

Do NOT over-collect evidence.

---

## 7. Common Bias Avoidance

### 7.1 Confirmation Bias

Risk: "I expect this table to have problems, so I see problems."
Mitigation: Explicit No-Finding is a valid, complete assessment.

### 7.2 Over-Correction Bias

Risk: "AI might have found something, so I should find something too."
Mitigation: NO-CHANGE is a valid, complete closure.

### 7.3 Risk Inflation Bias

Risk: "More risk = safer recommendation."
Mitigation: Use Confidence levels; R0/R1 is acceptable for simple tables.

### 7.4 Under-Correction Bias

Risk: "I'll just agree with whatever AI likely said."
Mitigation: This is exactly what Blind Review prevents.

---

## 8. After All 5 Tables Submitted

### 8.1 Comparison Phase

AI Engineer (not reviewer) executes comparison:
- Per dimension: AGREEMENT / SAFE DISAGREEMENT / AI FALSE POSITIVE / AI FALSE NEGATIVE / RISK ERROR / GATE ERROR / CLOSURE ERROR
- Per table: Aggregate divergence classification
- Cumulative: 4 hard safety metrics

### 8.2 Reviewer Not Involved in Comparison

Reviewer's job ends at Track B submission. Comparison is independent.

---

## 9. Violations

If reviewer discovers they viewed Track A before completing Track B:

```
IMMEDIATELY DECLARE:
"I viewed AI Track A for table [N] before/during Track B.
This Track B is voided. A new Track B review is required."

Declaration Date: _______________
Reviewer: _______________
```

---

## 10. Reviewer Checklist (Pre-submission)

For each table, before saving:

```
[ ] All 7 dimensions assessed (Finding OR Explicit No-Finding)
[ ] Evidence tags used appropriately
[ ] Risk level selected with rationale
[ ] Confidence level indicated
[ ] All 5 Hard Gates evaluated
[ ] Recommended Action chosen
[ ] Recommended Closure chosen
[ ] Routing observations (if any)
[ ] Submission confirmation signed
[ ] File saved with correct naming
[ ] NOT modified AI Track A
```

---

## 11. Reviewer Compensation / Time Tracking

Reviewer should record:

| Table | Review Start | Review End | Duration | Notes |
|---|---|---|---|---|
| 01 | | | | |
| 02 | | | | |
| 03 | | | | |
| 04 | | | | |
| 05 | | | | |

Total review time is reported in P8-A.5 Productivity Baseline.

---

## 12. Protocol Violation Handling

| Violation | Action |
|---|---|
| Viewed Track A before Track B | Void that table's Track B; require fresh review |
| Multiple people cross-contaminating | Void affected tables; restart |
| Track B modified after AI Comparison begun | Void Track B; restart |
| Track B references AI findings | Void Track B; restart |

---

## 13. End of Protocol

This protocol is binding for P8-A.3 Human Blind Review.

Modifications require Phase Gate approval.
