# P8-C.1 — Production Scope Classification Framework

> **Phase**: 8 — P8-C.1 (New sub-phase, inserted before scale)
> **Status**: ACTIVE (2026-08-30)
> **Decision Authority**: Chief Architect (Phase Gate Decision)
> **Replaces**: Implicit "289 = Production Universe" assumption

---

## 1. Why This Step Exists

The previous assumption was:

> `289 physical tables = 289 Production Refactoring Tables`

This is **incorrect**. The 289 tables include:
- Real JNPF platform production tables
- Built-in extension sample modules (ext_*, wform_*)
- Demo/Sample tables (Demo_*)
- Snowflake-ID generated test tables (mt*)
- Backup tables (_BAK_*)
- SQL Server system metadata (sysdiagrams)

Optimizing demo/test tables as if they were production assets is **wasteful engineering** and pollutes refactoring results with non-product data.

**Correction**: 289 = **Physical Table Inventory**, NOT **Production Refactoring Universe**.

---

## 2. Classification Categories

### 2.1 Scope Classification

| Code | Name | Definition | Refactor Eligibility |
|---|---|---|---|
| **A** | PRODUCT_CORE | JNPF platform runtime / official business capability dependency | IN_SCOPE (default) |
| **B** | SYSTEM_TEMPLATE | Platform pre-built templates, initialization templates, reusable business modules | CONDITIONAL |
| **C** | DEMO_SAMPLE | Demo / sample / teaching / showcase business tables | OUT_OF_SCOPE (default) |
| **D** | TEST_FIXTURE | Test data, integration fixtures, benchmark tables | OUT_OF_SCOPE |
| **E** | TEMPORARY | Temporary, experimental, one-off tables | OUT_OF_SCOPE |
| **U** | UNKNOWN | Cannot determine with available evidence | Human Decision required |

### 2.2 Refactor Eligibility

| Code | Eligibility | Meaning |
|---|---|---|
| **1** | IN_SCOPE | Must enter production refactoring |
| **2** | CONDITIONAL | Eligible if product value confirmed (Human Decision) |
| **3** | OUT_OF_SCOPE | Skip Phase 8 |
| **4** | BLOCKED | Cannot refactor (external dependency / data integrity) |

---

## 3. Classification Rules

### 3.1 PRODUCT_CORE (A)

A table is **PRODUCT_CORE** if **at least 2** of the following are true:
- [E1] JNPF official Entity mapping exists (`[SugarTable]` attribute in `JNPF.*.Entitys`)
- [E2] Referenced by JNPF official Repository / Service / Controller
- [E3] Required by JNPF platform startup / module initialization
- [E4] Has tenant_id (multi-tenant aware) AND non-zero row count
- [E5] Name follows JNPF standard convention (`base_*`, `BASE_*`, `flow_*`, `sa_*`, `ai_*`, `kg_*`, `BASE_IR_*`, `BASE_REPORT`, `blade_visual*`, `report_*`, `WH_*`, `WM_*`, `BASE_AI_*`, `BASE_KNOWLEDGE_*`)

### 3.2 SYSTEM_TEMPLATE (B)

A table is **SYSTEM_TEMPLATE** if:
- [E6] Name pattern: `wform_*`, `ext_*`, `BASE_AI_PROMPT_TEMPLATE`, `BASE_AI_UI_TEMPLATE`
- [E7] Pre-built business module / business scenario template
- [E8] May be regenerated per tenant deployment

### 3.3 DEMO_SAMPLE (C)

A table is **DEMO_SAMPLE** if **any** of the following:
- [E9] Name starts with `Demo_` (explicit demo prefix)
- [E10] Name pattern: `ext_table_example` (explicit "Example" suffix)
- [E11] Comment/description mentions "示例", "demo", "sample", "教学"
- [E12] Zero row count AND non-core entity

### 3.4 TEST_FIXTURE (D)

A table is **TEST_FIXTURE** if:
- [E13] Name starts with `mt` followed by Snowflake ID (e.g., `mt543406707183714245`)
- [E14] Name contains `_BAK_`, `_backup_`, `_fixture_`, `_test_`
- [E15] SQL Server system tables (`sysdiagrams`)
- [E16] Created by test migration (would require migration log)

### 3.5 TEMPORARY (E)

A table is **TEMPORARY** if:
- [E17] Name starts with `temp_`, `#`, or `tmp_`
- [E18] Created in last 30 days AND no Entity mapping (manual/script-created)

### 3.6 UNKNOWN (U)

A table is **UNKNOWN** if:
- None of the above rules match with HIGH confidence
- Conflicting evidence exists
- Human Decision required

---

## 4. Evidence Priority (for classification)

| Tier | Source | Confidence |
|---|---|---|
| 1 | Production code references (Repository/Service/Controller) | HIGHEST |
| 2 | Entity mapping + JNPF official code | HIGH |
| 3 | Migration / Module registration | MEDIUM-HIGH |
| 4 | Naming conventions + Module hints | MEDIUM |
| 5 | Row count + Tenant presence | LOW-MEDIUM |
| 6 | Directory / Comments | LOW |

If evidence at Tier 1-3 is ambiguous → Tier 4-6 used → if still ambiguous → UNKNOWN.

---

## 5. Evidence Collection (One-time SQL)

For each of the 289 tables, gather:

