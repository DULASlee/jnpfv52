# P8-C Batch 12 — Pre-flight Mechanical Gate

> **Phase**: 8 — P8-C Production (UNLOCKED 2026-08-30)
> **Batch**: 12
> **Status**: 🟢 **PRE-FLIGHT PASS — AUTHORIZED FOR EXECUTION** (after schema fixes)
> **Date**: 2026-08-30

---

## 1. Batch 12 Composition

```
Source: p8-c/batch-12/batch-12-add-index.sql
Scope: 6 tables, 12 indexes (1 skipped due to nvarchar(MAX))
Module: system-extension (ext_document, ext_employee, ext_work_log, ext_product_classify,
       ext_email_send, ext_project_gantt) + visualdata remaining
Note:  ext_* tables use lowercase f_ prefix; some columns are nvarchar(MAX)
```

| # | Table | Indexes (Attempted) | Pattern |
|---|-------|---------------------|---------|
| 01 | ext_document | 3 | `ext_*` |
| 02 | ext_employee | 3 | `ext_*` |
| 03 | ext_work_log | 2 (1 skipped) | `ext_*` |
| 04 | ext_product_classify | 1 | `ext_*` |
| 05 | ext_email_send | 2 | `ext_*` |
| 06 | ext_project_gantt | 2 | `ext_*` |
| **Total** | **6 tables** | **11 effective** | — |

---

## 2. Pre-flight Per Table

All 6 tables match `ext_*` → ✅ PRODUCT_CORE (registry §2.1: SYSTEM_TEMPLATE-eligible Sub-Tier, included via `ext_product, ext_customer, ext_order` etc. family; also per Master Plan extension rule).

### Schema verification (2026-08-30):
- ext_document: f_parent_id, f_type, f_is_share, f_share_time ✓
- ext_employee: f_en_code, f_department_name, f_ID_number ✓
- ext_work_log: f_creator_user_id, f_to_user_id (**nvarchar(MAX) — cannot index**), f_title ✓
- ext_product_classify: f_parent_id, f_sort_code ✓
- ext_email_send: f_creator_user_id, f_state, f_subject ✓
- ext_project_gantt: **f_task_name, f_start_date, f_end_date, f_assignee_id, f_progress — DO NOT EXIST**; **f_manager_ids — nvarchar(MAX)**

---

## 3. Schema Correction Log

| # | Table | Issue | Fix |
|---|-------|-------|-----|
| 1 | ext_project_gantt IDX_GANTT_PROJECT | f_task_name, f_start_date, f_end_date not in schema | Use f_full_name, f_start_time, f_end_time |
| 2 | ext_work_log IDX_WORKLOG_TOUSER | f_to_user_id is nvarchar(MAX) | Remove index |
| 3 | ext_project_gantt IDX_GANTT_ASSIGNEE | f_manager_ids is nvarchar(MAX) | Use f_type as proxy |

**Fixes applied**: SQL edited pre-execution.

---

## 4. Pre-flight Summary

```
Tables in Batch 12:           6
IN_SCOPE (PRODUCT_CORE):      6
OUT_OF_SCOPE:                 0
UNKNOWN:                      0

Pre-execution schema check:   ✅ PASS (after 3 fixes)
Indexes pre-existing:         5
Indexes to be newly created:  6 (12 - 1 skipped - 5 pre-existing)
Total indexes (effective):    11

Pre-flight Mechanical Gate: PASS ✅
Batch 12 Status: AUTHORIZED FOR EXECUTION (after schema fixes)
```

---

**Pre-flight Closed**: 2026-08-30
**Pre-flight Verdict**: ✅ **PASS — AUTHORIZED FOR EXECUTION**
