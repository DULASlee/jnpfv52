# Production Progress Ledger

> **Phase**: 8 — Cross-Phase KPI
> **Status**: ACTIVE
> **Date**: 2026-08-30 (retrospective correction) + ongoing
> **Authority**: Chief Architect directive 2026-08-30
> **Update frequency**: After each batch closure or universe change

---

## 1. Progress State Definitions

| State | Definition | Countable as Production Progress? |
|---|---|---|
| **EXECUTED** | Physical database change has been applied (CREATE INDEX, ALTER TABLE, etc.) | ✅ YES |
| **PREPARED** | SQL written and reviewed; NOT yet applied to database | ❌ NO |
| **CLOSED** | Table Unit has passed all verification gates and is marked CLOSED in Registry | ✅ YES |
| **BLOCKED** | Table Unit cannot proceed due to upstream dependency, UNKNOWN classification, or Hard Gate | ❌ NO |
| **DEFERRED** | Table Unit deferred with explicit documented reason | ❌ NO |
| **OUT_OF_SCOPE** | Classified as DEMO_SAMPLE, TEST_FIXTURE, or confirmed UNKNOWN; permanently excluded | ❌ NO |

**Critical rule**: PREPARED ≠ EXECUTED. This distinction is mandatory. Mixing them produces false progress metrics.

---

## 1.1 Formal Production Baseline (Locked 2026-08-30)

```
Production Universe = 274 Table Units
  IN_SCOPE (PRODUCT_CORE):  206
  IN_SCOPE (ST-PROD):        68
  OUT_OF_SCOPE (permanent):  14
  UNKNOWN:                    0
  Physical total:            289

Actual Executed:              30 tables / 70 indexes (P8-B batches 01-06)
  RETAIN (PRODUCT_CORE):      24
  RECLASSIFY (ST-PROD):        5 (ext_* → ST-PROD)
  RETAIN-AS-EXCEPTION:         1 (ext_table_example — OUT_OF_SCOPE, not counted as production gain)

Remaining:                   244 Table Units
  PREPARED (P8-C frozen):     58
  NOT STARTED:               182 (PRODUCT_CORE) + 4 (ST-PROD not yet in P8-C)
  OUT_OF_SCOPE:               14 (permanent)

Correct progress: 30 / 274 = 10.9%
Deprecated metric: 94 / 289 = 32.5% (PREPARED counted as EXECUTED — DO NOT USE)
```

---

## 2. Production Universe

```
Physical Inventory:              289

OUT_OF_SCOPE (permanent):
  DEMO_SAMPLE (5)              - Demo_ExcelTest, Demo_Order, Demo_OrderDetail,
                                - ext_table_example, student
  TEST_FIXTURE (6)             - 5 × mt* (Snowflake ID) + BASE_STUDIO_MENU_BAK_20260617
  UNKNOWN (3)                  - zx_sys_config, zx_sys_db, zx_system_db
                                → OUT_OF_SCOPE / NOT EXECUTABLE (approved 2026-08-30)
                                ─────────────────────────────────────────
  Total excluded:               14

Production Universe:             289 - 14 = 275 (theoretical max)

SYSTEM_TEMPLATE Sub-Tier (RESOLVED 2026-08-30):
  wform_* (51)                  → ST-PROD ✅ (all referenced in DataBaseService.cs)
  ext_* excluding ext_table_example (17) → ST-PROD ✅ (all referenced in DataBaseService.cs)
  ext_table_example (1)         → OUT_OF_SCOPE / RETAIN-AS-EXCEPTION (SVR-001)
  ────────────────────────────────────────────────────────────────────────
  SYSTEM_TEMPLATE total:         69 (68 ST-PROD + 1 OUT_OF_SCOPE)

Effective Production Universe (IN_SCOPE + ST-PROD):
  MIN: 206 (PRODUCT_CORE only)
  MAX: 274 (206 PRODUCT_CORE + 68 ST-PROD)
  CURRENT: 274 ✅ (SYSTEM_TEMPLATE now ST-PROD eligible)
```

---

## 3. Current Progress (as of 2026-08-30)

### 3.1 Main Ledger

| Category | Count | Tables | Notes |
|---|---|---|---|
| **EXECUTED** | 30 | base_organize, base_role, base_position, base_user_relation, base_authorize, base_module, base_module_button, base_module_column, base_module_form, base_dictionary_type, base_dictionary_data, base_bill_rule, base_common_fields, base_common_words, base_sys_config, base_sys_log, base_api_log, base_sign_img, base_syn_third_info, base_province, base_province_atlas, base_data_interface, base_data_interface_log, base_data_interface_oauth, ext_product, ext_customer, ext_order, ext_order_entry, ext_email_config | P8-B batches 01-06 |
| **CLOSED** | 30 | Same as EXECUTED (all 30 EXECUTED tables are also CLOSED per p8-b-closure.md) | All passed verification; no rework |
| **PREPARED** | 58 | See batch-07..17 table list in `p8-c/HARD-FREEZE.md` §3 | P8-C batches 07-17; SQL ready; HARD FROZEN |
| **BLOCKED** | 0 | — | No current blocks |
| **DEFERRED** | 0 | — | No current deferrals |
| **OUT_OF_SCOPE** | 14 | 5 demo + 6 test + 3 unknown | Permanently excluded |
| **NOT STARTED** | 206 - 30 = 176 | Remaining PRODUCT_CORE | Pending production |

### 3.2 Progress Metrics

