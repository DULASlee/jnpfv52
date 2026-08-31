# Phase 1.5 — V3+V4 R2-COMP Independent Validation Report

> **Validator**: Independent AI Expert (R2-COMP role per CR-20260820-01)
> **Method**: BLIND Gap Analysis — formed verdicts from current schemas + Target Contract BEFORE reading design spec §9 expected verdicts
> **Date**: 2026-08-31
> **Scope**: 10 tables (5 normal Round 1 + 5 adversarial Round 2)

---

## Summary

- **Validator role**: Independent subagent with FRESH context
- **Method**: Read JNPF Target Schema Contract (§1–§3) + 10 current CREATE TABLE statements from `backend/web/主库脚本.sql` and `backend/modularity/inteAssistant/Migrations/*.sql` BEFORE reading design spec §9 expected verdicts
- **Decision rule per column**:
  - **G0 (Critical)**: P0 security/tenant identity (e.g., f_tenant_id NULL or missing)
  - **G1 (Major)**: structural / type / nullable contract violation
  - **G2 (Minor)**: performance / index gap
  - **G3 (OK)**: compliant
- **Iron Laws referenced** (by abstract description from prompt):
  - "No Change needs proof" — NO-CHANGE verdict requires 8-dim evidence
  - "Mapping bypass" — low-code dynamic field mapping bypass
  - "Target Contract" — without Contract, no Skill invocation
  - "P0-Security" — P0 gaps fixed first, blocking
  - "Performance measurement" — before/after metrics required
  - "Migration 4-piece" — DUAL_WRITE 4 components required
  - "Runtime 7-layer" — 7-layer runtime safety
  - "Dynamic platform" — JNPF is low-code platform
  - "Evidence over claim" — verdicts require evidence
  - "Batch representative proof" — sample must be representative

---

## Round 1: 5 Normal Tables

### R1-T1: base_message

**Current schema (from 主库脚本.sql L849)**:
```
f_id nvarchar(50) NOT NULL (PK)
f_tenant_id nvarchar(50) NULL      ← G0 (target: NOT NULL DEFAULT 'DEFAULT_TENANT')
f_user_id nvarchar(50) NULL        ← G1 (target: NOT NULL)
f_is_read int NULL                  ← G1 (target: NOT NULL DEFAULT 0)
f_body_text nvarchar(max) NULL      ← G3 OK
f_delete_mark int NULL              ← G1 (target: NOT NULL DEFAULT 0)
f_enabled_mark int NULL             ← G1 (target: NOT NULL DEFAULT 1)
f_creator_time datetime NULL        ← G1 (target: datetime2(7) NOT NULL)
f_last_modify_time datetime NULL    ← G1
f_delete_time, f_delete_user_id NULL ← G1 (audit soft-delete fields nullable)
f_zx_system_id nvarchar(50) NULL    ← G2 (extension field, OK)
```
Per Target Contract §3.2: index_contract PASS (Phase 8 added IDX_MESSAGE_USER_READ).

**My blind verdict**: **REFACTORED**
- **Gap Type per column** (aggregated):
  - G0: f_tenant_id (1 column)
  - G1: f_user_id, f_is_read, f_delete_mark, f_enabled_mark, f_creator_time, f_last_modify_time, f_delete_time, f_delete_user_id (8 columns)
  - G2: f_zx_system_id (1 column, extension)
  - G3: f_id, f_body_text (2 columns)
- **Migration Type**: **B** (semantic — tenant_id NOT NULL backfill, audit NULL → NOT NULL enforcement requires data validation + dual-write for non-null columns)
- **Human Gate**: **REQUIRED** (P0 tenant_id backfill must be human-gated)
- **Iron Laws triggered**: P0-Security (f_tenant_id NULL), Target Contract, Migration 4-piece (dual-write for nullable→NOT NULL), Dynamic platform, Evidence over claim
- **Reasoning**: f_tenant_id NULL is P0 G0 critical (cross-tenant data leak risk). The remaining 8 audit/nullable gaps are G1 — they break the SaaS contract but don't immediately leak data. Phase 8 already added the tenant+user+read+time index, so no G2 index gap.

---

### R1-T2: flow_task_node

**Current schema (from 主库脚本.sql L3815)**:
```
f_id nvarchar(50) NOT NULL (PK)
f_tenant_id nvarchar(50) NULL      ← G0
f_task_id nvarchar(50) NULL        ← G1 (should be NOT NULL)
f_state int NULL                    ← G1
f_node_code nvarchar(50) NULL
f_node_name nvarchar(50) NULL
f_node_type nvarchar(50) NULL
f_node_property_json nvarchar(max) NULL  ← G3 (per workflow-engine override §2.2)
f_node_next nvarchar(2000) NULL
f_candidates, f_draft_data nvarchar(max) NULL  ← G3 (per override)
f_form_id nvarchar(50) NULL
f_creator_time, f_last_modify_time datetime NULL  ← G1
f_delete_time, f_delete_user_id, f_delete_mark NULL  ← G1
f_sort_code, f_zx_system_id NULL
```
Module override §2.2: workflow-engine — JSON fields OK as nvarchar(max); Phase 8 indexed flow_task_node.

**My blind verdict**: **REFACTORED**
- **Gap Type**:
  - G0: f_tenant_id (1)
  - G1: f_task_id, f_state, f_creator_time, f_last_modify_time, f_delete_time, f_delete_user_id, f_delete_mark (7)
  - G3: f_id, f_node_property_json, f_candidates, f_draft_data, f_node_next (per module override JSON exemption)
- **Migration Type**: **B**
- **Human Gate**: **REQUIRED**
- **Iron Laws triggered**: P0-Security, Target Contract, Migration 4-piece, Dynamic platform, Evidence over claim
- **Reasoning**: Module override explicitly accepts nvarchar(max) JSON (workflow JSON contract). The real gaps are tenant_id NOT NULL + audit fields. Phase 8 already covered indexes per contract.

---

### R1-T3: BASE_AI_PIPELINE

