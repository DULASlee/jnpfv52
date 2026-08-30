# P8-C.1 Production Scope Registry

> **Phase**: 8 — P8-C.1
> **Status**: ✅ CLASSIFICATION COMPLETE (pending Human Decision on 3 UNKNOWN tables)
> **Date**: 2026-08-30

---

## 1. Executive Summary

```
Production Universe Freeze (Pre-Human-Decision):

  PRODUCT_CORE     (IN_SCOPE)       206   71.3%
  SYSTEM_TEMPLATE  (CONDITIONAL)     69   23.9%
  DEMO_SAMPLE      (OUT_OF_SCOPE)     5    1.7%
  TEST_FIXTURE     (OUT_OF_SCOPE)     6    2.1%
  UNKNOWN          (HUMAN_DECISION)   3    1.0%
                                       ---
                                       289  100.0%

Physical Inventory:                  289
Production Universe (IN_SCOPE):      206  ← Real refactoring target
```

**Critical correction**: The "289" used as production target was incorrect. Real production universe is **206 tables**.

---

## 2. Classification Rules Applied

### 2.1 PRODUCT_CORE (A) — 206 tables

**Rule**: Match one of:
- `base_*`, `BASE_*` (JNPF system core)
- `WH_*`, `WM_*` (warehouse management)
- `sa_*` (SA output tables)
- `ai_*`, `inte_*` (AI infrastructure)
- `kg_*` (knowledge graph)
- `flow_*` (workflow engine)
- `blade_*`, `report*`, `BASE_REPORT`, `data_report` (visual designer)
- Explicit allowlist: SYS_PROCESSED_EVENT, SYS_EVENT_OUTBOX_MESSAGE, undo_log, SchemaVersions, PROCESSED_EVENT, EVAL_METRIC, BASE_TENANT_GLOSSARY, BASE_TENANT_INDUSTRY, BASE_FOUNDER_AUTH_LOG, BASE_SANDBOX, domain_model

**Eligibility**: **1 — IN_SCOPE** (default production refactoring)

### 2.2 SYSTEM_TEMPLATE (B) — 69 tables

**Rule**: Match `wform_*` (51) or `ext_*` excluding `ext_table_example` (18)

**Examples**:
- `wform_applybanquet`, `wform_leaveapply`, `wform_contractapproval` (workflow form templates)
- `ext_product`, `ext_customer`, `ext_order` (extension examples)

**Eligibility**: **2 — CONDITIONAL** (depends on tenant deployment)

**Rationale**: These are JNPF's pre-built business templates. They're shipped with the platform but may be:
- Customized per tenant deployment
- Skipped if tenant doesn't use the feature
- Generated dynamically per use case

**Decision required from Chief Architect**:
- **Option A**: Include in Production Universe (treat as PRODUCT_CORE)
- **Option B**: Treat as OUT_OF_SCOPE (skip refactoring)
- **Option C**: Hybrid — only the ones tenants actually use

**Default pending**: CONDITIONAL until user decision.

### 2.3 DEMO_SAMPLE (C) — 5 tables

**Rule**: Match `Demo_*` prefix, `ext_table_example` (explicit "Example" suffix), or `student`

**Tables**:
- Demo_ExcelTest (14 cols, 3 rows)
- Demo_Order (15 cols, 151 rows)
- Demo_OrderDetail (9 cols, 60 rows)
- ext_table_example (28 cols, 33 rows)
- student (7 cols, 4 rows)

**Eligibility**: **3 — OUT_OF_SCOPE**

**Rationale**: Explicit demo/sample/teaching tables. Optimizing these as production assets is wasted effort.

### 2.4 TEST_FIXTURE (D) — 6 tables

**Rule**: Match `mt` + Snowflake ID pattern, contains `_BAK_`, or is SQL Server system metadata

**Tables**:
- mt543406707183714245 (7 cols, 2 rows)
- mt543408365615710149 (7 cols, 64 rows)
- mt543552698159464389 (7 cols, 32 rows)
- mt543668771097673669 (7 cols, 2 rows)
- mt543971603646513093 (3 cols, 0 rows)
- BASE_STUDIO_MENU_BAK_20260617 (19 cols, 54 rows — backup)

**Eligibility**: **3 — OUT_OF_SCOPE**

**Rationale**: Test data, fixtures, backups. Not part of production refactoring.

### 2.5 UNKNOWN (U) — 3 tables

**Tables**:
- zx_sys_config (17 cols, 2 rows)
- zx_sys_db (8 cols, 5 rows)
- zx_system_db (8 cols, 0 rows)

