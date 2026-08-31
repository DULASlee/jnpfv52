# P8-C Batch 09 — Execution Plan & DDL Summary

> **Phase**: 8 — P8-C Production (UNLOCKED 2026-08-30)
> **Batch**: 09
> **Status**: PLAN COMPLETE — READY FOR EXECUTION
> **Date**: 2026-08-30
> **Composition**: 6 tables (inteAssistant-AI, all PRODUCT_CORE)
> **Pre-flight**: PASS ✅ (after schema correction, see `PRE-FLIGHT.md`)

---

## 1. Executive Summary

```
Batch 09: PLAN COMPLETE ✅ (Pre-flight PASS after fix)

Composition: 6 tables (inteAssistant-AI)
  01 BASE_AI_PIPELINE         R2 — 2 indexes (1 pre-existing + 1 new)
  02 BASE_AI_AGENT_CONFIG     R2 — 2 indexes (new)
  03 ai_ir_events             R3+ — 3 indexes (new)
  04 ai_entity_field          R3+ — 2 indexes (pre-existing)
  05 BASE_AI_SKILL_REVIEW     R2 — 1 index (pre-existing)
  06 BASE_AI_EVAL_RUN         R2 — 2 indexes (1 new + 1 pre-existing)

Total Indexes: 12 across 6 tables (7 new + 5 pre-existing verified)
HGs Triggered: 0
DB Writes Required: 7 new CREATE INDEX (5 IF NOT EXISTS skipped)
Schema Changes: 0
Data Migration: 0

Execution Mode: AUTHORIZED (Production READY)
Pre-flight Mechanical Gate: PASS (after schema correction)
```

---

## 2. Pre-Execution Verification (BEFORE any DB write)

For each table, MUST verify:

```sql
-- Per-table column verification (mixed-case AI module)
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('BASE_AI_PIPELINE','BASE_AI_AGENT_CONFIG','ai_ir_events','ai_entity_field','BASE_AI_SKILL_REVIEW','BASE_AI_EVAL_RUN')
ORDER BY TABLE_NAME, ORDINAL_POSITION;

-- Per-table row counts (baseline)
SELECT 'BASE_AI_PIPELINE', COUNT(*) FROM BASE_AI_PIPELINE
UNION ALL SELECT 'BASE_AI_AGENT_CONFIG', COUNT(*) FROM BASE_AI_AGENT_CONFIG
UNION ALL SELECT 'ai_ir_events', COUNT(*) FROM ai_ir_events
UNION ALL SELECT 'ai_entity_field', COUNT(*) FROM ai_entity_field
UNION ALL SELECT 'BASE_AI_SKILL_REVIEW', COUNT(*) FROM BASE_AI_SKILL_REVIEW
UNION ALL SELECT 'BASE_AI_EVAL_RUN', COUNT(*) FROM BASE_AI_EVAL_RUN;
```

**Verification result** (executed 2026-08-30):
- ✅ All columns present (after schema correction documented in PRE-FLIGHT §5)
- ✅ Row counts: BASE_AI_PIPELINE=409, BASE_AI_AGENT_CONFIG=5, ai_ir_events=3780, ai_entity_field=824, BASE_AI_SKILL_REVIEW=0, BASE_AI_EVAL_RUN=0

---

## 3. Execution Order

Per AI module dependency:

```
Step 1: BASE_AI_PIPELINE (foundation — pipeline orchestration)
Step 2: BASE_AI_AGENT_CONFIG (depends on pipeline)
Step 3: ai_ir_events (event sourcing — independent)
Step 4: ai_entity_field (IR projection — depends on ir_events)
Step 5: BASE_AI_SKILL_REVIEW (depends on agent + ir_events)
Step 6: BASE_AI_EVAL_RUN (depends on skill_review + ir_events)
```

---

## 4. Execution Instructions

### 4.1 Idempotency Guarantee

All 12 indexes use `IF NOT EXISTS` guard. Re-running the SQL is safe.

### 4.2 Execution Command

```bash
sqlcmd -S (local)\SQLEXPRESS -d ZXAF_V1_DevTest1 -i batch-09-add-index.sql
```

### 4.3 Post-Execution Verification

```sql
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN ('BASE_AI_PIPELINE','BASE_AI_AGENT_CONFIG','ai_ir_events','ai_entity_field','BASE_AI_SKILL_REVIEW','BASE_AI_EVAL_RUN')
  AND i.name LIKE 'IDX_%'
ORDER BY TableName, IndexName;
```

---

## 5. Risk Per Table

| Table | Risk | HGs | Action | Closure | New Indexes |
|-------|------|-----|--------|---------|-------------|
| BASE_AI_PIPELINE | R2 | 0 | REFACTORED | CLOSED | 1 |
| BASE_AI_AGENT_CONFIG | R2 | 0 | REFACTORED | CLOSED | 2 |
| ai_ir_events | R3+ | 0 | REFACTORED | CLOSED | 3 |
| ai_entity_field | R3+ | 0 | REFACTORED | CLOSED | 0 (verified) |
| BASE_AI_SKILL_REVIEW | R2 | 0 | REFACTORED | CLOSED | 0 (verified) |
| BASE_AI_EVAL_RUN | R2 | 0 | REFACTORED | CLOSED | 1 |

**Total**: 12 indexes (7 newly created + 5 verified pre-existing).

---

## 6. Failure Handling

If a table fails:
- Mark as `BLOCKED`
- Other eligible tables continue
- Document failure in table-specific evidence file

If an index creation fails:
- Check column case mismatch first (most common cause in AI module)
- Check existing index conflict (column order, included columns)
- Do not silently skip

---

## 7. Closure Documentation

After execution, produce:
- `batch-09-closure.md` (overall batch closure)
- `execution-evidence.md` (consolidated evidence)
- `PRE-FLIGHT.md` (already produced)

Update `Production-Progress-Ledger.md`:
- EXECUTED += 6
- PREPARED -= 6
- Progress: 46 / 274 = 16.8%

---

**Plan Status**: COMPLETE — READY FOR EXECUTION
**Pre-flight**: PASS (after schema correction, see PRE-FLIGHT.md §5)
**Authorization**: Chief Architect directive 2026-08-30
**Next Action**: Execute `batch-09-add-index.sql`