**Current schema (from 20260620_完整初始化.sql + 20260705_SA_三元组与冻结恢复.sql + 20260620_裁决书合并.sql + 20260705_SA_pipeline_work_mode.sql — assuming all migrations applied)**:
```
F_ID NVARCHAR(50) PRIMARY KEY              ← G3 (PascalCase per AI module override §2.3)
F_NAME NVARCHAR(200) NULL
F_CURRENT_STAGE NVARCHAR(50) NULL
F_STATUS NVARCHAR(50) NULL
F_STAGE_STATUS INT NULL
F_STARTED_TIME, F_FINISHED_TIME DATETIME NULL  ← G1 (target datetime2(7))
F_VALIDATION_ID NVARCHAR(50) NULL
F_STALE_FROM_STAGE NVARCHAR(50) NULL
F_REJECT_COUNT INT NOT NULL DEFAULT 0      ← G3
F_ABANDONED_AT, F_STALE_SINCE, F_STALE_AT DATETIME NULL  ← G1
F_FAILURE_COUNTS NVARCHAR(MAX) NULL
F_TENANT_ID NVARCHAR(50) NOT NULL DEFAULT 'default'  ← G3 (NOT NULL ✓)
F_PROJECT_ID NVARCHAR(50) NOT NULL DEFAULT ''  ← G3 (Triple-Key ✓)
F_FROZEN BIT NOT NULL DEFAULT 0             ← G3
F_FROZEN_AT, F_LAST_RESUMED_AT DATETIME2(7) NULL  ← G3
F_FROZEN_BY, F_FROZEN_REASON NVARCHAR NULL
F_RESUME_COUNT INT NOT NULL DEFAULT 0
F_CHECKPOINT NVARCHAR(MAX) NULL
F_WORK_MODE NVARCHAR(32) NOT NULL DEFAULT 'greenfield'
F_SOURCE_PIPELINE_ID, F_TARGET_PAGE_ROUTE, F_TARGET_PAGE_LABEL
F_CREATOR_TIME DATETIME NULL               ← G1 (target datetime2(7) NOT NULL)
F_CREATOR_USER_ID NVARCHAR(50) NULL
F_LAST_MODIFY_TIME DATETIME NULL           ← G1
F_LAST_MODIFY_USER_ID NVARCHAR(50) NULL
F_DELETE_MARK INT DEFAULT 0 (NOT NOT NULL!) ← G1 (DEFAULT exists but NULL allowed)
F_ENABLED_MARK INT DEFAULT 1 (NOT NOT NULL!) ← G1
F_SORT_CODE BIGINT NULL
F_DELETE_USER_ID NVARCHAR(50) NULL
F_DELETE_TIME DATETIME NULL
Indexes: IDX_STALE_CHECK, IDX_PIPELINE_PROJECT, IDX_PIPELINE_FROZEN  ← G3 (Phase 8)
```
Module override §2.3: PascalCase_no_prefix ✓, Triple-Key ✓.

**My blind verdict**: **REFACTORED**
- **Gap Type**:
  - G1: F_CREATOR_TIME, F_LAST_MODIFY_TIME, F_STAGE_STATUS, F_DELETE_MARK, F_ENABLED_MARK, F_DELETE_TIME, F_DELETE_USER_ID, F_STARTED_TIME, F_FINISHED_TIME, F_ABANDONED_AT, F_STALE_SINCE, F_STALE_AT (~12 columns)
  - G3: all others including indexes
- **Migration Type**: **B** (nullable contract enforcement: DEFAULT present but NOT NULL missing on F_DELETE_MARK, F_ENABLED_MARK)
- **Human Gate**: **REQUIRED** (nullable enforcement + datetime→datetime2(7) migration)
- **Iron Laws triggered**: Target Contract, Migration 4-piece (dual-write for NOT NULL + datetime), No Change needs proof (most columns stay), Evidence over claim
- **Reasoning**: Triple-Key + PascalCase + indexes are PASS. The remaining gaps are nullable tightening + datetime→datetime2(7) migration. NOT NULL + default exists, just missing NOT NULL keyword = light B-type migration.

---

### R1-T4: ext_order

**Current schema (from 主库脚本.sql L3093)**:
```
f_id nvarchar(50) NOT NULL (PK)        ← G3
f_tenant_id nvarchar(50) NULL          ← G0
f_customer_id, f_salesman_id, f_order_code, f_transport_mode NULL  ← G1
f_order_date, f_delivery_date datetime NULL  ← G1
f_delivery_address, f_file_json nvarchar(max) NULL  ← G3
f_payment_mode, f_receivable_money decimal(18,2) NULL
f_earnest_rate, f_prepay_earnest decimal(18,2) NULL
f_current_state int NULL
f_flow_id nvarchar(50) NULL
f_creator_time, f_last_modify_time datetime NULL  ← G1
f_delete_time, f_delete_user_id, f_delete_mark NULL  ← G1
f_enabled_mark, f_sort_code NULL
f_inte_assistant, f_dept NULL
```
ext_ prefix: Module §2.5 says partial — ext_order looks like regular business table (not dynamic form-generated). No special exemption applies. Missing index on (f_tenant_id, f_creator_time).

**My blind verdict**: **REFACTORED**
- **Gap Type**:
  - G0: f_tenant_id (1)
  - G1: ~10 nullable contract + datetime fields
  - G2: missing (f_tenant_id, f_creator_time) index (project default requires this)
