# R2 Round 2 — Table 01 — sa_business_process — Skill Result A

> **Date**: 2026-08-30
> **Reviewer type**: table-refactor-expert Skill (Result A)
> **Source evidence**: `p8-a/r2/round-2/evidence/SOURCE-EVIDENCE.md`

---

## 1. Table Overview

- **Name**: sa_business_process
- **Module**: inteAssistant-SA-output (SA materialization)
- **Entity**: **NONE — Dynamic SQL only** (per P8-0 registry §2)
- **Row count**: 19 rows
- **Tenant**: NO (per P8-0 §5.1 — SA-output pattern, no tenant)
- **SoftDelete**: NO (per P8-0 §5.1)
- **FKs in/out**: **4 INCOMING** (sa_data_dictionary, sa_pspec, sa_state_machine, sa_ui), 1 OUTGOING (to sa_dfd via dfd_id)
- **Special**: **FK HUB** — most-incoming-FK table in inteAssistant SA module (tied with sa_data_dictionary at 5, but sa_data_dictionary has 5)

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | NO entity file. Inferred schema (based on FKs + SA pattern): F_Id, dfd_id (FK to sa_dfd), name, version, description, content, creator fields. ~15-25 columns typical. NO F_TenantId (SA-output pattern). NO F_DeleteMark (SA-output pattern). | P8-0 §2; FK evidence | [GUESS] for schema specifics, [KNOWN] for FKs |
| **B Integrity** | 4 INCOMING FKs = strong integrity enforcement (cascading deletes blocked at DB level). 1 OUTGOING FK to sa_dfd. Orphan protection from DB. | P8-0 §4.1 (incoming FK list) | [KNOWN] |
| **C Index** | **Critical question**: does sa_business_process have indexes supporting its 4 incoming FKs? If 4 different tables (sa_data_dictionary, sa_pspec, sa_state_machine, sa_ui) JOIN to sa_business_process.id, then **sa_business_process.id is the JOIN target** (already PK, indexed). But sa_business_process.dfd_id (outgoing FK) likely needs index. | FK analysis | [INFERRED] |
| **D Lifecycle** | SA-output pattern: ephemeral, per-pipeline. No standard lifecycle (no Created/Modified/Deleted state). | SA-output pattern | [INFERRED] |
| **E CRUD/Query** | Hot paths: 1) FK JOINs from 4 incoming tables → queries by id (PK, indexed). 2) FK JOIN to sa_dfd → queries by dfd_id (likely needs index). 3) SA pipeline queries by name/version. | FK analysis | [INFERRED] |
| **F DDD** | Aggregate: BusinessProcess. FK associations are external. **Hub of SA domain model**. Critical for SA workflow (bpm = business process model). | DDD analysis | [INFERRED] |
| **G Consumer/Target** | **HIGH cross-module consumer**: 4 tables in same SA module reference this. Plus: SA pipeline reads business process definitions. Foundry Target: needs efficient FK JOIN support. | FK topology | [INFERRED] |

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: HIGH
- **Rationale**:
  - FK hub (4 incoming + 1 outgoing) = cross-module impact
  - No entity = Skill cannot make application-layer recommendations
  - 19 rows = low volume but FK traffic could be high
  - SA-output pattern (no tenant, no soft delete) is unusual for JNPF

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason |
|----|-----------|--------|
| HG#1 Tenant Isolation | NO | SA-output pattern has NO tenant by design (P8-0 §5.1). No multi-tenant concern. |
| HG#2 Data Integrity | NO | 4 DB FKs provide strong integrity enforcement. Orphan risk LOW. |
| HG#3 Migration | NO | Only ADD INDEX proposed (no schema change). |
| HG#4 Cross-Module | **YES triggered** | 4 tables in same module + SA pipeline reference this. Cross-module impact confirmed via FK topology. |
| HG#5 Business Ambiguity | NO | SA-output is documented pattern (per-pipeline data). Semantics clear. |

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — requires architectural decision
- **Closure**: DEFERRED — pending evidence acquisition

### Why Defer

1. **No entity** → Skill cannot determine application access pattern
2. **FK hub** → any index change affects 4+ downstream tables
3. **SA pipeline** → SA business process is core SA artifact

### Required Before Action

1. **Acquire SQL evidence**:
   ```sql
   SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('sa_business_process');
   SELECT * FROM sys.dm_db_index_usage_stats WHERE object_id = OBJECT_ID('sa_business_process');
   SELECT TOP 50 * FROM sa_business_process; -- verify schema
   ```

2. **Architectural review**: Is dfd_id properly indexed? Are FK JOINs efficient?

### Potential Index Recommendations (deferred)

```sql
-- Index 1: dfd_id FK lookup (if not already indexed)
CREATE NONCLUSTERED INDEX IDX_SA_BPM_DFD ON sa_business_process (dfd_id);

-- Index 2: SA pipeline search pattern (assumed)
CREATE NONCLUSTERED INDEX IDX_SA_BPM_NAME ON sa_business_process (name, version);
```

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §3.1
  - P8-0 §4.1 (incoming FK list — confirmed sa_business_process has 4 incoming)
  - P8-0 §5.1 (SA-output pattern — no tenant, no soft delete)
  - No entity file found
- **Evidence tags used**: [KNOWN] 2, [INFERRED] 4, [GUESS] 1
- **Stop condition met**: PARTIAL — KNOWN evidence for FKs is sufficient; INFERRED for application access pattern; GUESS for schema. Per Skill §2.2, undefined situation → escalate.

---

## 7. State Machine Status

```
DISCOVERED  → ✅
ASSESSED    → ✅ (with limitations)
DESIGNED    → ⏸ BLOCKED (per Skill §2.2)
READY       → ⏸ BLOCKED
REFACTORED  → ⏸ BLOCKED
VERIFIED    → ⏸ BLOCKED
CLOSED      → ⏸ BLOCKED
```

**Current State**: DISCOVERED → ASSESSED → DEFERRED (Human Approval)

---

**Skill Result A complete for Table 01 — sa_business_process**
