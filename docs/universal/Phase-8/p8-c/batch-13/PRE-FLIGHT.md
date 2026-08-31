# P8-C Batch 13 — Pre-flight Mechanical Gate

> **Phase**: 8 — P8-C Production (UNLOCKED 2026-08-30)
> **Batch**: 13
> **Status**: 🟢 **PRE-FLIGHT PASS — AUTHORIZED FOR EXECUTION** (after 4 schema fixes)
> **Date**: 2026-08-30

---

## 1. Batch 13 Composition

```
Source: p8-c/batch-13/batch-13-add-index.sql
Scope: 6 tables, 18 indexes (3 per table)
Module: workflow-form-example (wform_* — SYSTEM_TEMPLATE/ST-PROD Sub-Tier)
Note:  Mixed naming — F_ApplyUser not universal; F_ApplyDate not universal
```

| # | Table | Indexes | Pattern |
|---|-------|---------|---------|
| 01 | wform_applybanquet | 3 | `wform_*` |
| 02 | wform_leaveapply | 3 | `wform_*` |
| 03 | wform_contractapproval | 3 | `wform_*` |
| 04 | wform_salesorder | 3 | `wform_*` |
| 05 | wform_purchaselist | 3 | `wform_*` |
| 06 | wform_travelapply | 3 | `wform_*` |
| **Total** | **6 tables** | **18 indexes** | — |

---

## 2. Pre-flight Per Table

All 6 tables match `wform_*` → ✅ PRODUCT_CORE/ST-PROD (registry §2.1 explicit allowlist, ST-PROD Sub-Tier per R6 RESOLUTION).

### Schema verification (2026-08-30):
- wform_applybanquet: F_BillNo, F_FlowId, F_ApplyUser, F_ApplyDate ✓
- wform_leaveapply: F_BillNo, F_FlowId, F_ApplyUser, F_LeaveStartTime, F_LeaveEndTime ✓
- wform_contractapproval: F_BillNo, F_FlowId ✓; F_ApplyUser ❌ (uses F_InputPerson); F_ApplyDate ❌ (uses F_SigningDate)
- wform_salesorder: F_BillNo, F_FlowId ✓; F_ApplyUser ❌ (uses F_Salesman); F_ApplyDate ❌ (uses F_SalesDate)
- wform_purchaselist: F_BillNo, F_FlowId, F_ApplyUser ✓; F_ApplyDate ❌ (uses F_PurchaseDate)
- wform_travelapply: F_BillNo, F_FlowId ✓; F_ApplyUser ❌ (uses F_TravelMan); F_ApplyDate ✓

---

## 3. Schema Correction Log

| # | Table | Index | Issue | Fix |
|---|-------|-------|-------|-----|
| 1 | wform_contractapproval | IDX_WFORM_CONTRACT_USER | F_ApplyUser/F_ApplyDate missing | Use F_InputPerson/F_SigningDate |
| 2 | wform_salesorder | IDX_WFORM_SALESORDER_USER | F_ApplyUser/F_ApplyDate missing | Use F_Salesman/F_SalesDate |
| 3 | wform_purchaselist | IDX_WFORM_PURCHASE_USER | F_ApplyDate missing | Use F_PurchaseDate |
| 4 | wform_travelapply | IDX_WFORM_TRAVEL_USER | F_ApplyUser missing | Use F_TravelMan |

**Fixes applied**: SQL edited pre-execution.

---

## 4. Pre-flight Summary

```
Tables in Batch 13:           6
IN_SCOPE (PRODUCT_CORE/ST-PROD): 6
OUT_OF_SCOPE:                 0
UNKNOWN:                      0

Pre-execution schema check:   ✅ PASS (after 4 fixes)
Total indexes:                18 (3 per table)

Pre-flight Mechanical Gate: PASS ✅
Batch 13 Status: AUTHORIZED FOR EXECUTION (after schema fixes)
```

---

**Pre-flight Closed**: 2026-08-30
**Pre-flight Verdict**: ✅ **PASS — AUTHORIZED FOR EXECUTION**