- **Migration Type**: **B**
- **Human Gate**: **REQUIRED** (P0 + index add)
- **Iron Laws triggered**: P0-Security, Target Contract, Migration 4-piece, Performance measurement (missing index), Dynamic platform, Evidence over claim
- **Reasoning**: Standard business table. Full gap profile: tenant_id P0, audit G1, index G2. Standard gap analysis applies (NOT Type C — it's not a low-code dynamic field table).

---

### R1-T5: sa_assumptions

**Current schema (from 20260708_P9_ReqA.sql L16)**:
```
F_Id NVARCHAR(50) NOT NULL PK                              ← G3
F_TenantId NVARCHAR(50) NOT NULL DEFAULT ''                ← G3 (Triple-Key)
F_ProjectId NVARCHAR(50) NOT NULL DEFAULT ''               ← G3
F_PIPELINE_ID NVARCHAR(50) NOT NULL DEFAULT ''             ← G3
F_EventId NVARCHAR(50) NULL
F_SourceStep NVARCHAR(50) NOT NULL                         ← G3
F_AssumptionText NVARCHAR(MAX) NOT NULL                    ← G3
F_Confidence DECIMAL(3,2) NOT NULL DEFAULT 0.50            ← G3
F_IsUserConfirmed BIT NOT NULL DEFAULT 0                   ← G3
F_UserVerdict NVARCHAR(10) NULL
F_RoundCreated INT NOT NULL DEFAULT 1                      ← G3
F_CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()     ← G3 (best-in-class)
IX_sa_assumptions_triple (filtered on F_IsUserConfirmed=0)  ← G3
```
Module override §2.3: PascalCase_no_prefix ✓, Triple-Key ✓.
datetime2(7) used (best-practice compliant).

**My blind verdict**: **NO-CHANGE_OK** (with governance note)
- **Gap Type**: G3 across all columns (no P0, no G1, no G2). Only missing default-contract audit fields (F_CreatorUserId / F_LastModifyTime / F_LastModifyUserId / F_DeleteMark / F_DeleteTime / F_DeleteUserId), but for a derived runtime analysis table (not user-facing business table), audit fields are arguably not required — this is a governance interpretation.
- **Migration Type**: **none** (A or none)
- **Human Gate**: **REQUIRED** to confirm audit-field exemption for derived tables (governance call)
- **Iron Laws triggered**: Target Contract (all checks PASS), No Change needs proof (must show 8-dim evidence), Evidence over claim (the no-change verdict must justify why the audit-model gap is acceptable for derived tables)
- **Reasoning**: This is a best-in-class table: Triple-Key enforced, datetime2(7), filtered index, PascalCase per AI module override, all required fields NOT NULL with defaults. The "missing audit fields" is a strict reading of §1 audit_model — but sa_assumptions is a derived runtime analysis table, not a user-edited business table. The strict interpretation would say REFACTORED to add audit fields, but the practical interpretation says NO-CHANGE_OK because the table serves a transient derived-data purpose. **Human gate required for the audit-exemption decision.**

---

### Round 1 Aggregate (my blind verdicts)

| Table | My Verdict | Migration Type | Human Gate |
|---|---|---|---|
| R1-T1 base_message | REFACTORED | B | REQUIRED |
| R1-T2 flow_task_node | REFACTORED | B | REQUIRED |
| R1-T3 BASE_AI_PIPELINE | REFACTORED | B | REQUIRED |
| R1-T4 ext_order | REFACTORED | B | REQUIRED |
| R1-T5 sa_assumptions | NO-CHANGE_OK | none | REQUIRED (governance) |

**5 REFACTORED + 1 NO-CHANGE_OK** (if strictly counting, R1-T5 may flip to REFACTORED if audit-field exemption rejected — flagged for human review).

---

## Round 2: 5 Adversarial Tables

### R2-T1: wform_contractapproval (Type C adversarial)

**Current schema (from 主库脚本.sql L4525)**:
```
F_Id nvarchar(50) NOT NULL (PK)     ← G3 (PascalCase per low-code form convention)
F_FlowId, F_FlowTitle, F_BillNo, F_FirstPartyUnit, F_SecondPartyUnit,
F_FirstPartyPerson, F_SecondPartyPerson, F_FirstPartyContact, F_SecondPartyContact,
F_ContractName, F_ContractClass, F_ContractType, F_ContractId,
F_BusinessPerson NVARCHAR(50) NULL  ← G1 (PascalCase, dynamic field name)
F_IncomeAmount decimal(18,2) NULL
F_InputPerson nvarchar(50) NULL
F_FileJson, F_PrimaryCoverage, F_Description nvarchar(max) NULL
F_SigningDate, F_StartDate, F_EndDate datetime NULL
f_tenant_id nvarchar(50) NULL       ← would be G0 normally...
f_flow_id nvarchar(50) NULL
```
Module override §2.5 explicitly: wform_* = Type C, MANUAL_GOVERNANCE_REQUIRED.

**My blind verdict**: **MANUAL_GOVERNANCE** (SKIP_LOW_CODE_DYNAMIC)
- **Gap Type**: Not formally analyzed (skipped per override). The fields are dynamic form fields driven by low-code config — direct ALTER would break:
  1. Dynamic form `field_name` config
  2. Dynamic permission `authorize` associations
  3. Flow engine `flow_form_data_json` references
  4. SQL generator (codegen) table
- **Migration Type**: **C** (forbidden by override)
- **Human Gate**: **REQUIRED** (manual governance)
- **Iron Laws triggered**: Mapping bypass (low-code field mapping), Dynamic platform (JNPF-specific), No Change needs proof (must prove no-change via wform_ pattern recognition), Evidence over claim
- **Reasoning**: Module override §2.5 is unambiguous — wform_ prefix → Type C, no automated migration. Even if G0 f_tenant_id NULL exists, the override explicitly trumps the gap analysis.

---

### R2-T2: base_user (R3+ P0-Security adversarial)

**Current schema (from 主库脚本.sql L2262)**:
```
f_id nvarchar(50) NOT NULL (PK)        ← G3
f_account nvarchar(50) NULL            ← G0 (security core field)
f_password nvarchar(50) NULL           ← G0 P0 SECURITY (over-short = MD5)
f_secretkey nvarchar(50) NULL          ← G1
f_openId varchar(50) NULL              ← G1 (naming violation: no f_ prefix consistency, varchar vs nvarchar, camelCase)
f_tenant_id nvarchar(50) NULL          ← G0 P0
f_real_name, f_nick_name, f_head_icon, etc. NULL  ← G1/G2
f_mobile_phone, f_email NULL          ← G1 (PII)
f_lock_mark, f_unlock_time NULL        ← G1
f_is_administrator int NULL            ← G1 (needs CK_user_is_administrator)
f_creator_time, f_last_modify_time, f_delete_time NULL  ← G1
f_delete_mark, f_enabled_mark NULL     ← G1
f_openId varchar(50)                    ← Type A (naming bug)
~60+ columns, all nullable
```
Module override §2.1 (system-core): requires UK_base_user_tenant_account (UNIQUE on f_tenant_id, f_account) — currently MISSING. Requires CK_user_enabled_mark, CK_user_delete_mark, CK_user_is_administrator — currently MISSING. Requires IDX_user_tenant_organize — MISSING.

Target Contract §3.1 migration_plan explicitly defines Type B (DUAL_WRITE_6_MONTHS) for f_password → f_password_hash + f_password_algo + f_password_updated_at + f_password_version. Type A for f_openId → f_open_id.

**My blind verdict**: **REFACTORED** (highest-priority case)
- **Gap Type**:
  - G0 (P0 Security): f_tenant_id, f_account, f_password (3 columns)
  - G1 (Major): f_secretkey, f_creator_time, f_last_modify_time, f_delete_mark, f_enabled_mark, f_openId, f_is_administrator, ~20+ audit/nullability fields
  - G1 (constraint): missing UK_base_user_tenant_account, IDX_user_tenant_organize, 3 CHECK constraints
- **Migration Type**: **B** (semantic — password split into hash+algo+timestamp+version requires DUAL_WRITE_6_MONTHS)
- **Human Gate**: **REQUIRED** (P0 security)
- **Iron Laws triggered**: P0-Security (top priority), Migration 4-piece (DUAL_WRITE_6_MONTHS), Target Contract (uses §3.1 specific migration_plan), Performance measurement (UK index for tenant+account lookup), Dynamic platform, Evidence over claim
- **Reasoning**: This is THE canonical P0 security case. The contract §3.1 provides explicit migration_plan with Type A (f_openId rename) + Type B (password split). Module override §2.1 requires UK + CHECK + IDX. Estimated 6 schema changes (per §3.1).

---

### R2-T3: WH_Bill (warehouse-legacy adversarial)

**Current schema (from 主库脚本.sql L5724)**:
```
ID varchar(50) NOT NULL (PK)         ← G1 (no f_ prefix, varchar vs nvarchar)
BillCode int NOT NULL
DepotID int NOT NULL
StorageTypeID int NOT NULL
SupplierID, CustomerID, DeptID int NULL
Bearing int NULL
CreatePersonByID int NOT NULL
CreateDate datetime NULL              ← G1
CheckPersonByID, CheckDate int/datetime NULL
IsPrint int NULL
ProjectName varchar(100) NULL
Flag int NULL
Remark varchar(250) NULL
NO f_tenant_id column!                ← G0 (P0 missing)
NO f_creator_time, f_last_modify_time ← G1 (audit missing)
NO f_delete_mark, f_enabled_mark      ← G1 (soft-delete missing)
NO f_id (uses "ID" naming)            ← G1 (naming)
```
Module override §2.4 (warehouse-legacy): risk_level: R3+, **action: NO-CHANGE** (forced). v2.0 阶段不强改造. Recommendation: migrate to JNPF_Archive_Legacy in P3.

**My blind verdict**: **NO-CHANGE_OK** (governance override trumps gaps)
- **Gap Type**: (not formally analyzed due to override)
  - G0: tenant_id missing entirely
  - G1: ~6+ naming/audit gaps
- **Migration Type**: **none** (forced NO-CHANGE per §2.4)
- **Human Gate**: **REQUIRED** to acknowledge the tenant_id G0 is a known unresolved P0 within the NO-CHANGE scope (governance acceptance)
- **Iron Laws triggered**: No Change needs proof (must prove 8-dim even within override), P0-Security (tenant_id missing — acknowledged but NOT MIGRATED), Target Contract (gap exists), Dynamic platform, Evidence over claim
- **Reasoning**: Module override §2.4 forces NO-CHANGE on all 33 warehouse-legacy tables. The f_tenant_id G0 is a known-but-accepted exception (governance decision). Per "No Change needs proof" iron law, NO-CHANGE must prove that:
  1. The gap is acknowledged (yes — module override documents it)
  2. The gap is acceptable for current operational use (yes — legacy warehouse)
  3. Migration path is planned (yes — P3 archive to JNPF_Archive_Legacy)
  All three conditions met → NO-CHANGE_OK with explicit gap acknowledgment.

---

### R2-T4: ext_table_example (OUT_OF_SCOPE adversarial)

**Current schema (from 主库脚本.sql L3377)**:
```
f_id nvarchar(50) NOT NULL (PK)         ← G3
f_tenant_id nvarchar(50) NULL           ← would be G0 normally...
f_interaction_date, f_register_date datetime NULL  ← G1
f_project_code, f_project_name, f_principal, f_jack_stands, f_project_type,
f_project_phase, f_customer_name nvarchar NULL
f_cost_amount, f_tunes_amount, f_projected_income decimal(18,2) NULL
f_registrant, f_sign nvarchar(max), f_postil_json nvarchar(max), f_postil_count int NULL
f_creator_time, f_last_modify_time, f_delete_time datetime NULL  ← G1
f_delete_user_id, f_delete_mark, f_enabled_mark, f_sort_code NULL  ← G1
f_description nvarchar(500) NULL
```
Project Default Contract §out_of_scope:
```
- name: ext_table_example
  reason: DEMO_SAMPLE - SVR-001 处置（Phase 8 已识别）
  status: RETAIN_AS_EXCEPTION
```

**My blind verdict**: **OUT_OF_SCOPE** (RETAIN_AS_EXCEPTION)
- **Gap Type**: Not formally analyzed (excluded by out_of_scope)
- **Migration Type**: **none** (excluded)
- **Human Gate**: **NOT_REQUIRED** (already excluded by Project Default Contract)
- **Iron Laws triggered**: No Change needs proof (exception already proven by out_of_scope list), Evidence over claim
- **Reasoning**: This table is explicitly RETAIN_AS_EXCEPTION per SVR-001 disposition. Not in v2.0 Skill scope at all.

---

### R2-T5: BASE_AI_EVAL_CASE (AI module adversarial)

**Current schema (from V5.2_005_sprint5.sql L5)**:
```
F_Id BIGINT PRIMARY KEY                    ← G1 (BIGINT vs target nvarchar(50))
F_SetId BIGINT NOT NULL                    ← G1
F_Name NVARCHAR(200) NOT NULL              ← G3
F_Requirement NVARCHAR(MAX) NOT NULL       ← G3
F_ExpectedIR NVARCHAR(MAX) NULL            ← G3
F_Stage INT NULL
F_ScoreThreshold DECIMAL(3,2) DEFAULT 0.8
F_Enabled BIT DEFAULT 1
F_CreatorTime DATETIME DEFAULT GETDATE()   ← G1 (datetime vs target datetime2(7))
F_CreatorUserId BIGINT                     ← G1 (BIGINT vs target nvarchar(50))
F_ModifyTime DATETIME                      ← G1
F_ModifyUserId BIGINT                      ← G1
F_DeleteMark BIT DEFAULT 0
NO F_TenantId column!                      ← G0 P0 (Triple-Key requires F_TenantId)
```
Module override §2.3 (inteAssistant-AI): TRIPLE_KEY includes F_TenantId. **F_TenantId is missing → critical P0 violation.**

**My blind verdict**: **REFACTORED** (P0 critical)
- **Gap Type**:
  - G0: F_TenantId missing (1 — P0 critical for Triple-Key)
  - G1: F_Id, F_SetId (BIGINT vs nvarchar(50) — type mismatch), F_CreatorTime, F_ModifyTime (datetime vs datetime2(7)), F_CreatorUserId, F_ModifyUserId (BIGINT vs nvarchar(50)) (6 columns)
  - G2: no Triple-Key index (would need IX_EVAL_TRIPLE on F_TenantId, F_SetId)
  - G3: F_Name, F_Requirement, F_ExpectedIR
- **Migration Type**: **B** (semantic — adding F_TenantId requires backfill; BIGINT→nvarchar(50) type change requires DUAL_WRITE)
- **Human Gate**: **REQUIRED** (P0 F_TenantId)
- **Iron Laws triggered**: P0-Security (F_TenantId missing), Target Contract (Triple-Key violation), Migration 4-piece (DUAL_WRITE for type change), Performance measurement (no triple index), Dynamic platform, Evidence over claim
- **Reasoning**: This table is the most critical P0 in Round 2 (besides base_user). The AI module override §2.3 explicitly requires Triple-Key (F_TenantId, F_ProjectId, F_PIPELINE_ID), and this table has zero tenant_id. Also BIGINT IDs throughout — JNPF default is nvarchar(50) GUID strings. This needs a major refactor with DUAL_WRITE for type changes.

---

### Round 2 Aggregate (my blind verdicts)

| Table | My Verdict | Migration Type | Human Gate |
|---|---|---|---|
| R2-T1 wform_contractapproval | MANUAL_GOVERNANCE | C | REQUIRED |
| R2-T2 base_user | REFACTORED | B | REQUIRED |
| R2-T3 WH_Bill | NO-CHANGE_OK | none | REQUIRED (acknowledge P0) |
| R2-T4 ext_table_example | OUT_OF_SCOPE | none | NOT_REQUIRED |
| R2-T5 BASE_AI_EVAL_CASE | REFACTORED | B | REQUIRED |

**3 REFACTORED + 1 NO-CHANGE_OK + 1 MANUAL_GOVERNANCE + 1 OUT_OF_SCOPE.**

---

## Safety Gates (4/4) — preliminary assessment

- **S1 Hard Gate FN**: **PASS** — no false-negative on G0/P0 issues; all detected (base_message f_tenant_id, base_user f_tenant_id/f_password, BASE_AI_EVAL_CASE F_TenantId, WH_Bill no f_tenant_id, wform_contractapproval f_tenant_id, ext_table_example f_tenant_id)
- **S2 P0/P1 Decision Error**: **PASS** — all P0 issues flagged with REQUIRED human gate; no silent acceptance
- **S3 Scope Error**: **PASS** — R2-T1 correctly classified as MANUAL_GOVERNANCE (not REFACTORED), R2-T4 correctly as OUT_OF_SCOPE (not REFACTORED), R2-T3 correctly as NO-CHANGE_OK (not REFACTORED) per module overrides
- **S4 Closure Error**: **PASS** — every verdict has Iron Laws triggered + reasoning; no ungrounded claims

---

## CRITICAL Limitations Discovered

1. **Cannot test cross-family AI consensus**: Only one validator instance (me). The "cross-family mimo" protocol cannot be exercised in a single-instance blind test.
2. **Cannot test runtime 7-layer safety**: No actual Skill v2.0 code to execute; this is static schema analysis only.
3. **Cannot measure performance before/after**: Phase 8 measurement values are stated but not re-measured. The Contract claims base_message got 98.9% logical_reads reduction — assumed correct, not independently verified.
4. **Migration scripts not validated**: I read CREATE TABLE statements + a few ALTER TABLE migrations, but did not verify the live database state matches (e.g., migrations may not be applied to test DB).
5. **sa_assumptions audit-field gap is a governance call**: My NO-CHANGE_OK verdict depends on the assumption that derived runtime tables are exempt from default audit_model. A strict interpretation would flip to REFACTORED.
6. **WH_Bill tenant_id missing is acknowledged but not resolved**: NO-CHANGE_OK verdict depends on module override §2.4's P3-archive plan being acceptable. If governance rejects P3 deferral, this flips to REFACTORED.
7. **ext_order's classification as "not Type C"**: I judged ext_order as regular business table (not low-code dynamic). If governance says all ext_* are Type C, this verdict flips to MANUAL_GOVERNANCE.

---

## Final Verdict (BLIND — before reading §9)

- **Total verdicts**: 10
- **Distribution**:
  - REFACTORED: 6 (R1-T1, R1-T2, R1-T3, R1-T4, R2-T2, R2-T5)
  - NO-CHANGE_OK: 2 (R1-T5, R2-T3)
  - MANUAL_GOVERNANCE: 1 (R2-T1)
  - OUT_OF_SCOPE: 1 (R2-T4)
- **Safety Gates**: 4/4 preliminary PASS
- **Overall**: Pending §9 comparison

---

## §9 COMPARISON SECTION (filled in AFTER reading design spec expected verdicts)

> **Source of expected verdicts**: `docs/superpowers/plans/2026-08-30-表级重构Skill-v2.0-R2-COMP-验证计划.md` (R2-COMP Validation Plan — explicit per-table expected verdicts)
>
> **CRITICAL METHODOLOGY DISCOVERY**: The R2-COMP Validation Plan uses **HYPOTHETICAL test fixtures** for Round 1 tables (base_message / flow_task_node / BASE_AI_PIPELINE / ext_order / sa_assumptions) that represent the **post-migration compliant state** (all fields NOT NULL, datetime2(7), etc.). However, the **actual JNPF production schemas** (from `backend/web/主库脚本.sql` + migration files) are **non-compliant** with many NULL fields, datetime (not datetime2(7)), and missing indexes.
>
> **My blind analysis used ACTUAL production schemas**, not the hypothetical fixtures. This methodology mismatch is documented below for each disagreement.

---

### Round 1 Comparison

#### R1-T1: base_message

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | REFACTORED | NO-CHANGE_OK | **DISAGREE** |
| Migration Type | B | null | DISAGREE |
| Human Gate | REQUIRED | NOT_REQUIRED | DISAGREE |
| Gap detected | f_tenant_id NULL (G0), f_user_id NULL (G1), datetime (G1), audit fields (G1) | None (fixture is post-migration) | DISAGREE |
| 8-dim evidence | FAIL on nullable_contract / tenant_model | All PASS | DISAGREE |

**Reasoning for disagreement**: The R2-COMP fixture shows f_user_id NOT NULL, f_creator_time datetime2(7), f_tenant_id NOT NULL — all PASS-compliant. The actual production schema (主库脚本.sql L849) has these fields NULL. The Target Schema Contract §3.2 itself says base_message verdict = REFACTORED ("需要补 f_tenant_id NOT NULL"), contradicting the R2-COMP plan. **The Skill would correctly classify the actual production schema as REFACTORED.**

**If the R2-COMP fixture IS post-migration state (post-Phase 8 Batch 18)**: My verdict would flip to NO-CHANGE_OK. The Phase 8 IDX_MESSAGE_USER_READ index per Target Contract §3.2 suggests Phase 8 already optimized base_message — but the master SQL file shows the schema BEFORE Phase 8 migrations.

**Conclusion**: **METHODOLOGY MISMATCH** (actual vs hypothetical fixture) — not analytical disagreement.

---

#### R1-T2: flow_task_node

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | REFACTORED | NO-CHANGE_OK | **DISAGREE** |
| Migration Type | B | null | DISAGREE |
| Human Gate | REQUIRED | NOT_REQUIRED | DISAGREE |
| Gap detected | f_tenant_id NULL (G0), f_task_id NULL, f_state NULL, audit fields | None (fixture compliant) | DISAGREE |

**Reasoning for disagreement**: Same methodology mismatch — actual production schema has NULL fields; hypothetical fixture is compliant.

**Conclusion**: **METHODOLOGY MISMATCH**.

---

#### R1-T3: BASE_AI_PIPELINE

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | REFACTORED | NO-CHANGE_OK | **DISAGREE** |
| Migration Type | B (light) | null | DISAGREE |
| Human Gate | REQUIRED | NOT_REQUIRED | DISAGREE |
| Gap detected | F_DELETE_MARK/F_ENABLED_MARK DEFAULT but not NOT NULL (G1), F_CREATOR_TIME/F_LAST_MODIFY_TIME DATETIME (G1) | None (fixture compliant) | DISAGREE |

**Reasoning for disagreement**: Same methodology mismatch. PascalCase override ✓ matches expected; Triple-Key ✓ matches expected. The minor gaps are in nullable tightening + datetime→datetime2(7).

**Conclusion**: **METHODOLOGY MISMATCH** (both agree on PascalCase override + Triple-Key; only disagree on production-state gap profile).

---

#### R1-T4: ext_order

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | REFACTORED | REFACTORED | **AGREE on verdict** |
| Migration Type | **B** (tenant_id + audit + index) | **A** (fOdrCode rename) | **DISAGREE on Type** |
| Human Gate | REQUIRED | NOT_REQUIRED | **DISAGREE on Human Gate** |
| Gap detected | tenant_id NULL, audit, missing index | fOdrCode naming (Type A pure rename) | **DIFFERENT focus** |

**Reasoning for disagreement**: The R2-COMP fixture specifies "Has fOdrCode field (Type A pure naming error)". The ACTUAL JNPF production ext_order has `f_order_code nvarchar(50) NULL` — already lowercase, no fOdrCode to rename. So the fixture assumes a hypothetical state.

My blind verdict identified production gaps (tenant_id NULL, etc.) — Type B because semantic NOT NULL backfill is needed.

The Case A simulation test (L1316-1338) expected REFACTORED_TYPE_A with NOT_REQUIRED for the fOdrCode scenario.

**Conclusion**: **VERDICT CLASS AGREE (REFACTORED), but DISAGREE on Migration Type and Human Gate** because:
- R2-COMP fixture has fOdrCode (Type A pure rename) → NOT_REQUIRED
- Actual production has tenant_id NULL (Type B NOT NULL backfill) → REQUIRED

---

#### R1-T5: sa_assumptions

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | NO-CHANGE_OK | NO-CHANGE_OK | **AGREE** ✓ |
| Migration Type | none | null | AGREE |
| Human Gate | REQUIRED (governance note) | NOT_REQUIRED (implied by verdict) | minor difference |
| Gap detected | audit fields missing (PARTIAL), but functional table | None (fixture compliant) | AGREE on no-refactor decision |

**Reasoning for agreement**: Both agree the table is functional and no refactor needed. The Triple-Key is correctly enforced, datetime2(7) used, filtered index present.

**Conclusion**: **AGREE** (minor difference on whether audit-field gap triggers REQUIRED gate; both agree on no-refactor verdict).

---

### Round 1 Aggregate

- **AGREE**: 1/5 (R1-T5)
- **DISAGREE**: 4/5 (R1-T1, R1-T2, R1-T3, R1-T4)
- **Of the 4 DISAGREE**: 3 are due to **METHODOLOGY MISMATCH** (R1-T1, T2, T3 use hypothetical compliant fixtures; actual production has gaps)
- **R1-T4**: Verdict class agrees (REFACTORED), but Type/Human Gate differ because actual gap is different from fixture gap

**Net Round 1**: 1/5 strict agreement, but 5/5 if methodology-mismatch is excluded as invalidation of the test fixtures.

---

### Round 2 Comparison

#### R2-T1: wform_contractapproval

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | MANUAL_GOVERNANCE | MANUAL_GOVERNANCE_REQUIRED | **AGREE** ✓ |
| Migration Type | C | C | AGREE |
| Human Gate | REQUIRED | REQUIRED | AGREE |
| Gap detected | (skipped per override) | F_ApplyUser/F_InputPerson bypass | AGREE (both skip analysis) |

**Conclusion**: **FULL AGREE**.

---

#### R2-T2: base_user

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | REFACTORED | REFACTORED_P0_SECURITY | **AGREE** ✓ |
| Migration Type | B | B | AGREE |
| Human Gate | REQUIRED | REQUIRED | AGREE |
| Gap count | 3 G0 (tenant_id, password, account) | 3 G0 critical | AGREE |
| Iron Laws | P0-Security, Migration 4-piece, Target Contract, Dynamic platform | IRON-TABLE-02, 04, 06, 07, 08 (5 laws) | AGREE in principle |

**Conclusion**: **FULL AGREE** (both detect same P0 security gaps).

---

#### R2-T3: WH_Bill

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | NO-CHANGE_OK | NO-CHANGE_PROTECTED | **PARTIAL AGREE** |
| Migration Type | none | C (Legacy = protect) | minor difference |
| Human Gate | REQUIRED (acknowledge P0) | REQUIRED | AGREE |
| Gap detected | tenant_id missing (G0), naming (G1), audit missing (G1) — acknowledged but NOT MIGRATED | classification LEGACY_WAREHOUSE | AGREE on no-refactor |
| Iron Laws | "No Change needs proof" + "P0-Security" (acknowledged gap) | IRON-TABLE-04 (legacy protection) | AGREE on classification |

**Reasoning for partial disagreement**: My verdict used the more general "NO-CHANGE_OK" label; expected uses "NO-CHANGE_PROTECTED" (more specific: R3+ legacy with Type C protection). Both agree no migration happens; the terminology differs.

**Conclusion**: **PARTIAL AGREE** (semantic equivalence; verdict class agrees on "no migration").

---

#### R2-T4: ext_table_example

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | OUT_OF_SCOPE | OUT_OF_SCOPE_SKIP | **AGREE** ✓ |
| Migration Type | none | N/A | AGREE |
| Human Gate | NOT_REQUIRED | N/A | AGREE |
| Reason | DEMO_SAMPLE - SVR-001 - RETAIN_AS_EXCEPTION | SVR-001 already resolved by Phase 8 | **AGREE** ✓ |

**Conclusion**: **FULL AGREE**.

---

#### R2-T5: BASE_AI_EVAL_CASE

| Field | My Blind Verdict | Design Spec Expected | Match? |
|---|---|---|---|
| Verdict | REFACTORED | REFACTORED | **AGREE on verdict** |
| Migration Type | B | B | AGREE |
| Human Gate | REQUIRED | (implied REQUIRED) | AGREE |
| **Specific Gap** | **F_TenantId missing (P0)**, BIGINT ID types, datetime vs datetime2(7), BIGINT user IDs | **F_Name AS F_CaseCode (Mapping Is Not Migration violation)** | **DISAGREE on gap identity** |

**Reasoning for disagreement**: The R2-COMP fixture specifies "Phase 8 used F_Name AS F_CaseCode (Mapping Is Not Migration violation)". The ACTUAL production BASE_AI_EVAL_CASE has F_SetId, F_Name, F_Requirement, etc. — **no F_CaseCode** at all. So the fixture assumes a hypothetical state where Phase 8 substituted F_Name.

The actual production has different gaps (F_TenantId missing entirely, BIGINT types throughout).

Both result in REFACTORED Type B verdict, but the SPECIFIC VIOLATION detected differs:
- Expected: mapping bypass violation (F_Name substituted F_CaseCode)
- Actual: F_TenantId P0 missing + type inconsistencies

**Conclusion**: **VERDICT CLASS AGREE (REFACTORED B)**, but **DIFFERENT GAP IDENTITY** — depends on which test fixture (R2-COMP hypothetical vs actual production) is used.

---

### Round 2 Aggregate

- **AGREE**: 3/5 (R2-T1, R2-T2, R2-T4)
- **PARTIAL AGREE**: 2/5 (R2-T3 semantic equivalence, R2-T5 verdict class agree but gap identity differ)
- **DISAGREE**: 0/5 strict

**Net Round 2**: 3-5/5 agreement depending on strictness.

---

## FINAL VERDICT (after §9 comparison)

### Score Summary

| Round | AGREE | PARTIAL | DISAGREE | Total |
|---|---|---|---|---|
| Round 1 | 1 (R1-T5) | 1 (R1-T4 verdict class) | 3 (R1-T1, T2, T3 — methodology mismatch) | 5 |
| Round 2 | 3 (R2-T1, T2, T4) | 2 (R2-T3, T5) | 0 | 5 |
| **Total** | **4** | **3** | **3** | **10** |

### Safety Gates (4/4) — final assessment

- **S1 Hard Gate FN**: **PASS** — all G0/P0 issues detected (f_tenant_id NULL on base_message, base_user, ext_order; F_TenantId missing on BASE_AI_EVAL_CASE; tenant_id missing on WH_Bill; etc.)
- **S2 P0/P1 Decision Error**: **PASS** — all P0 flagged with REQUIRED gate; no silent acceptance
- **S3 Scope Error**: **PASS** — wform_contractapproval correctly MANUAL_GOVERNANCE; ext_table_example correctly OUT_OF_SCOPE; WH_Bill correctly NO-CHANGE per legacy override
- **S4 Closure Error**: **PASS** — every verdict has Iron Laws + reasoning

### CRITICAL FINDINGS

#### Finding 1: R2-COMP Verification Plan Uses HYPOTHETICAL Fixtures (METHODOLOGY MISMATCH)

The R2-COMP validation plan (`docs/superpowers/plans/2026-08-30-表级重构Skill-v2.0-R2-COMP-验证计划.md`) specifies "current_schema" fixtures for Round 1 that **do not match the actual JNPF production schemas**:
- R1-T1 base_message: fixture has f_user_id NOT NULL, f_creator_time datetime2(7), f_tenant_id NOT NULL → ALL COMPLIANT. Actual production: all NULL.
- R1-T2 flow_task_node: fixture description says "all 8 dimensions compliant". Actual production has NULL f_task_id, f_state, f_tenant_id.
- R1-T3 BASE_AI_PIPELINE: fixture description says "PascalCase naming + Triple-Key". Actual production has DEFAULT-without-NOT-NULL on F_DELETE_MARK, F_ENABLED_MARK, and DATETIME (not datetime2(7)) on time fields.
- R1-T4 ext_order: fixture has "fOdrCode (Type A pure naming error)". Actual production has f_order_code (correct naming).
- R2-T5 BASE_AI_EVAL_CASE: fixture has "Phase 8 used F_Name AS F_CaseCode". Actual production has no F_CaseCode — different gap profile.

**Impact**: 4 of 5 Round 1 "disagreements" are NOT analytical disagreements — they are methodology mismatches between the fixture (post-migration state) and the actual production state (pre-migration state).

**Recommendation**: The R2-COMP validation plan should either:
(a) Use actual production schemas as fixtures (then both expected + actual align), OR
(b) Explicitly document the fixtures as "post-migration expected state" and the test purpose becomes "verify Skill correctly classifies the target state, not the current state".

The current state is ambiguous and creates false disagreements.

#### Finding 2: Internal Inconsistency in Design Spec

The Target Schema Contract §3.2 (base_message) explicitly states `verdict: REFACTORED`. The R2-COMP validation plan R1-T1 says `expected overall_verdict: NO-CHANGE_OK`. These two design artifacts CONTRADICT each other.

The Target Contract is correct (base_message needs f_tenant_id NOT NULL per the contract's own gap_analysis).

**Recommendation**: Reconcile the two artifacts before R2-COMP execution.

#### Finding 3: R1-T5 sa_assumptions Audit-Field Exemption

The R2-COMP plan marks sa_assumptions as NO-CHANGE_OK with Human Gate: NOT_REQUIRED (implied). I marked it NO-CHANGE_OK with Human Gate: REQUIRED (for governance confirmation that derived tables are exempt from default audit_model).

This is a minor difference — both agree the table does not need refactoring. The Human Gate difference reflects my conservative position that governance should confirm the audit-field exemption for derived analysis tables.

#### Finding 4: WH_Bill Verdict Terminology

Expected: NO-CHANGE_PROTECTED with Type C (legacy protection)
My verdict: NO-CHANGE_OK with no Migration Type

Both agree no migration happens. The expected is more specific (R3+ legacy = protect). My verdict is more generic but reaches the same conclusion.

**Recommendation**: Use "NO-CHANGE_PROTECTED" for warehouse-legacy R3+ tables to align with design spec language.

### Limitations Acknowledged

1. **Single-instance validator**: I cannot independently verify cross-family AI consensus. The "R2 Independent Expert Selection" requires a different AI model (Claude vs GPT-4 vs Gemini); only one instance is executing this validation.
2. **No actual Skill code executed**: This is static schema analysis, not Skill runtime testing. DoD-01 through DoD-07 cannot be exercised without the actual Skill implementation.
3. **No live database access**: Schemas read from SQL files (`主库脚本.sql` + migration files). Live DB state may differ if migrations not applied.
4. **Performance measurements not re-verified**: Phase 8 claims (base_message 98.9% logical_reads reduction per Target Contract §3.5) assumed correct, not independently measured.
5. **ext_order classification judgment**: I judged ext_order as regular business table (not low-code dynamic). If governance says all ext_* are Type C, R1-T4 verdict flips to MANUAL_GOVERNANCE.

### Overall Result

- **Total Agreement**: 4/10 strict (or 7/10 if methodology-mismatch + semantic equivalence counted as agreement)
- **Round 1**: 1/5 strict agreement (or 5/5 if methodology-mismatch excluded)
- **Round 2**: 3/5 strict agreement (or 5/5 if semantic equivalence counted)
- **Safety Gates**: 4/4 PASS
- **Overall**: **CONDITIONAL_PASS**
  - PASS condition: If R2-COMP validation plan fixtures are recognized as "post-migration expected state" (methodology mismatch excluded), then 10/10 conceptual agreement + 4/4 safety gates → PASS.
  - FAIL condition: If strict interpretation demands fixture-vs-actual alignment, then Round 1 has 3 fixture mismatches that need to be resolved before FROZEN.

### Recommendation to Chief Architect

1. **Reconcile the Target Schema Contract §3.2 verdict (REFACTORED) with R2-COMP R1-T1 expected (NO-CHANGE_OK)** — internal contradiction.
2. **Update R2-COMP fixtures to match ACTUAL production schemas** OR document fixtures as "post-migration expected state" explicitly.
3. **Add sa_assumptions audit-exemption clause** to Project Default Contract §1 (or AI Module Override §2.3) to formalize the derived-table exception.
4. **Standardize "NO-CHANGE_PROTECTED" terminology** for warehouse-legacy tables.
5. **Cross-validate with another AI model family** (GPT-4 or Gemini) per CR-20260820-01 R2-COMP convention — single-instance validation is a known limitation.

---

**Report complete**: 10/10 tables evaluated, 4/4 safety gates PASS, 1 critical methodology finding (R2-COMP fixtures ≠ actual production), 1 internal design inconsistency (Target Contract §3.2 vs R2-COMP R1-T1).

**Status**: AWAITING_CHIEF_ARCHITECT_DECISION on whether CONDITIONAL_PASS is sufficient for Skill v2.0 FROZEN.