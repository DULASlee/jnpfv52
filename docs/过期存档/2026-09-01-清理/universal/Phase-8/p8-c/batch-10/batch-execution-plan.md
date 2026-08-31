# P8-C Batch 10 — Execution Plan & DDL Summary

> **Phase**: 8 — P8-C Production (UNLOCKED 2026-08-30)
> **Batch**: 10
> **Status**: PLAN COMPLETE — READY FOR EXECUTION
> **Date**: 2026-08-30
> **Composition**: 6 tables (workflow-engine remaining, all PRODUCT_CORE)
> **Pre-flight**: PASS ✅ (see `PRE-FLIGHT.md`)

---

## 1. Executive Summary

```
Batch 10: PLAN COMPLETE ✅ (Pre-flight PASS)

Composition: 6 tables (workflow-engine remaining)
  01 flow_task                R3+ — 4 indexes (all pre-existing)
  02 flow_comment             R2  — 1 index  (pre-existing)
  03 flow_event_log           R2  — 1 index  (pre-existing)
  04 flow_task_operator_user  R2  — 2 indexes (pre-existing)
  05 flow_task_circulate      R2  — 1 index  (pre-existing)
  06 flow_visible             R2  — 0 indexes (diagnostic only)

Total Indexes: 9 across 6 tables
Indexes Pre-Existing: 9/9 (idempotent no-op re-execution)
HGs Triggered: 0
Schema Changes: 0
Data Migration: 0

Execution Mode: AUTHORIZED (Production READY)
Pre-flight Mechanical Gate: PASS (all 6 tables IN_SCOPE)
```

---

## 2. Pre-Execution Verification

Schema verification (executed 2026-08-30):
- ✅ All columns present
- ✅ All 9 IDX_* indexes pre-existing
- ✅ Row counts: flow_task=16, flow_comment=0, flow_event_log=24, flow_task_operator_user=0, flow_task_circulate=0, flow_visible=41

---

## 3. Execution Order

Per workflow-engine dependency:

```
Step 1: flow_task (foundation — workflow task orchestration)
Step 2: flow_comment (depends on flow_task)
Step 3: flow_event_log (depends on flow_task_node — already in Batch 07)
Step 4: flow_task_operator_user (depends on flow_task + flow_task_operator — both in Batch 07)
Step 5: flow_task_circulate (depends on flow_task)
Step 6: flow_visible (diagnostic only — no indexes added)
```

---

## 4. Execution Instructions

### 4.1 Idempotency Guarantee

All 9 indexes use `IF NOT EXISTS` guard. Re-running the SQL is safe.

### 4.2 Execution Command

```bash
sqlcmd -S (local)\SQLEXPRESS -d ZXAF_V1_DevTest1 -i batch-10-add-index.sql
```

---

## 5. Risk Per Table

| Table | Risk | HGs | Action | Closure | New Indexes |
|-------|------|-----|--------|---------|-------------|
| flow_task | R3+ | 0 | NO-CHANGE | CLOSED | 0 |
| flow_comment | R2 | 0 | NO-CHANGE | CLOSED | 0 |
| flow_event_log | R2 | 0 | NO-CHANGE | CLOSED | 0 |
| flow_task_operator_user | R2 | 0 | NO-CHANGE | CLOSED | 0 |
| flow_task_circulate | R2 | 0 | NO-CHANGE | CLOSED | 0 |
| flow_visible | R2 | 0 | NO-CHANGE | CLOSED | 0 |

**Total**: 9 indexes verified pre-existing.

---

## 6. Closure Documentation

After execution, produce:
- `batch-10-closure.md`
- `execution-evidence.md`

Update `Production-Progress-Ledger.md`:
- EXECUTED += 6
- PREPARED -= 6
- Progress: 52 / 274 = 19.0%

---

**Plan Status**: COMPLETE — READY FOR EXECUTION
**Authorization**: Chief Architect directive 2026-08-30
