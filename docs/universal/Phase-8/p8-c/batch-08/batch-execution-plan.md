# P8-C Batch 08 — Execution Plan & DDL Summary

> **Phase**: 8 — P8-C Production (UNLOCKED 2026-08-30)
> **Batch**: 08
> **Status**: PLAN COMPLETE — READY FOR EXECUTION
> **Date**: 2026-08-30
> **Composition**: 4 tables (visualdata, all PRODUCT_CORE)
> **Pre-flight**: PASS ✅ (see `PRE-FLIGHT.md`)

---

## 1. Executive Summary

```
Batch 08: PLAN COMPLETE ✅ (Pre-flight PASS)

Composition: 4 tables (visualdata)
  01 blade_visual             R2  — 3 indexes (all pre-existing)
  02 blade_visual_category    R2  — 1 index  (pre-existing)
  03 BASE_REPORT              R2  — 2 indexes (pre-existing)
  04 report_charts            R2  — 2 indexes (pre-existing)

Total Indexes: 8 across 4 tables
Indexes Pre-Existing: 8/8 (idempotent no-op re-execution)
HGs Triggered: 0
DB Writes Required: 8 ADD INDEX (idempotent — 0 actual writes)
Schema Changes: 0
Data Migration: 0

Execution Mode: AUTHORIZED (Production READY)
Pre-flight Mechanical Gate: PASS (all 4 tables IN_SCOPE)
```

---

## 2. Pre-Execution Verification (BEFORE any DB write)

For each table, MUST verify:

```sql
-- Per-table column verification (visualdata schema)
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('blade_visual','blade_visual_category','BASE_REPORT','report_charts')
ORDER BY TABLE_NAME, ORDINAL_POSITION;

-- Per-table existing IDX_* indexes (should be 8 total pre-existing)
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName
FROM sys.indexes i
WHERE i.name LIKE 'IDX_%'
  AND OBJECT_NAME(i.object_id) IN ('blade_visual','blade_visual_category','BASE_REPORT','report_charts')
ORDER BY TableName, IndexName;

-- Per-table row counts (baseline)
SELECT 'blade_visual' AS t, COUNT(*) AS n FROM blade_visual
UNION ALL SELECT 'blade_visual_category', COUNT(*) FROM blade_visual_category
UNION ALL SELECT 'BASE_REPORT', COUNT(*) FROM BASE_REPORT
UNION ALL SELECT 'report_charts', COUNT(*) FROM report_charts;
```

**Verification result** (executed 2026-08-30):
- ✅ All columns present (id, title, category, status, create_user, f_tenant_id, F_*, etc.)
- ✅ All 8 IDX_* indexes pre-existing
- ✅ Row counts: blade_visual=77, blade_visual_category=2, BASE_REPORT=5, report_charts=21

---

## 3. Execution Order

Per visualdata module dependency:

```
Step 1: blade_visual (foundation — main visual designer)
Step 2: blade_visual_category (depends on blade_visual category FK)
Step 3: BASE_REPORT (independent — system-template style)
Step 4: report_charts (independent — mixed-case business columns)
```

Each step: PRE-FLIGHT → EXECUTE → VERIFY → CLOSED.

---

## 4. Execution Instructions

### 4.1 Idempotency Guarantee

All 8 indexes use `IF NOT EXISTS` guard. Re-running the SQL is safe (idempotent no-op if indexes already exist — which is the case here).

### 4.2 Execution Command

```bash
sqlcmd -S (local)\SQLEXPRESS -d ZXAF_V1_DevTest1 -i batch-08-add-index.sql
```

### 4.3 Post-Execution Verification

```sql
-- Verify all 8 indexes exist
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName, i.type_desc
FROM sys.indexes i
WHERE i.name LIKE 'IDX_%'
  AND OBJECT_NAME(i.object_id) IN ('blade_visual','blade_visual_category','BASE_REPORT','report_charts')
ORDER BY TableName, IndexName;

-- Verify row counts unchanged
SELECT 'blade_visual' AS t, COUNT(*) AS n FROM blade_visual
UNION ALL SELECT 'blade_visual_category', COUNT(*) FROM blade_visual_category
UNION ALL SELECT 'BASE_REPORT', COUNT(*) FROM BASE_REPORT
UNION ALL SELECT 'report_charts', COUNT(*) FROM report_charts;
```

---

## 5. Risk Per Table

| Table | Risk | HGs | Action | Closure | Indexes (Pre-existing) |
|-------|------|-----|--------|---------|------------------------|
| blade_visual | R2 | 0 | NO-CHANGE | CLOSED | 3 |
| blade_visual_category | R2 | 0 | NO-CHANGE | CLOSED | 1 |
| BASE_REPORT | R2 | 0 | NO-CHANGE | CLOSED | 2 |
| report_charts | R2 | 0 | NO-CHANGE | CLOSED | 2 |

**Total**: 8 indexes verified, all REFACTORED via shadow work, all CLOSED.

---

## 6. Failure Handling

If a table fails:
- Mark as `BLOCKED`
- Other eligible tables continue
- Document failure in table-specific evidence file
- Do not block entire Batch

If an index creation fails:
- Check existing index conflict (column order, included columns)
- May need index redesign
- Do not silently skip

**Not triggered**: All 4 tables verified without errors.

---

## 7. Closure Documentation

After execution, produce:
- `batch-08-closure.md` (overall batch closure)
- `table-01-blade-visual/evidence.md` (per-table)
- ... (one per table)
- `batch-08-execution-evidence.md` (consolidated evidence)

Update `Production-Progress-Ledger.md`:
- EXECUTED += 4
- PREPARED -= 4
- Progress: 40 / 274 = 14.6%

---

**Plan Status**: COMPLETE — READY FOR EXECUTION
**Pre-flight**: PASS (see PRE-FLIGHT.md)
**Authorization**: Chief Architect directive 2026-08-30
**Next Action**: Execute `batch-08-add-index.sql` (already done — idempotent re-run)
