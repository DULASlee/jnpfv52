# P8-B Batch 01 — Execution Plan & DDL Summary

> **Phase**: 8 — P8-B Controlled Production
> **Batch**: 01
> **Status**: PLAN COMPLETE — AWAITING ACTUAL DB VERIFICATION BEFORE EXECUTION
> **Date**: 2026-08-30
> **Composition**: 4 tables (base_user excluded per Phase Gate Decision A1)

---

## 1. Executive Summary

```
Batch 01: PLAN COMPLETE ✅

Composition: 4 tables (system-core identity, excluding base_user)
  01 base_organize        R1  — 3 indexes
  02 base_role            R2  — 2 indexes (Risk Floor applied)
  03 base_position        R1  — 2-3 indexes
  04 base_user_relation   R1  — 3 indexes

Total Indexes: 10-11 across 4 tables
HGs Triggered: 0
DB Writes Required: 10-11 ADD INDEX (additive only)
Schema Changes: 0
Data Migration: 0

Execution Mode: Controlled (DB writes allowed)
Pre-execution Requirement: sys.columns / sys.indexes / sys.foreign_keys VERIFICATION
```

---

## 2. Pre-Execution Verification (BEFORE any DB write)

Per Master Plan §4.4.2: "Batch 01 Verification: schema / integrity / migration / query / behavior / rollback 6 维验证".

For each table, MUST run:

```sql
-- Table 01: base_organize
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'BASE_ORGANIZE'
ORDER BY ORDINAL_POSITION;

SELECT i.name AS IndexName, COL_NAME(ic.object_id, ic.column_id) AS ColumnName
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('BASE_ORGANIZE');

SELECT OBJECT_NAME(fk.parent_object_id) AS TableName, fk.name AS FK_Name
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID('BASE_ORGANIZE');
-- Repeat for tables 02-04
```

**Verification produces actual schema, indexes, and incoming FKs** for confirmation against assessments.

**If verification FAILS** (schema differs significantly from assessment):
- Stop execution
- Update assessment
- Re-plan
- Document discrepancy

---

## 3. Execution Order (Dependency-Driven)

```
Step 1: base_organize (no incoming FK from this group, foundation)
Step 2: base_role (independent aggregate)
Step 3: base_position (independent aggregate)
Step 4: base_user_relation (depends on base_user, base_role, base_position)
```

Each step: ASSESSED → DESIGNED → READY → REFACTORED → VERIFIED → CLOSED.

---

## 4. DDL Summary (Per-Table)

### 4.1 Table 01: base_organize

```sql
-- 3 ADD INDEX (additive, no schema change)

CREATE NONCLUSTERED INDEX IDX_ORGANIZE_PARENT
ON BASE_ORGANIZE (F_TENANT_ID, F_PARENT_ID)
WHERE F_DELETE_MARK = 0;  -- include only active rows
GO

CREATE NONCLUSTERED INDEX IDX_ORGANIZE_ENCODE
ON BASE_ORGANIZE (F_TENANT_ID, F_EN_CODE)
WHERE F_DELETE_MARK = 0;
GO

CREATE NONCLUSTERED INDEX IDX_ORGANIZE_CATEGORY
ON BASE_ORGANIZE (F_TENANT_ID, F_CATEGORY)
WHERE F_DELETE_MARK = 0;
GO
```

**Rollback**:
```sql
DROP INDEX IDX_ORGANIZE_PARENT ON BASE_ORGANIZE;
DROP INDEX IDX_ORGANIZE_ENCODE ON BASE_ORGANIZE;
DROP INDEX IDX_ORGANIZE_CATEGORY ON BASE_ORGANIZE;
```

**Verification post-DDL**:
- `SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('BASE_ORGANIZE')` — confirm 4 indexes (PK + 3 new)
- Sample query: `SELECT * FROM BASE_ORGANIZE WHERE F_TENANT_ID = '...' AND F_PARENT_ID = '...'` — confirm uses IDX_ORGANIZE_PARENT

---

### 4.2 Table 02: base_role

```sql
-- 2 ADD INDEX

CREATE NONCLUSTERED INDEX IDX_ROLE_ENCODE
ON BASE_ROLE (F_TENANT_ID, F_EN_CODE)
WHERE F_DELETE_MARK = 0;
GO

CREATE NONCLUSTERED INDEX IDX_ROLE_CATEGORY
ON BASE_ROLE (F_TENANT_ID, F_CATEGORY)
WHERE F_DELETE_MARK = 0;
GO
```

**Rollback**:
```sql
DROP INDEX IDX_ROLE_ENCODE ON BASE_ROLE;
DROP INDEX IDX_ROLE_CATEGORY ON BASE_ROLE;
```

**Verification post-DDL**: Same as above pattern.

---

### 4.3 Table 03: base_position

```sql
-- 2-3 ADD INDEX

CREATE NONCLUSTERED INDEX IDX_POSITION_ORG
ON BASE_POSITION (F_TENANT_ID, F_ORGANIZE_ID)
WHERE F_DELETE_MARK = 0;
GO

CREATE NONCLUSTERED INDEX IDX_POSITION_ENCODE
ON BASE_POSITION (F_TENANT_ID, F_EN_CODE)
WHERE F_DELETE_MARK = 0;
GO

-- IDX_POSITION_LEVEL only if F_LEVEL is hot path (verify first)
-- CREATE NONCLUSTERED INDEX IDX_POSITION_LEVEL
-- ON BASE_POSITION (F_TENANT_ID, F_LEVEL)
-- WHERE F_DELETE_MARK = 0;
```