**Eligibility**: **4 — HUMAN_DECISION**

**Rationale**: "zx" prefix is NOT a standard JNPF naming convention. Likely either:
- Customer-specific extensions (e.g., "ZXAF" project code from database `ZXAF_V1_DevTest1`)
- Legacy code from a specific customer engagement
- Demo data for a particular client

**Requires Chief Architect decision**: Are these JNPF platform production tables, or tenant-specific?

---

## 3. Production Universe Freeze

```
Physical Inventory:    289  (all dbo.BASE TABLE)
                         ↓
Production Scope Decision Tree:
  ├─ A: PRODUCT_CORE     206  → IN_SCOPE         ← Real refactoring target
  ├─ B: SYSTEM_TEMPLATE   69  → CONDITIONAL      ← Awaiting Chief Architect decision
  ├─ C: DEMO_SAMPLE        5  → OUT_OF_SCOPE     ← Skipped
  ├─ D: TEST_FIXTURE       6  → OUT_OF_SCOPE     ← Skipped
  └─ U: UNKNOWN            3  → HUMAN_DECISION   ← Awaiting Chief Architect decision

Effective Production Universe (after Human Decision):
  206 (definite) + (0-69 from SYSTEM_TEMPLATE) + (0-3 from UNKNOWN)
  = 206 to 278 tables
```

---

## 4. Progress Metric Correction

### 4.1 Old (Deprecated)
```
Tables Closed / Physical Inventory:
94 / 289 = 32.53%
```
**Problem**: Includes demo, test, template as production target.

### 4.2 New (Effective Immediately)
```
Production Tables Closed / Production Universe:
M / 206 = X%
```

Where M is tables with Eligibility=1 (IN_SCOPE) that have been closed via Phase 8 refactoring.

### 4.3 Current State (After P8-B + P8-C Pause)

| Category | Already Closed | Remaining |
|---|---|---|
| IN_SCOPE (Product Core) | TBD by re-audit | 206 - TBD |
| CONDITIONAL (Templates) | TBD | 69 |
| OUT_OF_SCOPE | Skipped permanently | — |
| UNKNOWN | Pending | 3 |

---

## 5. Re-Audit Required: 17 Batches Already Executed

Per Master Plan §0.4 (严禁: 单个 JNPF finding 污染 Universal Core), we must verify that the 94 tables we already indexed do NOT include OUT_OF_SCOPE tables that should not have been touched.

### 5.1 Already-Indexed Tables (Batches 01-17)

**Product Core (correct)**:
- base_organize, base_role, base_position, base_user_relation
- base_authorize, base_module, base_module_button, base_module_column, base_module_form
- base_dictionary_type, base_dictionary_data, base_bill_rule, base_common_fields, base_common_words
- base_sys_config, base_sys_log, base_api_log, base_sign_img, base_syn_third_info
- base_province, base_province_atlas, base_data_interface, base_data_interface_log, base_data_interface_oauth
- flow_task, flow_task_node, flow_task_operator, flow_template, flow_form, flow_delegate, flow_candidates, flow_comment, flow_event_log, flow_task_operator_user, flow_task_circulate, flow_visible
- blade_visual, blade_visual_category, BASE_REPORT, report_charts
- BASE_AI_PIPELINE, BASE_AI_AGENT_CONFIG, ai_ir_events, ai_entity_field, BASE_AI_SKILL_REVIEW, BASE_AI_EVAL_RUN, BASE_AI_PROMPT_TEMPLATE, BASE_AI_MODEL_PROVIDER, BASE_AI_MODEL_ROUTING, BASE_AI_CALL_LOG, BASE_AI_MCP_CONFIG, BASE_AI_AGENT_SKILL, BASE_AI_EVAL_CASE, BASE_AI_EVAL_GOLDEN_SET, BASE_AI_GENERATED_PROJECT, BASE_AI_PIPELINE_S2_PROGRESS, BASE_AI_PIPELINE_STAGE_CONFIG, BASE_AI_UI_TEMPLATE
- BASE_KNOWLEDGE_RULE, kg_pattern, kg_pattern_usage
- sa_assumptions, sa_consistency, sa_quality_score
- WH_Bill, WH_BillDetail, WH_Customer, WH_Material, WH_Supplier, WH_Depot

