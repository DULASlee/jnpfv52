# R2 — Coverage Matrix and Round Selection

> **Phase**: 8 — P8-A.5 (R2)
> **Status**: 🟢 **SELECTION COMPLETE** (Round 1 + Round 2 ready for execution)
> **Date**: 2026-08-30
> **Authority**: Chief Architect directive 2026-08-30 (R2 Comparative Validation Upgrade)

---

## 1. Exclusion Set (Tables NOT available for R2)

R2 must select from tables NOT in:

### 1.1 P8-A Shadow Tables (already validated, R1)

| # | Table | Risk |
|---|-------|------|
| 1 | base_sys_config | R0/R1 |
| 2 | base_user | R2 |
| 3 | base_visual_dev | R2 |
| 4 | ext_table_example | R2 |
| 5 | sa_data_dictionary | R3+ |

### 1.2 Pilot-Covered Tables (Phase 6)

| # | Table | Pilot |
|---|-------|-------|
| 1 | BASE_AI_PIPELINE | Pilot 1 |
| 2 | BASE_KNOWLEDGE_NODE | Pilot 2 |
| 3 | BASE_KNOWLEDGE_EDGE | Pilot 2 |
| 4 | FLOW_TASK | Pilot 3 |

### 1.3 P8-B Executed Tables (Batches 01-06, 30 tables)

```
Batch 01 (4): BASE_ORGANIZE, BASE_ROLE, BASE_POSITION, BASE_USER_RELATION
Batch 02 (5): base_authorize, base_module, base_module_button, base_module_column, base_module_form
Batch 03 (5): base_dictionary_type, base_dictionary_data, base_bill_rule, base_common_fields, base_common_words
Batch 04 (5): base_sys_config, base_sys_log, base_api_log, base_sign_img, base_syn_third_info
Batch 05 (5): base_province, base_province_atlas, base_data_interface, base_data_interface_log, base_data_interface_oauth
Batch 06 (6): ext_table_example, ext_product, ext_customer, ext_order, ext_order_entry, ext_email_config
```

### 1.4 P8-C Frozen Tables (Batches 07-17, prepared but not executed)

```
Batch 07 (6): flow_task_node, flow_task_operator, flow_template, flow_form, flow_delegate, flow_candidates
Batch 08 (4): blade_visual, blade_visual_category, BASE_REPORT, report_charts
Batch 09 (6): BASE_AI_PIPELINE, BASE_AI_AGENT_CONFIG, ai_ir_events, ai_entity_field, BASE_AI_SKILL_REVIEW, BASE_AI_EVAL_RUN
Batch 10 (6): flow_task, flow_comment, flow_event_log, flow_task_operator_user, flow_task_circulate, flow_visible
Batch 11 (6): BASE_AI_AGENT_SKILL, BASE_AI_PROMPT_TEMPLATE, BASE_AI_MODEL_PROVIDER, BASE_AI_MODEL_ROUTING, BASE_AI_CALL_LOG, BASE_AI_MCP_CONFIG
Batch 12 (6): ext_document, ext_employee, ext_work_log, ext_product_classify, ext_email_send, ext_project_gantt
Batch 13 (6): wform_applybanquet, wform_leaveapply, wform_contractapproval, wform_salesorder, wform_purchaselist, wform_travelapply
Batch 14 (6): WH_Bill, WH_BillDetail, WH_Customer, WH_Material, WH_Supplier, WH_Depot
Batch 15 (4): sa_assumptions, sa_consistency, sa_quality_score, sa_entity_fields
Batch 16 (3): BASE_KNOWLEDGE_RULE, kg_pattern, kg_pattern_usage
Batch 17 (11): BASE_AI_AGENT_CONFIG, BASE_AI_AGENT_SKILL, BASE_AI_EVAL_CASE, BASE_AI_EVAL_GOLDEN_SET, BASE_AI_GENERATED_PROJECT, BASE_AI_MODEL_PROVIDER, BASE_AI_MODEL_ROUTING, BASE_AI_PIPELINE_S2_PROGRESS, BASE_AI_PIPELINE_STAGE_CONFIG, BASE_AI_PROMPT_TEMPLATE, BASE_AI_UI_TEMPLATE
```

