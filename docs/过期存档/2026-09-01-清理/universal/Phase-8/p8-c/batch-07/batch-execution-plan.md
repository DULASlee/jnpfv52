# P8-C Batch 07 — Execution Plan & DDL Summary

> **Phase**: 8 — P8-C Production (UNLOCKED 2026-08-30)
> **Batch**: 07
> **Status**: PLAN COMPLETE — READY FOR EXECUTION
> **Date**: 2026-08-30
> **Composition**: 6 tables (workflow-engine, all PRODUCT_CORE)
> **Pre-flight**: PASS ✅ (see `PRE-FLIGHT.md`)

---

## 1. Executive Summary

```
Batch 07: PLAN COMPLETE ✅ (Pre-flight PASS)

Composition: 6 tables (workflow-engine, flow_*)
  01 flow_task_node        R2  — 3 indexes
  02 flow_task_operator    R2  — 4 indexes
  03 flow_template         R2  — 2 indexes
  04 flow_form             R2  — 3 indexes
  05 flow_delegate         R2  — 3 indexes
  06 flow_candidates       R2  — 2 indexes

Total Indexes: 16 across 6 tables
HGs Triggered: 0
DB Writes Required: 16 ADD INDEX (additive only)
Schema Changes: 0
Data Migration: 0

Execution Mode: AUTHORIZED (Production READY)
Pre-flight Mechanical Gate: PASS (all 6 tables IN_SCOPE)
```

---

## 2. Pre-Execution Verification (BEFORE any DB write)

For each table, MUST verify:

```sql
-- Per-table column verification
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'flow_task_node' -- repeat for each
ORDER BY ORDINAL_POSITION;

-- Per-table existing indexes
SELECT i.name AS IndexName, COL_NAME(ic.object_id, ic.column_id) AS ColumnName
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('flow_task_node');

-- Per-table existing FKs (informational, no FKs expected for workflow tables)
SELECT OBJECT_NAME(fk.parent_object_id) AS TableName, fk.name AS FK_Name
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('flow_task_node');
```

**If verification fails** (schema differs significantly from assessment):
- Stop execution
- Update assessment
- Re-plan
- Document discrepancy

---

## 3. Execution Order

Per Master Plan dependency order:

```
Step 1: flow_task_node (foundation — workflow nodes)
Step 2: flow_task_operator (depends on flow_task_node)
Step 3: flow_template (independent — workflow templates)
Step 4: flow_form (depends on flow_template)
Step 5: flow_delegate (independent — delegation)
Step 6: flow_candidates (depends on flow_task_node, flow_task_operator)
```

Each step: PRE-FLIGHT → EXECUTE → VERIFY → CLOSED.

---

## 4. Execution Instructions

### 4.1 Idempotency Guarantee

All 16 indexes use `IF NOT EXISTS` guard. Re-running the SQL is safe.

### 4.2 Execution Command (DBA or AI Engineer with DB access)

```bash
# Execute via SQL Server Management Studio, sqlcmd, or Azure Data Studio:
sqlcmd -S (local)\SQLEXPRESS -d ZXAF_V1_DevTest1 -i batch-07-add-index.sql
```

### 4.3 Post-Execution Verification

```sql
-- Verify all 16 indexes created
SELECT i.name AS IndexName, OBJECT_NAME(i.object_id) AS TableName
FROM sys.indexes i
WHERE i.name IN (
    'IDX_TASKNODE_TASK', 'IDX_TASKNODE_STATE', 'IDX_TASKNODE_NODECODE',
    'IDX_TASKOPERATOR_TASK', 'IDX_TASKOPERATOR_NODE', 'IDX_TASKOPERATOR_HANDLE', 'IDX_TASKOPERATOR_STATE',
    'IDX_TEMPLATE_ENCODE', 'IDX_TEMPLATE_CATEGORY',
    'IDX_FLOWFORM_ENCODE', 'IDX_FLOWFORM_CATEGORY', 'IDX_FLOWFORM_FLOWID',
    'IDX_DELEGATE_USER', 'IDX_DELEGATE_TOUSER', 'IDX_DELEGATE_FLOW',
    'IDX_CANDIDATES_TASK', 'IDX_CANDIDATES_HANDLE'
)
ORDER BY TableName, IndexName;

-- Verify row counts unchanged
SELECT 'flow_task_node' AS TableName, COUNT(*) AS Rows FROM flow_task_node
UNION ALL SELECT 'flow_task_operator', COUNT(*) FROM flow_task_operator
UNION ALL SELECT 'flow_template', COUNT(*) FROM flow_template
UNION ALL SELECT 'flow_form', COUNT(*) FROM flow_form
UNION ALL SELECT 'flow_delegate', COUNT(*) FROM flow_delegate
UNION ALL SELECT 'flow_candidates', COUNT(*) FROM flow_candidates;
```

---

## 5. Risk Per Table

| Table | Risk | HGs | Action | Closure | New Indexes |
|-------|------|-----|--------|---------|-------------|
| flow_task_node | R2 | 0 | REFACTORED | CLOSED | 3 |
| flow_task_operator | R2 | 0 | REFACTORED | CLOSED | 4 |
| flow_template | R2 | 0 | REFACTORED | CLOSED | 2 |
| flow_form | R2 | 0 | REFACTORED | CLOSED | 3 |
| flow_delegate | R2 | 0 | REFACTORED | CLOSED | 3 |
| flow_candidates | R2 | 0 | REFACTORED | CLOSED | 2 |

**Total**: 16 indexes, all REFACTORED, all expected to CLOSE.

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

---

## 7. Closure Documentation

After execution, produce:
- `batch-07-closure.md` (overall batch closure)
- `table-01-flow-task-node/evidence.md` (per-table)
- ... (one per table)
- `batch-07-execution-evidence.md` (consolidated evidence)

Update `Production-Progress-Ledger.md`:
- EXECUTED += 6
- PREPARED -= 6
- Progress: 36 / 274 = 13.1%

---

**Plan Status**: COMPLETE — READY FOR EXECUTION
**Pre-flight**: PASS (see PRE-FLIGHT.md)
**Authorization**: Chief Architect directive 2026-08-30
**Next Action**: Execute `batch-07-add-index.sql`
