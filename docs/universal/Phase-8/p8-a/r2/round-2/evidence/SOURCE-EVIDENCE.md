# R2 Round 2 — Source Evidence Package

> **Phase**: 8 — P8-A.6 R2-COMP Round 2
> **Date**: 2026-08-30
> **Authority**: Both Skill and Independent AI Expert read from this package
> **Constraint**: Both MUST NOT see each other's output

---

## Shared Source Evidence

This is the **only** evidence both Skill and Independent AI Expert may read before producing their Result.

---

## 1. Round 2 Table Summary

| # | Table | Module | Category | Entity | Rows | Tenant | SoftDel | FKs In | FKs Out |
|---|-------|--------|----------|--------|------|--------|---------|--------|---------|
| 01 | sa_business_process | inteAssistant-SA | SA-output | **NO (dynamic)** | 19 | NO | NO | **4** | 1 |
| 02 | sa_decision_table | inteAssistant-SA | SA-output | **NO (dynamic)** | 172 | NO | NO | 0 | **2** |
| 03 | WM_BillDetail | system-warehouse-legacy | legacy | **NO (dynamic)** | 1629 | NO | NO | 0 | 0 |
| 04 | base_msg_account | system-core | messaging | YES | 4 | YES (inherited) | YES | 0 | 0 |
| 05 | base_visual_filter | system-core | visualdata | **NO (dynamic)** | 0 | YES (assumed) | YES (assumed) | 0 | 0 |

---

## 2. FK Detail (from P8-0 §4 / foreign-keys-raw.txt)

### 2.1 sa_business_process

**Incoming FKs (4) — HUB**:
- FK_sa_dict_bpm: sa_data_dictionary.bpm_id → sa_business_process.id
- FK_sa_pspec_bpm: sa_pspec.bpm_id → sa_business_process.id
- FK_sa_std_bpm: sa_state_machine.bpm_id → sa_business_process.id
- FK_sa_ui_bpm: sa_ui.bpm_id → sa_business_process.id

**Outgoing FKs (1)**:
- FK_sa_bpm_dfd: sa_business_process.dfd_id → sa_dfd.id

### 2.2 sa_decision_table

**Incoming FKs (0)**.

**Outgoing FKs (2)**:
- FK_sa_dt_dict: sa_decision_table.dict_id → sa_data_dictionary.id
- FK_sa_dt_pspec: sa_decision_table.pspec_id → sa_pspec.id

### 2.3 WM_BillDetail

**No FKs** (consistent with warehouse-legacy pattern — P8-0 §5.1 confirmed 0/39 tenant).

### 2.4 base_msg_account

**No FKs**.

### 2.5 base_visual_filter

**No FKs**.

---

## 3. Per-Table Schema Evidence

### 3.1 sa_business_process

- **File**: NONE (dynamic only)
- **SugarTable**: `sa_business_process`
- **Module**: inteAssistant-SA-output (per registry)
- **Inferred schema** (based on FKs + JNPF SA pattern):
  - F_Id (PK)
  - dfd_id (FK to sa_dfd)
  - TenantId? (NO per registry §5.1 — SA tables typically no tenant)
  - Common fields? (NO soft delete per registry — SA output)
  - ~15-25 columns typical for SA process table (name, version, description, content, creator, etc.)

### 3.2 sa_decision_table

- **File**: NONE (dynamic only)
- **SugarTable**: `sa_decision_table`
- **Module**: inteAssistant-SA-output
- **Inferred schema**:
  - F_Id (PK)
  - dict_id (FK to sa_data_dictionary)
  - pspec_id (FK to sa_pspec)
  - TenantId? (NO)
  - Common fields? (NO soft delete)
  - ~10-20 columns typical for SA decision table

### 3.3 WM_BillDetail

- **File**: NONE (dynamic only)
- **SugarTable**: `WM_BillDetail` (UPPERCASE, legacy naming)
- **Module**: system-warehouse-legacy
- **Inferred schema** (warehouse-legacy pattern):
  - BillId (no F_ prefix — legacy)
  - BillNo, MaterialId, Qty, UnitPrice, Amount, Remark (typical bill detail fields)
  - NO tenant (legacy)
  - NO soft delete (legacy)
  - **Note**: column names use legacy conventions (no F_ prefix, mixed case)
  - ~10-15 columns typical

### 3.4 base_msg_account

- **File**: `D:\JNPF-v52\backend\modularity\message\JNPF.Message.Entitys\Entity\MessageAccountEntity.cs`
- **SugarTable**: `BASE_MSG_ACCOUNT`
- **Class**: `MessageAccountEntity : TenantCLDSEntityBase`
- **Tenant**: YES (inherited)
- **SoftDelete**: YES (inherited)