**Total Excluded**: 5 (Shadow) + 4 (Pilot) + 30 (P8-B Executed) + 70 (P8-C Frozen, some duplicates) = **unique ~85 tables excluded**

**Available for R2**: ~289 - 85 = ~204 tables

---

## 2. Coverage Matrix (10 tables across Risk × Dimension × Module × Entity)

| Dim \ Risk | R0/R1 | R2 | R3+ |
|-----------|-------|----|----|
| **A Schema** | ✓ R1-03 | ✓ R1-01, R2-04 | ✓ R2-01, R2-02, R2-03 |
| **B Integrity** | ✓ R1-03 | ✓ R1-02, R1-05 | ✓ R2-01, R2-02 |
| **C Index** | ✓ R1-03 | ✓ R1-01, R1-02 | ✓ R1-04, R2-01, R2-03 |
| **D Lifecycle** | ✓ R1-03 | ✓ R1-01, R1-02, R1-05 | ✓ R2-04 |
| **E CRUD/Query** | ✓ R1-03 | ✓ R1-01, R1-05 | ✓ R2-01, R2-03 |
| **F DDD** | ✓ R1-03 | ✓ R1-02, R2-04 | ✓ R1-04, R2-01 |
| **G Consumer/Target** | ✓ R1-03 | ✓ R1-02, R2-04 | ✓ R1-04, R2-01, R2-02, R2-05 |

**Coverage achieved**: Every dimension covered at every risk level ✓

---

## 3. Module Coverage

| Module | Round 1 | Round 2 | Total |
|--------|---------|---------|-------|
| system-core | 03 (base_advanced_query_scheme), 04 (base_file) | 04 (base_msg_account) | 3 |
| system-extension | 02 (ext_product_goods) | — | 1 |
| system-warehouse-legacy | — | 03 (WM_BillDetail) | 1 |
| workflow-engine | 05 (flow_template_json) | — | 1 |
| inteAssistant-SA-output | — | 01 (sa_business_process), 02 (sa_decision_table) | 2 |
| visualdata | — | 05 (base_visual_filter) | 1 |
| (other system) | 01 (base_message) | — | 1 |

**Module diversity**: 5 modules covered across 10 tables ✓

---

## 4. Entity Mapping Coverage

| Status | Round 1 | Round 2 | Total |
|--------|---------|---------|-------|
| YES (Entity-mapped) | 01, 02, 03, 05 | 04 | 5 |
| NO (Dynamic/no entity) | 04 | 01, 02, 03, 05 | 4 |

