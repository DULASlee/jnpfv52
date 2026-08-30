# R2 Round 2 — Table 02 — sa_decision_table — Expert Result B

> **Date**: 2026-08-30
> **Reviewer type**: Independent AI Expert (Result B)
> **Reviewer session ID**: IAE-R2-2026-08-30
> **Source evidence accessed**: `p8-a/r2/round-2/evidence/SOURCE-EVIDENCE.md`
> **Isolation guarantee**: No reference to `p8-a/r2/round-2/skill/*` (Result A)

---

## 1. Table Overview

- **Name**: sa_decision_table
- **Module**: inteAssistant-SA-output
- **Entity**: **NONE** — dynamic SQL access only
- **Row count**: 172 (highest among SA-output tables)
- **Tenant**: NO (SA-output pattern)
- **SoftDelete**: NO
- **FKs**: **2 OUTGOING** (to sa_data_dictionary via dict_id, to sa_pspec via pspec_id). **0 incoming.**
- **Special**: 172 rows = active SA decision data. Leaf in FK graph.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | NO entity file. Schema inferred from FKs + SA pattern: F_Id, dict_id (FK), pspec_id (FK), name, content, conditions. ~10-20 columns. | P8-0 §2 | [GUESS] for specifics, [KNOWN] for FKs |
| **B Integrity** | 2 OUTGOING FKs to sa_data_dictionary and sa_pspec. **Leaf in FK graph** (no incoming FKs = no downstream tables depend on this). | P8-0 §4.1 | [KNOWN] |
| **C Index** | **Critical question**: are dict_id and pspec_id both indexed? These are JOIN sources for queries going to sa_data_dictionary and sa_pspec. At 172 rows, even without indexes the JOINs may be fast (small data). But correctness matters more than performance here. | FK analysis + leaf pattern | [INFERRED] |
| **D Lifecycle** | SA-output pattern: ephemeral. | SA pattern | [INFERRED] |
| **E CRUD/Query** | Hot paths: 1) JOIN from sa_data_dictionary (by dict_id); 2) JOIN from sa_pspec (by pspec_id); 3) name-based queries. | Leaf + FK pattern | [INFERRED] |
| **F DDD** | Aggregate: DecisionTable. References external aggregates. Leaf entity in SA domain. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | **No incoming FKs** = no downstream tables reference this via FK. SA pipeline reads it but doesn't JOIN. Foundry Target: needs FK indexes for outgoing JOINs. | Leaf topology | [INFERRED] |

### Expert Reasoning Notes

**Leaf vs Hub distinction** is critical:
- sa_business_process = HUB (4 incoming FKs)
- sa_decision_table = LEAF (0 incoming FKs)
- Different risk profiles

**Implication for indexes**:
- HUB tables need indexes on the JOIN target (PK already, may need additional)
- LEAF tables need indexes on the JOIN source (outgoing FK columns)

For sa_decision_table, the question is: **are dict_id and pspec_id indexed?**

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: MEDIUM (lower than sa_business_process because leaf pattern is less ambiguous)
- **Rationale**:
  - 2 outgoing FKs = leaf, less cross-module impact than hub
  - 172 rows = active data
  - No entity = undefined access pattern
  - SA-output pattern

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason | Evidence |
|----|-----------|--------|----------|
| HG#1 Tenant Isolation | NO | SA-output pattern. | P8-0 §5.1 |
| HG#2 Data Integrity | NO | 2 DB FKs enforce integrity. Leaf has no orphan risk. | P8-0 §4.1 |
| HG#3 Migration | NO | No migration proposed. | N/A |
| HG#4 Cross-Module | **YES triggered** | 2 outgoing FKs to sa_data_dictionary and sa_pspec = cross-module concern (within SA module). SA pipeline consumes this. | P8-0 §4.1 |
| HG#5 Business Ambiguity | NO | SA-output pattern documented. | P8-0 §5.1 |

### Expert Note on HG#4

Master Spec §10.3 HG#4 trigger: "cross-module dependency detected (table referenced by 3+ modules via application logic, no DB FK indexes)".

sa_decision_table:
- ✅ 2 outgoing FKs (sa_data_dictionary, sa_pspec)
- ✅ SA pipeline consumes it
- ⚠️ "3+ modules" — only 2 FK targets. SA pipeline = 3rd.

**Verdict: HG#4 triggered** — SA pipeline as the 3rd consumer satisfies the criterion.

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — pending evidence
- **Closure**: DEFERRED

### Why Defer

Same as sa_business_process: no entity → undefined access pattern.

### Required Before Action

1. SQL query for existing indexes (specifically: are dict_id and pspec_id indexed?)
2. Architectural review: which JOIN is more frequent?

### Conditional Recommendation (pending evidence)

If dict_id NOT indexed:
```sql
CREATE NONCLUSTERED INDEX IDX_SA_DT_DICT ON sa_decision_table (dict_id);
```

If pspec_id NOT indexed:
```sql
CREATE NONCLUSTERED INDEX IDX_SA_DT_PSPEC ON sa_decision_table (pspec_id);
```

**Conservative approach**: recommend both, let execution decide based on actual usage stats.

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §3.2
  - P8-0 §4.1 (FK list)
  - P8-0 §5.1 (SA pattern)
- **Evidence tags used**: [KNOWN] 2, [INFERRED] 4, [GUESS] 1
- **Stop condition met**: PARTIAL

---

## 7. Additional Reasoning (Expert Commentary)

### Leaf vs Hub Risk Profile

| Aspect | Hub (sa_business_process) | Leaf (sa_decision_table) |
|--------|---------------------------|-------------------------|
| Incoming FKs | 4 | 0 |
| Outgoing FKs | 1 | 2 |
| Downstream impact | HIGH (4 tables depend) | NONE (no incoming) |
| Index strategy | PK + maybe dfd_id | dict_id + pspec_id |
| Cross-module risk | Incoming joins | Outgoing joins |

Both are R3+ because both have **cross-module concern** (different directions).

### Performance Consideration

At 172 rows, even without indexes, JOINs would be fast. But:
- FKs are correctness mechanisms, not just performance
- Indexes on FK columns are best practice (SQL Server doesn't auto-index FKs)
- Future scaling: sa_decision_table could grow to thousands of rows per pipeline

### Round 2 Test: Does Skill Correctly Distinguish Hub vs Leaf?

This is the structural test. Both Skill and Expert should:
- Recognize sa_business_process as HUB (more cross-module)
- Recognize sa_decision_table as LEAF (less cross-module)
- Apply different (but appropriate) recommendations

---

**Expert Result B complete for Table 02 — sa_decision_table**