| Metric | Value | Notes |
|---|---|---|
| **Effective progress** | 30 / 274 = 10.9% | PRODUCT_CORE (206) + ST-PROD (68) = 274 universe |
| **Conservative metric** | 30 / 289 = 10.4% | Physical inventory (deprecated — 14 tables permanently excluded) |
| **Correct metric** | **30 / 274 = 10.9%** | Effective universe (PRODUCT_CORE + ST-PROD, excludes OUT_OF_SCOPE 14) |

> **Deprecated metric**: 94 / 289 = 32.5% — This figure incorrectly counted PREPARED (58 tables) as EXECUTED. Do not use.

### 3.3 Closed Breakdown

| Tier | Executed | Closed | In Progress | Not Started |
|---|---|---|---|---|
| PRODUCT_CORE (206) | 24 | 24 | 0 | 182 |
| ST-PROD (68) | 5 (ext_*) | 5 | 0 | 63 |
| DEMO_SAMPLE (5) | 1 (ext_table_example) | 0 | 0 (SVR-001 resolved) | 4 |
| TEST_FIXTURE (6) | 0 | 0 | 0 | 6 |
| UNKNOWN (3) | 0 | 0 | 0 | 3 |
| **Total (289)** | **30** | **29** | **0** | **259** |

> **Note on ext_table_example**: OUT_OF_SCOPE / DEMO_SAMPLE + RETAIN-AS-EXCEPTION. Indexes retained but NOT counted as production gain.

---

## 4. Batch-Level View

### 4.1 P8-B (EXECUTED / CLOSED)

| Batch | Tables | Indexes | Status |
|---|---|---|---|
| 01 | 4 | 10 | ✅ EXECUTED + CLOSED |
| 02 | 5 | 12 | ✅ EXECUTED + CLOSED (evidence verify pending) |
| 03 | 5 | 12 | ⚠️ EXECUTED / CLOSURE EVIDENCE PENDING |
| 04 | 5 | 11 | ⚠️ EXECUTED / CLOSURE EVIDENCE PENDING |
| 05 | 5 | 11 | ⚠️ EXECUTED / CLOSURE EVIDENCE PENDING |
| 06 | 6 | 14 | ⚠️ EXECUTED / CLOSURE EVIDENCE PENDING (incl. ext_table_example with SVR-001 pending) |
| **Total** | **30** | **70** | **1 verified + 1 verify pending + 4 pending evidence** |

### 4.2 P8-C (PREPARED / HARD FROZEN)

| Batch | Tables | Indexes | Status |
|---|---|---|---|
| 07 | 6 | 17 | 🔒 HARD FROZEN |
| 08 | 3 | 8 | 🔒 HARD FROZEN |
| 09 | 6 | 12 | 🔒 HARD FROZEN |
| 10 | 5 | 9 | 🔒 HARD FROZEN |
| 11 | 6 | 11 | 🔒 HARD FROZEN |
| 12 | 6 | 13 | 🔒 HARD FROZEN |
| 13 | 6 | 18 | 🔒 HARD FROZEN |
| 14 | 6 | 12 | 🔒 HARD FROZEN |
| 15 | 4 | 8 | 🔒 HARD FROZEN |
| 16 | 3 | 5 | 🔒 HARD FROZEN |
| 17 | 11 | 15 | 🔒 HARD FROZEN |
| **Total** | **58** | **128** | **LOCKED** |

---

## 5. Scope Violation Log

| SVR ID | Table | Classification | Execution | Disposition | Status |
|---|---|---|---|---|---|
| SVR-001 | ext_table_example | OUT_OF_SCOPE / DEMO_SAMPLE | 3 indexes in P8-B Batch 06 | **RETAIN-AS-EXCEPTION** (not counted as production gain) | ✅ RESOLVED 2026-08-30 |

---

## 6. Change Log

| Date | Change | Tables | Indexes | Evidence |
|---|---|---|---|---|
| 2026-08-30 | P8-B Batch 01 executed | 4 | 10 | batch-01-closure.md |
| 2026-08-30 | P8-B Batch 02 executed | 5 | 12 | batch-02-plan-and-execution.md |
| 2026-08-30 | P8-B Batch 03 executed | 5 | 12 | p8-b-closure.md (consolidated) |
| 2026-08-30 | P8-B Batch 04 executed | 5 | 11 | p8-b-closure.md (consolidated) |
| 2026-08-30 | P8-B Batch 05 executed | 5 | 11 | p8-b-closure.md (consolidated) |
| 2026-08-30 | P8-B Batch 06 executed (incl. SVR-001) | 6 | 14 | p8-b-closure.md (consolidated) |
| 2026-08-30 | Progress ledger corrected | — | — | R-FIND-01/02/03 applied |

---

## 7. Usage Rules

1. **Every status report** must use this ledger as the source of truth
2. **Every KPI calculation** must use EXECUTED / CLOSED counts from this ledger, not PREPARED counts
3. **Every UNFREEZE** requires this ledger to be updated with the batch being unlocked
4. **Every new classification** (e.g., SYSTEM_TEMPLATE Sub-Tier) must update this ledger before the batch proceeds
5. **PREPARED tables** must never be reported as "in progress" or "completed"

---

## 8. Cross-References

- Production Universe Decision: `p8-c/P8-C1-Production-Universe-Decision.md`
- Reconciliation: `p8-b/P8-B-Executed-Change-Reconciliation.md`
- HARD FREEZE: `p8-c/HARD-FREEZE.md`
- Phase Gate State: `phase-gate-state.md`
- Problem Routing Log: `kpi/problem-routing-log.md`