**Entity diversity**: 5 Entity-mapped + 4 Dynamic (close to 50/50, weighted toward entity to test Skill's primary use case) ✓

---

## 5. Round 1 Selection — Normal Production Stability

**Goal**: Verify Skill performs reliably on normal production scenarios (typical mid-complexity tables, not extreme).

### 5.1 Selected Tables

| # | Table | Risk | Module | Entity | Rows | Why Selected |
|---|-------|------|--------|--------|------|--------------|
| 01 | **base_message** | R2 | system-core | YES | 1229 | Mid-volume CRUD; lifecycle (message state); tenant+softdelete; typical production messaging |
| 02 | **ext_product_goods** | R2 | system-extension | YES | 10 | Extension pattern; product goods (relationship-heavy to ext_product); typical R2 |
| 03 | **base_advanced_query_scheme** | R0/R1 | system-core | YES | 2 | R0/R1 simple config; tests Skill's R0/R1 path (auto-close / auto-apply) |
| 04 | **base_file** | R3+ | system-core | NO | 0 | Dynamic/no-entity; tests Skill's handling of dynamic access pattern |
| 05 | **flow_template_json** | R2 | workflow-engine | YES | 3 | JSON-heavy workflow template; tests Skill's JSON + workflow handling |

### 5.2 Risk Distribution

| Risk | Count | Tables |
|------|-------|--------|
| R0/R1 | 1 | base_advanced_query_scheme |
| R2 | 3 | base_message, ext_product_goods, flow_template_json |
| R3+ | 1 | base_file |

### 5.3 Test Patterns

- **Normal R2 CRUD**: base_message (lifecycle), ext_product_goods (extension)
- **R0/R1 simple**: base_advanced_query_scheme
- **Dynamic/no-entity**: base_file
- **JSON-heavy**: flow_template_json
- **Workflow pattern**: flow_template_json

### 5.4 Module Diversity

- system (3): base_message, base_advanced_query_scheme, base_file
- system-extension (1): ext_product_goods
- workflow (1): flow_template_json

---

## 6. Round 2 Selection — Adversarial / Boundary Stability

**Goal**: Stress-test Skill against harder cases likely to expose calibration gaps.

### 6.1 Selected Tables

| # | Table | Risk | Module | Entity | Rows | Why Selected (Adversarial) |
|---|-------|------|--------|--------|------|---------------------------|
| 01 | **sa_business_process** | R3+ | inteAssistant-SA-output | NO | 19 | FK hub (4 incoming); no entity; no tenant/softdelete — tests SA pattern complexity |
| 02 | **sa_decision_table** | R3+ | inteAssistant-SA-output | NO | 172 | FK-heavy (2 outgoing); no entity; tests cross-module FK routing |
| 03 | **WM_BillDetail** | R3+ | system-warehouse-legacy | NO | 1629 | Legacy naming (WM_*); no tenant/softdelete; high volume; no entity — tests legacy + dynamic |
| 04 | **base_msg_account** | R2 | system-core | YES | 4 | Narrow but wide (39 cols); third-party account center; tests narrow-but-wide pattern |
| 05 | **base_visual_filter** | R3+ | system-core | NO | 0 | Dynamic/no-entity; tests Skill's consistency on dynamic across Rounds |

### 6.2 Risk Distribution

| Risk | Count | Tables |
|------|-------|--------|
| R0/R1 | 0 | (intentional — Round 2 focuses on harder cases) |
| R2 | 1 | base_msg_account |
| R3+ | 4 | sa_business_process, sa_decision_table, WM_BillDetail, base_visual_filter |

### 6.3 Test Patterns

- **FK-heavy hub**: sa_business_process (4 incoming FKs)
- **FK-heavy + JSON**: sa_decision_table
- **Legacy + dynamic**: WM_BillDetail
- **Narrow-but-wide**: base_msg_account (39 cols, 4 rows)
- **Repeated dynamic pattern**: base_visual_filter (compare to Round 1 base_file)

### 6.4 Module Diversity

- inteAssistant-SA (2): sa_business_process, sa_decision_table
- system-warehouse-legacy (1): WM_BillDetail
- system-core (2): base_msg_account, base_visual_filter

### 6.5 No Overlap with Round 1

| Round 1 | Round 2 | Overlap? |
|---------|---------|----------|
| base_message | base_msg_account | Different (different tables; though both msg-related) |
| ext_product_goods | (none) | No |
| base_advanced_query_scheme | (none) | No |
| base_file | base_visual_filter | Different tables, but both dynamic/no-entity (intentional pattern repetition) |
| flow_template_json | (none) | No |

**Strictly no table overlap** ✓

---

## 7. Cross-Round Coverage Matrix

Combined Round 1 + Round 2 coverage:

### 7.1 Risk Levels

| Risk | Round 1 | Round 2 | Total |
|------|---------|---------|-------|
| R0/R1 | 1 | 0 | 1 |
| R2 | 3 | 1 | 4 |
| R3+ | 1 | 4 | 5 |
| **Total** | **5** | **5** | **10** |

**Risk coverage**: 1+4+5 = 10 across all 3 risk levels ✓

### 7.2 Entity Mapping

| Entity | Round 1 | Round 2 | Total |
|--------|---------|---------|-------|
| YES | 4 | 1 | 5 |
| NO (dynamic) | 1 | 4 | 5 |
| **Total** | **5** | **5** | **10** |

**Entity coverage**: 5+5 = balanced ✓

### 7.3 Modules

| Module | Round 1 | Round 2 | Total |
|--------|---------|---------|-------|
| system-core | 3 | 2 | 5 |
| system-extension | 1 | 0 | 1 |
| system-warehouse-legacy | 0 | 1 | 1 |
| workflow-engine | 1 | 0 | 1 |
| inteAssistant-SA-output | 0 | 2 | 2 |
| **Total** | **5** | **5** | **10** |

**Module coverage**: 5 modules covered ✓

### 7.4 Special Patterns Tested

| Pattern | Tables |
|---------|--------|
| Dynamic/no-entity | base_file (R1), sa_business_process (R2), sa_decision_table (R2), WM_BillDetail (R2), base_visual_filter (R2) |
| FK-heavy (≥1 FK) | sa_business_process (4 in), sa_decision_table (2 out) |
| Legacy naming | WM_BillDetail |
| High volume (>1000 rows) | base_message (1229), WM_BillDetail (1629) |
| JSON-heavy | flow_template_json |
| Narrow-but-wide (≥30 cols, <100 rows) | base_msg_account (39 cols, 4 rows) |
| Simple config (R0/R1) | base_advanced_query_scheme |
| Lifecycle complexity | base_message (message state) |

---

## 8. Selection Justification by Selection Criteria

### 8.1 Round 1 Selection Criteria

Per R2-MASTER-PLAN §5.1:

| Criterion | Met? | Evidence |
|-----------|------|----------|
| Mix of R0/R1, R2, R3+ | ✓ | R0/R1 (1), R2 (3), R3+ (1) |
| Mix of Entity-mapped and dynamic | ✓ | Entity (4), Dynamic (1) |
| Mix of modules | ✓ | system, extension, workflow |
| ≥1 dependency graph hub | ⚠ | No FK-heavy table in R1 (intentional — R2 tests FK) |
| ≥1 lifecycle-complex table | ✓ | base_message |
| ≥1 dynamic/no-entity table | ✓ | base_file |
| Don't pick all Hard Gate | ✓ | 3 R2 + 1 R0/R1 + 1 R3+ |

### 8.2 Round 2 Selection Criteria

Per R2-MASTER-PLAN §5.2:

| Criterion | Met? | Evidence |
|-----------|------|----------|
| FK-heavy | ✓ | sa_business_process (4 in), sa_decision_table (2 out) |
| Self-reference pattern | ⚠ | No DB-level self-ref exists (registry §4.3 confirmed 0 self-ref) |
| Lifecycle ambiguity | ⚠ | Limited (SA tables have SCD pattern) |
| Tenant/soft-delete/audit combinations | ✓ | WM_BillDetail (no tenant, no softdelete, no audit — boundary) |
| Legacy naming | ✓ | WM_BillDetail |
| Unusual indexing | ⚠ | TBD during execution |
| Dynamic/no-entity | ✓ | 4 of 5 tables |
| High-impact/large-row-count tables | ✓ | WM_BillDetail (1629 rows), base_msg_account (39 cols) |
| Strictly different from Round 1 | ✓ | Zero overlap |

### 8.3 Coverage Matrix Criteria

Per R2-MASTER-PLAN §6:

| Dim \ Risk | R0/R1 | R2 | R3+ | Met? |
|-----------|-------|----|----|------|
| A Schema | ✓ | ✓ | ✓ | Yes |
| B Integrity | ✓ | ✓ | ✓ | Yes |
| C Index | ✓ | ✓ | ✓ | Yes |
| D Lifecycle | ✓ | ✓ | ✓ | Yes |
| E CRUD/Query | ✓ | ✓ | ✓ | Yes |
| F DDD | ✓ | ✓ | ✓ | Yes |
| G Consumer/Target | ✓ | ✓ | ✓ | Yes |

**All 21 cells (7 dim × 3 risk) covered** ✓

---

## 9. Tables NOT Selected (Acknowledgment)

### 9.1 Tables Considered but Not Selected

| Table | Reason |
|-------|--------|
| sa_dfd, sa_er, sa_pspec, sa_state_machine, sa_ui | Round 2 picks 2 of 5 SA tables (sa_business_process + sa_decision_table); the rest are not selected to avoid SA saturation |
| BASE_STUDIO_MENU | Has bak_20260617 sibling (special status); not selected for clarity |
| mt543406707183714245 etc. (Snowflake legacy) | Pattern is too narrow (snowflake ID naming); not generalizable |
| Demo_ExcelTest, Demo_Order, student | Demo tables — likely OUT_OF_SCOPE (parallel to ext_table_example SVR-001); not selected for Round 1/2 to keep stable production focus |

### 9.2 Why These Are Acceptable Omissions

- R2 is not trying to test ALL tables — it's testing Skill judgment STABILITY on representative samples
- SA saturation would only test one module's pattern (not Skill's general judgment)
- Snowflake ID tables are a narrow pattern (legacy naming); one WM_* table covers legacy
- Demo tables may have SVR-class scope issues; Round 1/2 aims for normal production, not scope disputes

