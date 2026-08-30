# R2 Round 2 — Table 01 — sa_business_process — Expert Result B

> **Date**: 2026-08-30
> **Reviewer type**: Independent AI Expert (Result B)
> **Reviewer session ID**: IAE-R2-2026-08-30
> **Source evidence accessed**: `p8-a/r2/round-2/evidence/SOURCE-EVIDENCE.md`
> **Isolation guarantee**: No reference to `p8-a/r2/round-2/skill/*` (Result A)

---

## 1. Table Overview

- **Name**: sa_business_process
- **Module**: inteAssistant-SA-output (SA materialization layer)
- **Entity**: **NONE** — dynamic SQL access only
- **Row count**: 19
- **Tenant**: NO (SA-output pattern)
- **SoftDelete**: NO
- **FKs**: **4 INCOMING** (from sa_data_dictionary, sa_pspec, sa_state_machine, sa_ui); 1 OUTGOING (to sa_dfd)
- **Special**: FK hub for SA module — 4 tables depend on it

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | **NO entity file**. Schema can only be inferred from FKs + SA-output pattern. Expected columns: F_Id, dfd_id (FK), name, version, content, creator metadata. ~15-25 columns typical. **No F_TenantId**, **no F_DeleteMark** (SA-output pattern per registry §5.1). | P8-0 §2; FK evidence; SA pattern | [GUESS] for specifics, [KNOWN] for lack-of-entity + FKs |
| **B Integrity** | **4 INCOMING FKs** = strong DB-level referential integrity. **1 OUTGOING FK** = orphan protection from sa_dfd deletion side. This is **stronger integrity than most JNPF tables** (P8-0 §4.4 noted 14 FKs total, all in SA/KG). | P8-0 §4.1, §4.4 | [KNOWN] |
| **C Index** | Cannot make application-layer recommendations without entity source. **Architectural question**: are FK JOINs efficient? sa_business_process.id is the JOIN target (PK, indexed). sa_business_process.dfd_id (outgoing FK) may or may not be indexed — needs SQL verification. | FK analysis + lack of entity | [INFERRED] |
| **D Lifecycle** | SA-output = per-pipeline data. No Created/Modified/Deleted state. Ephemeral by design. | SA-output pattern | [INFERRED] |
| **E CRUD/Query** | Hot paths inferred: 1) Lookup by id (from 4 incoming FK JOINs) — PK index covers this. 2) FK JOIN to sa_dfd (dfd_id column). 3) SA pipeline: query by name/version (unclear without entity). | FK topology + SA pattern | [INFERRED] |
| **F DDD** | Aggregate root: BusinessProcess. **Central SA domain entity**. FK associations are external references. DDD pattern: Aggregate + 4 referenced entities. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | **4+ tables reference this** (FK-incoming from sa_data_dictionary, sa_pspec, sa_state_machine, sa_ui). Plus SA pipeline consumes it. **Cross-module concern within SA module**. | FK topology | [INFERRED] |

### Expert Reasoning Notes

This is a **textbook FK hub**:
- 4 incoming FKs = strong constraint enforcement
- 1 outgoing FK = dependency on sa_dfd
- **Implication**: indexes on sa_business_process are critical for JOIN performance from 4+ tables

The SA module has all 14 FKs in the entire DB (P8-0 §4.4). sa_business_process is one of the busiest JOIN targets. Even though it's only 19 rows now, FK traffic could be significant.

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: HIGH
- **Rationale**:
  - FK hub (4 incoming + 1 outgoing) → cross-module impact
  - No entity → cannot make application-layer recommendations
  - SA-output pattern (no tenant, no soft delete) is unusual
  - Any change here affects 4+ downstream tables
  - **Requires architectural input** before action

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason | Evidence |
|----|-----------|--------|----------|
| HG#1 Tenant Isolation | NO | SA-output pattern. No tenant by design. | P8-0 §5.1 |
| HG#2 Data Integrity | NO | 4 DB FKs provide strong integrity. NO orphan risk. | P8-0 §4.1 |
| HG#3 Migration | NO | No migration proposed. | N/A |
| HG#4 Cross-Module | **YES triggered** | 4 tables in SA module reference this via FK. SA pipeline references too. Cross-module concern explicit. | P8-0 §4.1 |
| HG#5 Business Ambiguity | NO | SA-output pattern documented in registry. Semantics are clear (per-pipeline ephemeral data). | P8-0 §5.1 |