| Property | Column | Type | Sensitivity |
|----------|--------|------|-------------|
| Category | F_CATEGORY | string? | Normal |
| FullName | F_FULL_NAME | string? | Normal |
| EnCode | F_EN_CODE | string? | Normal (business key) |
| AddressorName | F_ADDRESSOR_NAME | string? | Normal |
| SmtpServer | F_SMTP_SERVER | string? | Normal |
| SmtpPort | F_SMTP_PORT | int? | Normal |
| SslLink | F_SSL_LINK | int? | Normal |
| SmtpUser | F_SMTP_USER | string? | Normal |
| **SmtpPassword** | F_SMTP_PASSWORD | string? | **SENSITIVE — credential** |
| Channel | F_CHANNEL | int? | Normal |
| SmsSignature | F_SMS_SIGNATURE | string? | Normal |
| AppId | F_APP_ID | string? | Normal |
| **AppSecret** | F_APP_SECRET | string? | **SENSITIVE — credential** |
| EndPoint | F_END_POINT | string? | Normal |
| SdkAppId | F_SDK_APP_ID | string? | Normal |
| AppKey | F_APP_KEY | string? | Normal |
| ZoneName | F_ZONE_NAME | string? | Normal |
| ZoneParam | F_ZONE_PARAM | string? | Normal |
| EnterpriseId | F_ENTERPRISE_ID | string? | Normal |
| AgentId | F_AGENT_ID | string? | Normal |
| WebhookType | F_WEBHOOK_TYPE | int? | Normal |
| WebhookAddress | F_WEBHOOK_ADDRESS | string? | Normal |
| ApproveType | F_APPROVE_TYPE | int? | Normal |
| **Bearer** | F_BEARER | string? | **SENSITIVE — credential** |
| UserName | F_USER_NAME | string? | Normal |
| **Password** | F_PASSWORD | string? | **SENSITIVE — credential** |
| Description | F_DESCRIPTION | string? | Normal |

**Total columns**: 39 (28 declared + 11 inherited)
**Sensitive fields**: 4 (F_SMTP_PASSWORD, F_APP_SECRET, F_BEARER, F_PASSWORD)

### 3.5 base_visual_filter

- **File**: NONE (dynamic only)
- **SugarTable**: `base_visual_filter`
- **Module**: system-core (visualdata)
- **Inferred schema**:
  - F_Id (PK)
  - F_TenantId
  - F_FilterName, F_FilterConfig (JSON)
  - F_CreatorTime, F_CreatorUserId
  - F_DeleteMark
  - ~10-15 columns typical

---

## 4. Special Round 2 Considerations

### 4.1 SA-output Tables (sa_*)

- All 14 FK edges in the DB are within SA/KG modules (P8-0 §4.4)
- SA-output tables: NO tenant, NO soft delete (registry §5.1 confirmed)
- SA pattern: per-pipeline data, designed for ephemeral analysis
- **Implication**: sa_business_process and sa_decision_table are FK hubs of SA module but NOT multi-tenant

### 4.2 Legacy Tables (WM_*)

- No tenant (legacy)
- No soft delete (legacy)
- No F_ prefix on column names (legacy)
- Likely raw SQL access only
- 39 such tables in registry §2

### 4.3 Sensitive Credentials Table (base_msg_account)

- Contains 4 sensitive fields (passwords, secrets, bearer tokens)
- **HG#2 implications**: data integrity at risk if credentials leak
- **HG#3 implications**: any schema change = careful handling
- Foundry Target Profile: may require encryption-at-rest, masking in queries

### 4.4 Dynamic Table Pattern Repetition (base_visual_filter vs Round 1 base_file)

- Both are dynamic/no-entity
- Round 1 Skill correctly identified base_file as undefined situation
- **Round 2 test**: does Skill apply consistent treatment to base_visual_filter?

---

## 5. Hard Gates Reference (Master Spec §10.3)

| HG | Trigger Condition |
|----|-------------------|
| HG#1 | Tenant isolation missing or violated (no F_TenantId on multi-tenant table) |
| HG#2 | Data integrity at risk (no FK where app expects FK, orphan risk, missing UNIQUE) |
| HG#3 | Migration risk high (data type change, nullable → non-nullable, large data backfill) |
| HG#4 | Cross-module dependency detected (table referenced by 3+ modules via application logic, no DB FK indexes) |
| HG#5 | Business ambiguity (state machine unclear, multiple boolean fields, undocumented semantics) |

---

## 6. Risk Levels (Master Spec §10)

| Risk | Definition |
|------|------------|
| R0 | Auto-Close: no change needed; trivial table |
| R1 | Auto-Apply: simple index, low-risk DDL, no audit trail needed |
| R2 | Evidence-Driven: needs evidence-backed decision, audit trail recorded |
| R3+ | Human Approval required: complex change, cross-table impact, ambiguous business |

---

## 7. Round 2 Priority Observations

### Priority 1: Hard Gate stability
- sa_business_process = 4 INCOMING FKs → clear cross-module dependency
- sa_decision_table = 2 OUTGOING FKs → dependency on sa_data_dictionary
- base_visual_filter = no entity → undefined situation (Round 1 pattern)
- WM_BillDetail = legacy → possibly ambiguous semantics

### Priority 2: Risk systematic bias
- sa_business_process / sa_decision_table: should both be R3+ (FK hubs, no tenant)
- WM_BillDetail: R3+ (legacy, no entity, high volume)
- base_msg_account: R2 (entity exists, sensitive fields) or R3+ (security implications)
- base_visual_filter: R3+ (no entity, undefined)

### Priority 3: Evidence Sufficiency
- No entity tables → evidence is limited to DB metadata only
- Should Skill correctly STOP at metadata level, or insist on more?
- Round 1 pattern: Skill correctly escalated on base_file

---

## 8. Output Requirements

For each Table Unit, BOTH Skill and Independent AI Expert must produce:

1. Table Identity: Name, module, entity, row count, base class
2. A–G Assessment: 7 dimensions
3. Finding / Explicit No-Finding: with evidence tag
4. Evidence: Source files / SQL queries used
5. Risk: R0/R1/R2/R3+ with rationale
6. Hard Gate: All 5 HGs assessed
7. Recommended Action: One of 6 gates
8. Recommended Closure: One of 4

---

**Package compiled**: 2026-08-30
**Used by**: Both Skill (Result A) and Independent AI Expert (Result B)
**Isolation guarantee**: Neither path sees the other's output