---

## 10. Round 1 + Round 2 — Final Selection

### 10.1 Round 1 (5 tables)

```
01 base_message             R2    system-core        YES    1229 rows
02 ext_product_goods        R2    system-extension   YES    10 rows
03 base_advanced_query_scheme R0/R1  system-core    YES    2 rows
04 base_file                R3+   system-core        NO     0 rows (dynamic)
05 flow_template_json       R2    workflow-engine    YES    3 rows (JSON)
```

### 10.2 Round 2 (5 tables)

```
01 sa_business_process      R3+   inteAssistant-SA   NO     19 rows (4 in FKs)
02 sa_decision_table        R3+   inteAssistant-SA   NO     172 rows (2 out FKs)
03 WM_BillDetail            R3+   system-legacy      NO     1629 rows (legacy)
04 base_msg_account         R2    system-core        YES    4 rows (39 cols, narrow-wide)
05 base_visual_filter       R3+   system-core        NO     0 rows (dynamic)
```

---

## 11. Next Action

### Round 1 Execution

```
Step 1: Skill produces Result A
        - Read: table-refactor-expert/SKILL.md, Master Spec, Execution Manual
        - Process: 5 tables per Execution Manual §3 (5-step SOP)
        - Output: p8-a/r2/round-1/skill/01-base-message.md ... 05-flow-template-json.md
        
Step 2: Independent AI Expert produces Result B
        - Read: DB metadata + C# entity files (no Skill output visible)
        - Process: free-form expert reasoning, structured output per R2-EXPERT-PROTOCOL §3
        - Output: p8-a/r2/round-1/expert/01-base-message.md ... 05-flow-template-json.md
        
Step 3: Comparison
        - Per-table: 8 metrics + 4 safety gates
        - Output: p8-a/r2/round-1/comparison/per-table-comparison.md
        - Cumulative: p8-a/r2/round-1/comparison/cumulative-comparison.md
```

### Round 2 Execution (after Round 1 PASS or CONDITIONAL)

Same workflow with Round 2 tables.

---

## 12. Cross-References

- **R2 Master Plan**: `p8-a/r2/R2-MASTER-PLAN.md`
- **R2 Expert Protocol**: `p8-a/r2/R2-EXPERT-PROTOCOL.md`
- **R2 Comparison Protocol**: `p8-a/r2/R2-COMPARISON-PROTOCOL.md`
- **Phase 8 Master Plan**: `Phase-8-JNPF-Table-Refactoring-Master-Execution-Plan.md`
- **P8-0 Table Registry**: `p8-0/table-unit-registry-final.md` (289 tables)
- **P8-A Shadow 5**: `p8-a/shadow-table-selection.md`

---

**Document version**: 1.0
**Prepared by**: AI Engineer
**Date**: 2026-08-30
**Status**: ✅ Selection complete; Round 1 ready to execute