### Expert Note on HG#4

This is a **definitive cross-module trigger**, not borderline:
- ✅ 4 tables reference this table (sa_data_dictionary, sa_pspec, sa_state_machine, sa_ui)
- ✅ sa_dfd references this (via FK on dfd_id... wait, that's outgoing)
- ✅ SA pipeline consumes this for materialization

**Master Spec §10.3 HG#4**: "cross-module dependency detected (table referenced by 3+ modules via application logic, no DB FK indexes)".

sa_business_process:
- ✅ Referenced by 3+ modules (4 SA tables)
- ✅ "No DB FK indexes" — wait, the FKs ARE the index target. The JOIN source needs the index on the FK column of the referencing table, not on this table.

Let me reconsider. Master Spec §10.3 says "no DB FK indexes" — meaning **the FK column on the referencing table should have an index**. sa_business_process itself has the PK (already indexed). The FK columns on sa_data_dictionary.bpm_id, sa_pspec.bpm_id, etc. should be indexed.

But this table (sa_business_process) is the **target** of those FKs. From this table's perspective, the question is: does it have indexes supporting OUTGOING queries (dfd_id)?

**Verdict: HG#4 triggered** — this table is the cross-module dependency target. Whether to add indexes here depends on the JOIN direction frequency.

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — pending evidence
- **Closure**: DEFERRED

### Why Defer

Per Master Spec §2.2 (No Autonomous Rule Creation), undefined situations escalate. No entity = undefined access pattern.

### Required Before Action

1. **SQL evidence**:
   ```sql
   -- Existing indexes
   SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('sa_business_process');
   
   -- Index usage
   SELECT * FROM sys.dm_db_index_usage_stats 
   WHERE object_id = OBJECT_ID('sa_business_process');
   
   -- Verify schema
   SELECT TOP 50 * FROM sa_business_process;
   ```

2. **Architectural questions**:
   - Is dfd_id indexed? (outgoing FK)
   - What's the typical query pattern?
   - Are 4 incoming FKs sufficient for performance?

### Conditional Recommendation (pending evidence)

If dfd_id is NOT indexed:
```sql
CREATE NONCLUSTERED INDEX IDX_SA_BPM_DFD ON sa_business_process (dfd_id);
```

This is the **only** index I can recommend without entity source evidence. The 4 incoming FKs target sa_business_process.id (already PK). dfd_id is the only outgoing FK that needs verification.

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §3.1
  - P8-0 §4.1 (FK list)
  - P8-0 §4.4 (SA module FK summary)
  - P8-0 §5.1 (SA-output pattern)
  - No entity file found
- **Evidence tags used**: [KNOWN] 3, [INFERRED] 4, [GUESS] 1
- **Stop condition met**: PARTIAL — KNOWN FK evidence is strong; INFERRED for application pattern is acceptable; GUESS for schema is documented as such

---

## 7. Additional Reasoning (Expert Commentary)

### Why FK Hub Tables Are R3+

When a table is referenced by 4+ other tables via FK:
1. **Index changes have widespread impact** — adding/dropping an index affects 4+ downstream queries
2. **Schema changes are risky** — any column change can break FK relationships
3. **Architectural decisions required** — "should this be the hub or should we split?" is a design question

For sa_business_process specifically:
- 19 rows = low volume, but FK traffic could be high (4 tables JOIN here)
- The hub is well-defined and stable — it's not a temporary artifact
- The main risk is **performance degradation** if JOINs are not optimized

### Round 2 Test: Will Skill Correctly Identify FK Hub as R3+?

This is one of the Round 2 priority tests. Both Skill and Expert should:
- Recognize 4 incoming FKs as significant
- Trigger HG#4 (cross-module)
- Recommend R3+ with evidence acquisition
- NOT just add indexes blindly

---

**Expert Result B complete for Table 01 — sa_business_process**