```sql
SELECT
    t.TABLE_NAME,
    t.TABLE_TYPE,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME) AS Cols,
    (SELECT COUNT_BIG(*) FROM sys.dm_db_partition_stats ps
        WHERE ps.object_id = OBJECT_ID(t.TABLE_NAME) AND ps.index_id IN (0,1)) AS RowCount,
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(t.TABLE_NAME) AND name LIKE 'PK[_]%')
        THEN 1 ELSE 0 END AS HasPK,
    CASE WHEN t.TABLE_NAME LIKE 'base[_]%' OR t.TABLE_NAME LIKE 'BASE[_]%' THEN 1 ELSE 0 END AS PrefBase,
    CASE WHEN t.TABLE_NAME LIKE 'ext[_]%' THEN 1 ELSE 0 END AS PrefExt,
    CASE WHEN t.TABLE_NAME LIKE 'Demo[_]%' OR t.TABLE_NAME LIKE 'demo[_]%' THEN 1 ELSE 0 END AS PrefDemo,
    CASE WHEN t.TABLE_NAME LIKE 'mt%' AND LEN(t.TABLE_NAME) > 15 THEN 1 ELSE 0 END AS PrefMt,
    CASE WHEN t.TABLE_NAME LIKE 'wform[_]%' THEN 1 ELSE 0 END AS PrefWform,
    CASE WHEN t.TABLE_NAME LIKE 'WH[_]%' OR t.TABLE_NAME LIKE 'WM[_]%' THEN 1 ELSE 0 END AS PrefWH,
    CASE WHEN t.TABLE_NAME LIKE 'sa[_]%' OR t.TABLE_NAME LIKE 'ai[_]%' OR t.TABLE_NAME LIKE 'kg[_]%' THEN 1 ELSE 0 END AS PrefAI,
    CASE WHEN t.TABLE_NAME LIKE 'flow[_]%' THEN 1 ELSE 0 END AS PrefFlow,
    CASE WHEN t.TABLE_NAME LIKE 'blade[_]%' OR t.TABLE_NAME LIKE 'report%' THEN 1 ELSE 0 END AS PrefVisual,
    CASE WHEN t.TABLE_NAME LIKE '%[_]BAK[_]%' OR t.TABLE_NAME LIKE '%[_]backup%' THEN 1 ELSE 0 END AS PrefBAK,
    CASE WHEN t.TABLE_NAME = 'sysdiagrams' THEN 1 ELSE 0 END AS PrefSys,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME AND c.COLUMN_NAME = 'f_tenant_id') THEN 1 ELSE 0 END AS HasTenant
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_SCHEMA = 'dbo' AND t.TABLE_TYPE = 'BASE TABLE';
```

This produces evidence for heuristic-based classification.

---

## 6. Decision Flow

```
Physical Table (289)
   ↓
Gather Evidence (SQL above)
   ↓
Apply Heuristic Rules (Section 3)
   ↓
┌──────────────────────────┐
│ Confidence HIGH?          │
│ ├─ Yes → Final Class      │
│ └─ No  ↓                  │
└──────────────────────────┘
   ↓
Code Reference Check
   ↓
┌──────────────────────────┐
│ Code evidence?            │
│ ├─ Yes → Final Class      │
│ └─ No  ↓                  │
└──────────────────────────┘
   ↓
UNKNOWN
   ↓
Human Decision
```

---

## 7. Output Artifacts

After P8-C.1 completion:

### 7.1 Production Scope Registry

| Field | Description |
|---|---|
| Table | Physical table name |
| Module | Inferred module |
| Entity | Entity file path (if exists) |
| Row Count | Current row count |
| Has Tenant | Y/N (f_tenant_id present) |
| Classification | A/B/C/D/E/U |
| Eligibility | 1/2/3/4 |
| Evidence | Brief evidence summary |
| Reason | 1-3 sentences |
| Confidence | HIGH / MEDIUM / LOW |
| Classified By | AI / Human |

### 7.2 Production Universe Freeze

```
Physical Inventory:   289
Classified:          N
IN_SCOPE (1):        N  ← Production Refactoring Universe
CONDITIONAL (2):     N  ← Needs Human Decision
OUT_OF_SCOPE (3):    N  ← Skipped (Demo/Test/Backup)
BLOCKED (4):         N
UNKNOWN (U):         N  ← Needs Human Classification
```

### 7.3 Progress Metric Correction

**Old metric** (deprecated):
```
94 / 289 = 32.53%
```

**New metrics** (post P8-C.1):
```
Production Universe:  N
Closed:               N
Progress:             N / N = N%
Physical Inventory:   289  (kept for reference)
Out-of-Scope:         N    (excluded from production)
```

---

## 8. What This Step Does NOT Do

P8-C.1 explicitly EXCLUDES:
- ❌ Schema optimization
- ❌ Index optimization
- ❌ FK analysis
- ❌ DDD analysis
- ❌ Query audit
- ❌ Migration
- ❌ Data modification

Only: **Scope Classification**.

---

## 9. Exit Criteria

P8-C.1 completes when:
- [ ] All 289 physical tables have a Classification
- [ ] All classifications have Evidence and Reason
- [ ] UNKNOWN tables are explicitly listed
- [ ] Production Scope Registry is consistent (Classification + Eligibility always paired)
- [ ] No schema changes (read-only)
- [ ] No refactoring (only classification)
- [ ] No new audits (lightweight step)

Then: **PRODUCTION UNIVERSE FROZEN**.

---

## 10. Batch Rule (Forward-Looking)

After P8-C.1:
- Batches may only include tables with Eligibility = 1 (IN_SCOPE)
- Tables with Eligibility = 2 (CONDITIONAL) require explicit user approval per batch
- Tables with Eligibility = 3 (OUT_OF_SCOPE) are skipped permanently
- Tables with Eligibility = 4 (BLOCKED) require investigation
- UNKNOWN tables block batch selection until classified

---

## 11. Audit Trail

- **Decision date**: 2026-08-30
- **Decision maker**: Chief Architect (user)
- **Reason**: Implicit 289-as-Production assumption was incorrect
- **Impact**: Recalibrates progress metric; restricts future batches to production tables only
- **Reversal**: Can be revisited if new evidence emerges