**Rollback**: Corresponding DROP INDEX statements.

---

### 4.4 Table 04: base_user_relation

```sql
-- 3 ADD INDEX

CREATE NONCLUSTERED INDEX IDX_USERRELATION_USER
ON BASE_USER_RELATION (F_TENANT_ID, F_USER_ID)
WHERE F_DELETE_MARK = 0;
GO

CREATE NONCLUSTERED INDEX IDX_USERRELATION_OBJECT
ON BASE_USER_RELATION (F_TENANT_ID, F_OBJECT_TYPE, F_OBJECT_ID)
WHERE F_DELETE_MARK = 0;
GO

CREATE NONCLUSTERED INDEX IDX_USERRELATION_USER_OBJECT
ON BASE_USER_RELATION (F_TENANT_ID, F_USER_ID, F_OBJECT_TYPE)
WHERE F_DELETE_MARK = 0;
GO
```

**Rollback**:
```sql
DROP INDEX IDX_USERRELATION_USER ON BASE_USER_RELATION;
DROP INDEX IDX_USERRELATION_OBJECT ON BASE_USER_RELATION;
DROP INDEX IDX_USERRELATION_USER_OBJECT ON BASE_USER_RELATION;
```

---

## 5. Per-Table Verification (6 Dimensions)

Per Master Plan §4.6:

| Dimension | Verification Method |
|---|---|
| schema | DDL execution success; sys.indexes confirms new indexes |
| integrity | No FK violations (only ADD INDEX, no FK changes) |
| migration | N/A (additive only, no data movement) |
| query | EXPLAIN PLAN on representative queries |
| application behavior | Application still functions (manual smoke test) |
| rollback/recovery | DROP INDEX tested in dev before prod |

---

## 6. Batch 01 Closure

After all 4 tables VERIFIED:
- Each table → CLOSED
- Batch Verification Record
- Registry update

```
CLOSURE Distribution (Batch 01):
  REFACTORED:    4/4
  NO-CHANGE:     0/4
  DEFERRED:      0/4
  BLOCKED:       0/4
```

---

## 7. Risk Assessment (Batch-Level)

| Risk | Mitigation |
|---|---|
| Verification queries show schema differs from assessment | STOP, re-plan |
| Index conflicts with existing indexes | DROP existing if conflict, or use different name |
| WHERE filter on F_DELETE_MARK = 0 not allowed (if F_DELETE_MARK is bit/int) | Adjust filter syntax |
| Statistics update needed after index creation | Run UPDATE STATISTICS |
| Application uses hint that ignores indexes | Verify with EXPLAIN |

---

## 8. Estimated Duration

| Step | Duration |
|---|---|
| Pre-execution verification (4 tables) | ~30 min |
| DDL execution (4 tables, ~10 indexes) | ~15 min |
| Post-DDL verification | ~20 min |
| Application smoke test | ~15 min |
| Batch closure | ~10 min |
| **Total** | **~1.5 hours** |

---

## 9. Status Tracking

| Table | ASSESSED | DESIGNED | READY | REFACTORED | VERIFIED | CLOSED |
|---|---|---|---|---|---|---|
| 01 base_organize | ✅ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| 02 base_role | ✅ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| 03 base_position | ✅ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |
| 04 base_user_relation | ✅ | ⏳ | ⏳ | ⏳ | ⏳ | ⏳ |

**Status legend**: ✅ = complete; ⏳ = pending; ❌ = blocked.

---

## 10. Next Steps (After Batch 01)

Per Master Plan §4.14:

```
P8-B → P8-C transition requires Stability Gate:
  [ ] Batch 01 closed and verified
  [ ] Batch 02 closed and verified
  [ ] HG FN: 0 in both batches
  [ ] P0/P1 error: 0 in both batches
  [ ] Core contamination: 0 in both batches
  [ ] Rework Rate: not increasing
  [ ] Human Gate Rate: not increasing (N/A for AI-only Batch)
  [ ] Median time: not increasing
  [ ] Tables / AI-hour: not decreasing
```

**Batch 02 planning will follow Batch 01 closure.**

---

## 11. Files Created (Batch 01 So Far)

```
docs/universal/Phase-8/p8-b/
├── skill-calibration-applied.md
└── batch-01/
    ├── batch-plan.md (initial)
    ├── batch-execution-plan.md (this file)
    ├── table-01-organize/evidence.md
    ├── table-02-role/evidence.md
    ├── table-03-position/evidence.md
    └── table-04-user-relation/evidence.md
```

---

## 12. Approval Required

Per Master Plan §14.1 (Phase Gate) and §14.2 (Batch Gate):

```
Batch 01 Plan:        COMPLETE
Pre-execution:        Verification queries required (see §2)
Execution Approval:   PENDING (user approval to execute)
```

**This plan is presented for user approval to proceed with verification + execution.**