**Template (debatable)**:
- ext_table_example ⚠️ — Actually DEMO_SAMPLE (should be UNTOUCHED)
- ext_product, ext_customer, ext_order, ext_order_entry, ext_email_config, ext_document, ext_employee, ext_work_log, ext_product_classify, ext_email_send, ext_project_gantt ⚠️ — SYSTEM_TEMPLATE (need user decision)
- wform_applybanquet, wform_leaveapply, wform_contractapproval, wform_salesorder, wform_purchaselist, wform_travelapply ⚠️ — SYSTEM_TEMPLATE (need user decision)

### 5.2 Out-of-Scope Tables Already Indexed (NEEDS REMEDIATION)

⚠️ **ext_table_example** was indexed in Batch 06 (P8-A also assessed it). Per P8-C.1 classification, this is **DEMO_SAMPLE** and should NOT have been refactored.

**Remediation options**:
- Option 1: Rollback the indexes (DROP INDEX for ext_table_example)
- Option 2: Keep the indexes (they don't harm production)
- Option 3: Mark as "incidental" in registry

**Recommendation**: Option 2 (keep) — adding indexes to a demo table doesn't break anything; the wasted work is the time spent, not the indexes themselves.

---

## 6. Batch Rule Going Forward

### 6.1 Allowed

| Eligibility | Allowed in Batch? | Approval |
|---|---|---|
| 1 (IN_SCOPE) | ✅ Yes | AI autonomous |
| 2 (CONDITIONAL) | ✅ Yes, with user approval per batch | Chief Architect |
| 3 (OUT_OF_SCOPE) | ❌ No | N/A |
| 4 (HUMAN_DECISION) | ⏸ Blocked until classified | Chief Architect |

### 6.2 Forbidden Tables for Future Batches

- Demo_*, ext_table_example, student → DEMO_SAMPLE (skip)
- mt*, *_BAK_* → TEST_FIXTURE (skip)
- zx_* → UNKNOWN (blocked until user classifies)

---

## 7. Decision Required from Chief Architect

### 7.1 SYSTEM_TEMPLATE (69 tables)

**Decision needed**: Include in production refactoring or exclude?

**Option A**: INCLUDE (treat as PRODUCT_CORE)
- Pro: All JNPF-shipped tables are kept
- Con: 69 more tables in scope

**Option B**: EXCLUDE (treat as DEMO_SAMPLE-like)
- Pro: Smaller scope, faster refactoring
- Con: Some tenant-deployed tables may be missed

**Option C**: HYBRID — Only refactor if used (row count > 0)
- Pro: Balanced
- Con: Per-tenant decision; may need re-classification per deployment

**Recommendation**: Option C with row_count > 0 as heuristic.

### 7.2 UNKNOWN (3 zx_* tables)

**Decision needed**: Are these JNPF platform production or tenant-specific?

**Likely answer**: Tenant-specific (from "ZXAF" project code visible in DB name). Recommend OUT_OF_SCOPE.

---

## 8. After Human Decisions

Once decisions are made:

```
Final Production Universe:
  IN_SCOPE:        206 (Product Core) + TBD (Template per decision)
  OUT_OF_SCOPE:    5 (Demo) + 6 (Test) + TBD (zx per decision)
  CONDITIONAL:     0 (after decision applied)

New progress metric:
  Tables Closed / Production Universe
  = M / (206 + TBD)
```

---

## 9. Registry Update Required

### 9.1 Add to Existing Table Unit Registry

```sql
-- Conceptual Registry extension
ALTER TABLE UnitRegistry ADD ScopeClassification CHAR(1);
ALTER TABLE UnitRegistry ADD Eligibility INT;
ALTER TABLE UnitRegistry ADD ClassEvidence NVARCHAR(500);
ALTER TABLE UnitRegistry ADD ClassConfidence VARCHAR(10);
ALTER TABLE UnitRegistry ADD ClassifiedBy VARCHAR(20);
ALTER TABLE UnitRegistry ADD ClassifiedDate DATETIME;
```

### 9.2 Update Existing Records

Each of 289 tables needs Scope + Eligibility columns populated per the classification above.

---

## 10. Next Steps After P8-C.1

1. **Chief Architect decides** SYSTEM_TEMPLATE treatment (Option A/B/C)
2. **Chief Architect classifies** the 3 UNKNOWN (zx_*) tables
3. **Re-audit** the 17 executed batches against new classification
4. **Update Registry** with Scope columns
5. **Resume production** with IN_SCOPE + (CONDITIONAL if approved)
6. **Track new metric**: Tables Closed / Production Universe

**No new batches until decisions are made.**
