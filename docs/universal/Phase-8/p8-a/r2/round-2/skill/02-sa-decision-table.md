# R2 Round 2 — Table 02 — sa_decision_table — Skill Result A

> **Date**: 2026-08-30
> **Reviewer type**: table-refactor-expert Skill (Result A)
> **Source evidence**: `p8-a/r2/round-2/evidence/SOURCE-EVIDENCE.md`

---

## 1. Table Overview

- **Name**: sa_decision_table
- **Module**: inteAssistant-SA-output
- **Entity**: **NONE — Dynamic SQL only**
- **Row count**: 172 rows (highest among SA tables)
- **Tenant**: NO (SA-output pattern)
- **SoftDelete**: NO
- **FKs in/out**: 0 INCOMING, **2 OUTGOING** (to sa_data_dictionary via dict_id, to sa_pspec via pspec_id)
- **Special**: 172 rows = active SA decision data. Highest SA-output volume.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | NO entity file. Inferred schema: F_Id, dict_id (FK), pspec_id (FK), name, content, conditions, version. ~10-20 columns. SA-output pattern. | P8-0 §2 | [GUESS] for specifics, [KNOWN] for FKs |
| **B Integrity** | 2 OUTGOING FKs enforce referential integrity to sa_data_dictionary and sa_pspec. **No incoming FKs** = sa_decision_table is referenced by no other table (it's a leaf in the FK graph). | P8-0 §4.1 | [KNOWN] |
| **C Index** | Critical hot paths: 1) FK JOINs from sa_decision_table → sa_data_dictionary via dict_id (likely needs index on dict_id); 2) FK JOINs from sa_decision_table → sa_pspec via pspec_id (likely needs index). | FK analysis | [INFERRED] |
| **D Lifecycle** | SA-output pattern: ephemeral, per-pipeline. No standard lifecycle. | SA-output pattern | [INFERRED] |
| **E CRUD/Query** | Hot paths: queries by dict_id (JOIN to sa_data_dictionary), queries by pspec_id (JOIN to sa_pspec), name/version lookups. | FK analysis | [INFERRED] |
| **F DDD** | Aggregate: DecisionTable. References external aggregates (Dictionary, ProcessSpec). Leaf entity in SA domain. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | SA pipeline reads decision tables by dict_id. Foundry Target: needs efficient FK JOIN support. **Note: no incoming FKs = no downstream tables depend on this**. | FK topology | [INFERRED] |

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: HIGH
- **Rationale**:
  - 2 outgoing FKs to critical SA aggregates (data_dictionary, pspec)
  - 172 rows = active data, higher than other SA tables
  - No entity = Skill cannot determine application access pattern
  - SA-output pattern (no tenant, no soft delete)

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason |
|----|-----------|--------|
| HG#1 Tenant Isolation | NO | SA-output pattern. |
| HG#2 Data Integrity | NO | 2 DB FKs enforce integrity. No orphan risk from this side. |
| HG#3 Migration | NO | Only ADD INDEX proposed. |
| HG#4 Cross-Module | **YES triggered** | 2 outgoing FKs to critical SA aggregates. SA pipeline references both. Cross-module concern within SA module. |
| HG#5 Business Ambiguity | NO | SA-output pattern documented. |

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — pending evidence
- **Closure**: DEFERRED

### Why Defer

Same as sa_business_process: no entity → undefined access pattern. Plus: this is a leaf in FK graph, so index changes here don't affect downstream — **but** changes to referenced tables (sa_data_dictionary, sa_pspec) do affect this.

### Required Evidence

1. SQL query for existing indexes
2. SQL query for actual JOIN patterns (sa_data_dictionary ← sa_decision_table ← sa_pspec)
3. Architectural review: should dict_id and pspec_id both be indexed?

### Potential Indexes (deferred)

```sql
-- Index 1: FK to sa_data_dictionary
CREATE NONCLUSTERED INDEX IDX_SA_DT_DICT ON sa_decision_table (dict_id);

-- Index 2: FK to sa_pspec
CREATE NONCLUSTERED INDEX IDX_SA_DT_PSPEC ON sa_decision_table (pspec_id);
```

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §3.2
  - P8-0 §4.1 (FK list — confirmed sa_decision_table has 2 outgoing)
  - P8-0 §5.1 (SA-output pattern)
- **Evidence tags used**: [KNOWN] 2, [INFERRED] 4, [GUESS] 1
- **Stop condition met**: PARTIAL

---

## 7. State Machine Status

```
Current State: DEFERRED (HUMAN APPROVAL required)
```

---

**Skill Result A complete for Table 02 — sa_decision_table**
