# P8-A.2 — AI Track A: 5 Tables Evaluation

> **Phase**: 8 — P8-A
> **Status**: COMPLETE
> **Date**: 2026-08-30
> **Mode**: Shadow — Read-Only, 0 DB writes
> **Track**: A (AI Independent Output)

---

## Executive Summary

| Table | Module | Risk | Closure | Hard Gate | Core Contam. |
|---|---|---|---|---|---|
| base_sys_config | system | R0/R1 | NO-CHANGE | None | 0 |
| base_user | system | R2 | NO-CHANGE | None | 0 |
| base_visual_dev | visualdata | R2 | SAFE-REFACTOR | None | 0 |
| ext_table_example | system-ext | R2 | NO-CHANGE | None | 0 |
| sa_data_dictionary | inteAssistant-SA | R3+ | DEFERRED | None | 0 |

**Aggregate**: 5/5 evaluated, 4 NO-CHANGE/SAFE-REFACTOR, 1 DEFERRED. Zero Hard Gate triggers.

---

# Table 1: base_sys_config

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `BASE_SYS_CONFIG` | `[KNOWN]` |
| Row count | 74 | `[KNOWN]` |
| Entity | `SysConfigEntity` at `backend/modularity/system/JNPF.Systems.Entitys/Entity/System/SysConfigEntity.cs` | `[KNOWN]` |
| SugarTable mapping | `[SugarTable("BASE_SYS_CONFIG")]` | `[KNOWN]` |
| Base class | `TenantCLDSEntityBase` | `[KNOWN]` |
| Index count | 1 (PK only) | `[KNOWN]` |
| PK | `F_ID` (CLUSTERED) | `[KNOWN]` |

### Schema (17 columns)

| Column | Type | Nullable | Purpose |
|---|---|---|---|
| F_ID | nvarchar(100) | NO | PK (Snowflake-style) |
| F_FULL_NAME | nvarchar(100) | YES | Display name |
| F_KEY | nvarchar(100) | YES | Business key |
| F_VALUE | nvarchar(MAX) | YES | Value |
| F_CATEGORY | nvarchar(100) | YES | Category |
| F_SORT_CODE | bigint | YES | Sort order |
| F_CREATOR_TIME | datetime | YES | Create time |
| F_CREATOR_USER_ID | nvarchar(100) | YES | Creator |
| F_LAST_MODIFY_TIME | datetime | YES | Last modify |
| F_LAST_MODIFY_USER_ID | nvarchar(100) | YES | Last modifier |
| F_DELETE_TIME | datetime | YES | Delete time |
| F_DELETE_USER_ID | nvarchar(100) | YES | Delete user |
| F_DELETE_MARK | int | YES | Soft delete (1=deleted) |
| F_TENANT_ID | nvarchar(100) | YES | Tenant |
| F_ZX_SYSTEM_ID | nvarchar(100) | YES | ZX system ID |
| F_ENABLED_MARK | int | YES | Enabled (1=enabled) |
| F_ZX_DATATYPE | int | YES | ZX data type |

## 2. Seven-Dimension Assessment

### Dimension A: Schema

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Schema matches Entity mapping exactly (16 cols in Entity vs 17 in DB — extra F_ENABLED_MARK + F_ZX_SYSTEM_ID are JNPF platform-injected) | `[KNOWN]` |
| All columns have appropriate types and lengths | `[KNOWN]` |
| Nullable pattern follows JNPF CLDS standard | `[KNOWN]` |
| No business field type misuse detected | `[COMPUTED]` |

### Dimension B: Integrity

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| No FK constraints (correct for config table — values are independent) | `[KNOWN]` |
| PK is non-nullable, properly clustered | `[KNOWN]` |
| No self-references | `[KNOWN]` |
| Tenant isolation present (F_TENANT_ID) | `[KNOWN]` |

### Dimension C: Index

**Finding: SAFE-REFACTOR recommended**

| Evidence | Tag |
|---|---|
| Only PK index exists | `[KNOWN]` |
| F_KEY is queried by config lookup (e.g., `WHERE F_KEY = 'xxx'`) | `[INFERRED]` from JNPF SysConfigService pattern |
| F_CATEGORY is queried for category-filtered config list | `[INFERRED]` |
| **Recommended new index**: `IDX_SYS_CONFIG_KEY (F_TENANT_ID, F_KEY)` | `[DESIGN]` |

### Dimension D: Lifecycle

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Standard CLDS fields (Creator/Modifier/Delete + Times) | `[KNOWN]` |
| F_ENABLED_MARK for enable/disable (independent of soft delete) | `[KNOWN]` |
| No custom state machine | `[KNOWN]` |
| Delete pattern: F_DELETE_MARK=1 + F_DELETE_USER_ID + F_DELETE_TIME | `[KNOWN]` |

### Dimension E: CRUD/Query

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Read pattern: `WHERE F_KEY = ? AND F_TENANT_ID = ?` | `[INFERRED]` |
| Read pattern: `WHERE F_CATEGORY = ? AND F_TENANT_ID = ?` | `[INFERRED]` |
| Write pattern: standard INSERT/UPDATE by PK | `[INFERRED]` |
| No N+1 risk identified | `[COMPUTED]` |

### Dimension F: DDD

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| SysConfig is a Singleton aggregate (one config row per key) | `[INFERRED]` |
| No FK relationships — clear aggregate boundary | `[KNOWN]` |
| No entity-level lifecycle conflict | `[KNOWN]` |

### Dimension G: Consumer / Target Readiness

**Finding: SAFE-REFACTOR recommended**

| Evidence | Tag |
|---|---|
| Entity is straightforward (16 columns explicit in C#) | `[KNOWN]` |
| All column mappings explicit via `[SugarColumn]` | `[KNOWN]` |
| Foundry Target Profile (ISoftDeleteEntity → F_DELETE_MARK int) mapping is direct | `[KNOWN]` |
| One JNPF Extension-specific note: F_ZX_SYSTEM_ID / F_ZX_DATATYPE — these are `zx_*` fields likely related to a specific subsystem (not standard JNPF) | `[INFERRED]` |
| **Action**: Index on F_KEY recommended (see Dimension C) | `[DESIGN]` |

## 3. Risk Classification

**Risk Level: R0/R1** — Confidence: HIGH (≥80%)

Rationale:
- Simple config table with no FK complexity
- Explicit Entity mapping reduces ambiguity
- Standard JNPF CLDS pattern
- Low query complexity (only by F_KEY/F_CATEGORY)

## 4. Hard Gate

**None triggered**.

- HG#1 (tenant isolation breach): NOT triggered — F_TENANT_ID present
- HG#2 (data integrity violation): NOT triggered — no FK to validate
- HG#3 (schema migration): NOT triggered — only ADD INDEX recommended
- HG#4 (cross-module): NOT triggered — single module
- HG#5 (business ambiguity): NOT triggered — config semantics clear

## 5. Recommended Action

```
SAFE-REFACTOR: Add index IDX_SYS_CONFIG_KEY (F_TENANT_ID, F_KEY)
```

This is the ONLY recommendation. It is:
- Read-only equivalent at runtime
- No data migration needed
- Rollback-able (DROP INDEX)

## 6. Recommended Closure

```
NO-CHANGE (with index recommendation queued for future batch)
```

In Shadow Mode, this is recorded as recommendation but NO DB write is performed.

## 7. Extension Routing

| Finding | Routing |
|---|---|
| F_ZX_SYSTEM_ID / F_ZX_DATATYPE — JNPF Extension-specific zx_* fields | JNPF Extension — not Master Spec |

## 8. Universal Core Purity

✅ Zero Universal Core contamination. All findings routed to:
- JNPF Extension (zx_* fields)
- Skill Evolution (no execution issues found)
- JNPF Extension (index recommendation)
- Target/Provider Profile (no constraint issue)

---

# Table 2: base_user

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `BASE_USER` | `[KNOWN]` |
| Row count | 45 | `[KNOWN]` |
| Entity | `UserEntity` at `backend/modularity/system/JNPF.Systems.Entitys/Entity/Permission/UserEntity.cs` | `[KNOWN]` |
| SugarTable mapping | `[SugarTable("BASE_USER")]` | `[KNOWN]` |
| Base class | `TenantCLDSEntityBase` | `[KNOWN]` |
| Column count | 68 (highest in DB) | `[KNOWN]` |
| Index count | 1 (PK only) | `[KNOWN]` |

## 2. Seven-Dimension Assessment

### Dimension A: Schema

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| 68 columns — wide table but standard JNPF user model | `[KNOWN]` |
| Multiple identification fields (account, real_name, mobile, email, certificate) | `[KNOWN]` |
| Login tracking fields (log time / IP / counts) | `[KNOWN]` |
| Mixed case observation: `f_openId varchar(50)` — atypical lowercase mixed pattern | `[KNOWN]` |
| F_INTE_ASSISTANT (int) — JNPF-specific flag, possibly used by inteAssistant module | `[INFERRED]` |

### Dimension B: Integrity

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| No DB-level FK to base_organize (despite F_ORGANIZE_ID column) | `[KNOWN]` |
| No DB-level FK to base_position (despite F_POSITION_ID) | `[KNOWN]` |
| No DB-level FK to base_role (despite F_ROLE_ID) | `[KNOWN]` |
| **Application-level relationships** are managed in code (correctly) | `[INFERRED]` |
| **JNPF pattern**: Application manages these relationships, not DB FK | `[KNOWN]` from JNPF architecture |

**Important finding**: base_user has many code-level FKs but zero DB-level FKs. This is by JNPF design — application layer enforces referential integrity through Repository pattern.

### Dimension C: Index

**Finding: SAFE-REFACTOR recommended**

| Evidence | Tag |
|---|---|
| Only PK index | `[KNOWN]` |
| Critical query patterns inferred: | |
| — Login by F_ACCOUNT (high frequency) | `[INFERRED]` |
| — List by F_ORGANIZE_ID (organize tree) | `[INFERRED]` |
| — List by F_ROLE_ID | `[INFERRED]` |
| — Search by F_QUICK_QUERY (full-text search field) | `[INFERRED]` |
| **Recommended indexes**: | `[DESIGN]` |
| — `IDX_USER_ACCOUNT (F_TENANT_ID, F_ACCOUNT)` | |
| — `IDX_USER_ORG (F_TENANT_ID, F_ORGANIZE_ID)` | |

### Dimension D: Lifecycle

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Standard CLDS + F_ENABLED_MARK + F_LOCK_MARK | `[KNOWN]` |
| F_HANDOVER_MARK + F_HANDOVER_USERID — JNPF handover workflow | `[INFERRED]` |
| F_CHANGE_PASSWORD_DATE — password policy tracking | `[KNOWN]` |
| Multiple state fields (lock/enable/admin/dev/inte_assistant) but each is independent boolean | `[KNOWN]` |

### Dimension E: CRUD/Query

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| Highest query volume table (login, list, search) | `[INFERRED]` |
| Multiple list-by-relationship queries (org/role/position) | `[INFERRED]` |
| **Performance impact**: Critical table, current state (PK only) requires table scans for non-PK queries | `[COMPUTED]` |
| **Recommendation aligns with Pilot-2 finding pattern** (index refactor needed) | `[DESIGN]` |

### Dimension F: DDD

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| User is a clear aggregate root | `[KNOWN]` |
| UserOwns: roles, positions, organize membership — managed at app layer | `[INFERRED]` |
| Wide schema is appropriate (user has many direct attributes) | `[COMPUTED]` |
| **NO Aggregate ambiguity** — this is a well-defined identity aggregate | `[KNOWN]` |

### Dimension G: Consumer / Target Readiness

**Finding: SAFE-REFACTOR recommended**

| Evidence | Tag |
|---|---|
| Entity has explicit `[SugarColumn]` mappings for most fields | `[KNOWN]` |
| Foundry Target Profile direct mapping for CLDS fields | `[KNOWN]` |
| JNPF Extension needed for: F_LOCK_MARK, F_HANDOVER_*, F_INTE_ASSISTANT, F_IS_DEV, F_BIZ_SYSTEM_ID, F_OPENID | `[DESIGN]` |
| Tenant isolation present | `[KNOWN]` |

## 3. Risk Classification

**Risk Level: R2** — Confidence: HIGH (≥80%)

Rationale:
- Critical identity table (login, authorization)
- Many code-level FKs to lookup tables
- Performance impact (no supporting indexes)
- Wide schema but well-defined aggregate
- **NOT R3+** — this is a known-good pattern with index gaps, not architectural risk

Critical: **Large schema does NOT automatically mean high risk**. base_user is large but its structure is well-understood.

## 4. Hard Gate

**None triggered**.

- HG#1 (tenant isolation): NOT triggered — F_TENANT_ID present
- HG#2 (data integrity): NOT triggered — application manages referential integrity
- HG#3 (migration): NOT triggered — only ADD INDEX recommended
- HG#4 (cross-module): NOT triggered — single module (Permission)
- HG#5 (business ambiguity): NOT triggered — User aggregate is clear

## 5. Recommended Action

```
SAFE-REFACTOR:
1. Add IDX_USER_ACCOUNT (F_TENANT_ID, F_ACCOUNT)
2. Add IDX_USER_ORG (F_TENANT_ID, F_ORGANIZE_ID)
3. Add IDX_USER_ROLE (F_TENANT_ID, F_ROLE_ID)
4. Document F_OPENID / F_INTE_ASSISTANT as JNPF Extension-specific
```

These are all additive (no schema change beyond index addition).

## 6. Recommended Closure

```
NO-CHANGE (with index recommendations queued)
```

Schema structure is correct; performance index gaps are deferred to later batch.

## 7. Extension Routing

| Finding | Routing |
|---|---|
| F_INTE_ASSISTANT (int flag) | JNPF Extension — inteAssistant module integration |
| F_OPENID (WeChat-style lowercase column) | JNPF Extension — third-party login integration |
| F_HANDOVER_MARK / F_HANDOVER_USERID | JNPF Extension — handover workflow |
| F_IS_DEV | JNPF Extension — developer mode flag |
| F_BIZ_SYSTEM_ID | JNPF Extension — multi-system routing |
| F_LOCK_MARK / F_UNLOCK_TIME | JNPF Extension — security lock state |

## 8. Universal Core Purity

✅ Zero contamination. All JNPF-specific fields routed to JNPF Extension. No Master Spec changes required.

---

# Table 3: base_visual_dev

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `BASE_VISUAL_DEV` | `[KNOWN]` |
| Row count | 48 | `[KNOWN]` |
| Entity | `VisualDevEntity` at `backend/modularity/visualdev/JNPF.VisualDev.Entitys/Entity/VisualDevEntity.cs` | `[KNOWN]` |
| SugarTable mapping | `[SugarTable("BASE_VISUAL_DEV")]` | `[KNOWN]` |
| Base class | (need to verify, likely TenantCLDSEntityBase) | `[INFERRED]` |
| Column count | 30 | `[KNOWN]` |
| Index count | 1 (PK only) | `[KNOWN]` |

## 2. Seven-Dimension Assessment

### Dimension A: Schema

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| Several nvarchar(MAX) JSON-as-text columns: f_tables_data, f_form_data, f_column_data, f_app_column_data, f_interface_param | `[KNOWN]` |
| F_PARENT_ID column (parent-child for organize tree pattern) | `[KNOWN]` |
| F_DB_LINK_ID (data source reference) | `[KNOWN]` |
| F_FLOW_ID (workflow template reference) | `[KNOWN]` |
| F_INTERFACE_ID (data interface reference) | `[KNOWN]` |
| **Large JSON-as-text fields**: typical low-code designer pattern | `[KNOWN]` |

### Dimension B: Integrity

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| No DB-level FK (all relationships via app layer) | `[KNOWN]` |
| F_PARENT_ID is self-reference but no DB FK | `[KNOWN]` |
| Tenant isolation present | `[KNOWN]` |

### Dimension C: Index

**Finding: SAFE-REFACTOR recommended**

| Evidence | Tag |
|---|---|
| Only PK index | `[KNOWN]` |
| Critical queries (inferred): | |
| — List by F_CATEGORY (form category filter) | `[INFERRED]` |
| — List by F_STATE (state filter) | `[INFERRED]` |
| — List by F_TYPE (form type) | `[INFERRED]` |
| — Tree by F_PARENT_ID | `[INFERRED]` |
| **Recommended indexes**: | `[DESIGN]` |
| — `IDX_VISDEV_CATEGORY (F_TENANT_ID, F_CATEGORY)` | |
| — `IDX_VISDEV_PARENT (F_TENANT_ID, F_PARENT_ID)` | |
| — `IDX_VISDEV_STATE (F_TENANT_ID, F_STATE)` | |

### Dimension D: Lifecycle

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| F_STATE (int) — JNPF custom state field for form lifecycle | `[INFERRED]` |
| F_TYPE (int) — form type (form/list/flow) | `[INFERRED]` |
| F_WEB_TYPE (int) — web/mobile/PC variant | `[INFERRED]` |
| Standard CLDS + F_ENABLED_MARK | `[KNOWN]` |
| **Custom state machine present**: F_STATE controls form dev → published → deprecated flow | `[INFERRED]` |

### Dimension E: CRUD/Query

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Read pattern: list by category/state/type | `[INFERRED]` |
| Single item by PK for form editing | `[INFERRED]` |
| Read by en_code (business key) for runtime form loading | `[INFERRED]` |
| Note: `f_en_code nvarchar(400)` — likely business identifier | `[KNOWN]` |

### Dimension F: DDD

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| VisualDev is a clear aggregate (form template) | `[KNOWN]` |
| Has self-reference (parent_id) but no ambiguity — pure hierarchy | `[KNOWN]` |
| JSON-blob children (form_data, column_data) are part of aggregate | `[INFERRED]` |

### Dimension G: Consumer / Target Readiness

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| Entity has explicit mappings | `[KNOWN]` |
| Several JSON-as-text fields require careful Foundry mapping | `[DESIGN]` |
| Target Profile needs to handle: F_STATE / F_TYPE / F_WEB_TYPE — these are JNPF enums | `[DESIGN]` |

## 3. Risk Classification

**Risk Level: R2** — Confidence: HIGH (≥80%)

Rationale:
- Metadata-heavy table (form designer templates)
- Multiple JSON-blob fields (lifecycle complexity)
- Custom state machine (F_STATE)
- Performance impact (large tables, no supporting indexes)
- **NOT R3+** — pattern is well-understood, complexity is in index recommendations not architecture

## 4. Hard Gate

**None triggered**.

- HG#1 (tenant): NOT triggered
- HG#2 (integrity): NOT triggered
- HG#3 (migration): NOT triggered — index additions only
- HG#4 (cross-module): borderline (used by visualdata + workflow + data interface) but managed at app layer
- HG#5 (business): NOT triggered — form template semantics clear

## 5. Recommended Action

```
SAFE-REFACTOR:
1. Add IDX_VISDEV_CATEGORY (F_TENANT_ID, F_CATEGORY)
2. Add IDX_VISDEV_PARENT (F_TENANT_ID, F_PARENT_ID)
3. Add IDX_VISDEV_STATE (F_TENANT_ID, F_STATE)
4. Document JSON-blob fields as JNPF-specific (extension mapping)
```

## 6. Recommended Closure

```
NO-CHANGE (with index recommendations queued)
```

## 7. Extension Routing

| Finding | Routing |
|---|---|
| F_STATE / F_TYPE / F_WEB_TYPE — JNPF enums | JNPF Extension |
| F_FLOW_ID / F_INTERFACE_ID / F_DB_LINK_ID — JNPF inter-module refs | JNPF Extension |
| JSON-blob fields (form_data, column_data, etc.) | JNPF Extension — designer JSON schema |

## 8. Universal Core Purity

✅ Zero contamination. All JNPF-specific logic routed to Extension.

---

# Table 4: ext_table_example

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `EXT_TABLE_EXAMPLE` | `[KNOWN]` |
| Row count | 28 | `[KNOWN]` |
| Entity | `TableExampleEntity` at `backend/modularity/extend/JNPF.Extend.Entitys/Entity/TableExampleEntity.cs` | `[KNOWN]` |
| SugarTable mapping | `[SugarTable("EXT_TABLE_EXAMPLE")]` | `[KNOWN]` |
| Base class | (likely TenantCLDSEntityBase) | `[INFERRED]` |
| Column count | 28 | `[KNOWN]` |
| Index count | 1 (PK only) | `[KNOWN]` |

## 2. Seven-Dimension Assessment

### Dimension A: Schema

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Standard JNPF CLDS pattern | `[KNOWN]` |
| Business fields: project_code/name, principal, customer_name, costs/income (decimal(9)) | `[KNOWN]` |
| JSON-as-text fields: f_postil_json, f_sign | `[KNOWN]` |
| Decimal(9) for amounts — appropriate precision | `[KNOWN]` |

### Dimension B: Integrity

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| No FK | `[KNOWN]` |
| PK clustered | `[KNOWN]` |
| Tenant isolation present | `[KNOWN]` |

### Dimension C: Index

**Finding: SAFE-REFACTOR recommended**

| Evidence | Tag |
|---|---|
| Only PK index | `[KNOWN]` |
| Critical queries: | |
| — List by F_PROJECT_TYPE | `[INFERRED]` |
| — List by F_REGISTRANT | `[INFERRED]` |
| — Search by F_PROJECT_CODE / F_PROJECT_NAME | `[INFERRED]` |
| — List by F_CUSTOMER_NAME | `[INFERRED]` |
| **Recommended**: `IDX_EXTEXAMPLE_TYPE (F_TENANT_ID, F_PROJECT_TYPE)` | `[DESIGN]` |

### Dimension D: Lifecycle

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Standard CLDS + F_ENABLED_MARK | `[KNOWN]` |
| No custom state machine | `[KNOWN]` |

### Dimension E: CRUD/Query

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Standard CRUD pattern | `[INFERRED]` |
| No N+1 risk | `[COMPUTED]` |

### Dimension F: DDD

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| TableExample is a self-contained aggregate | `[KNOWN]` |
| JSON-blob fields (postil, sign) are aggregate children | `[INFERRED]` |

### Dimension G: Consumer / Target Readiness

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| Entity mapping direct | `[KNOWN]` |
| No special Foundry mapping required | `[KNOWN]` |

## 3. Risk Classification

**Risk Level: R2** — Confidence: HIGH (≥80%)

Rationale:
- Standard extension pattern
- Simple CRUD operations
- No FK complexity
- Performance impact only (indexes missing)

## 4. Hard Gate

**None triggered**.

## 5. Recommended Action

```
SAFE-REFACTOR: Add IDX_EXTEXAMPLE_TYPE (F_TENANT_ID, F_PROJECT_TYPE)
```

## 6. Recommended Closure

```
NO-CHANGE (with index recommendation queued)
```

## 7. Extension Routing

**None**. This is a pure JNPF-standard extension example, no extension-specific fields found.

## 8. Universal Core Purity

✅ Zero contamination. Standard JNPF pattern.

---

# Table 5: sa_data_dictionary

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `sa_data_dictionary` (lowercase) | `[KNOWN]` |
| Row count | 35 | `[KNOWN]` |
| Entity | **NONE** — dynamically queried | `[KNOWN]` |
| Index count | **8** (richer than JNPF tables) | `[KNOWN]` |
| PK | `id` (CLUSTERED) | `[KNOWN]` |
| FK references (incoming) | **5** (highest in DB) | `[KNOWN]` |

### CRITICAL: Pattern Divergence

This table uses a **different naming and type convention** from JNPF main tables:

| JNPF Standard | sa_data_dictionary |
|---|---|
| F_TENANT_ID (nvarchar) | tenant_id (nvarchar) |
| F_DELETE_MARK int (1=deleted) | is_deleted bit |
| F_ID nvarchar(100) Snowflake | id bigint (IDENTITY-style) |
| F_* prefix on all columns | bare names (tenant_id, project_id) |
| Single PK index | 8 indexes (composite triple-key) |
| Standard CLDS audit | created_at / updated_at (lowercase) |

This is the **SA (Studio Architecture) Output Table Pattern** — designed for Triple-Key Iron Law (R12): (tenant_id, project_id, pipeline_id).

## 2. Seven-Dimension Assessment

### Dimension A: Schema

**Finding: SAFE-REFACTOR observed (CRITICAL)**

| Evidence | Tag |
|---|---|
| Different naming convention (no F_* prefix) | `[KNOWN]` |
| BIGINT id vs nvarchar Snowflake | `[KNOWN]` |
| BOOLEAN (bit) for soft delete | `[KNOWN]` |
| Composite triple-key indexes present | `[KNOWN]` |
| **Schema is correct for its purpose (SA output)** but DIFFERENT from JNPF main tables | `[COMPUTED]` |
| **Hard Gate risk**: Mixing SA tables with JNPF tables requires careful Foundry Target Profile mapping | `[DESIGN]` |

### Dimension B: Integrity

**Finding: Explicit No-Finding**

| Evidence | Tag |
|---|---|
| NOT NULL on tenant_id / project_id / asset_level / pipeline_id | `[KNOWN]` |
| Foreign key columns (dfd_id, bpm_id, event_id) are nullable — semantically meaningful | `[KNOWN]` |
| Incoming FK from 5 tables (sa_decision_table, sa_er, sa_pspec, sa_state_machine, sa_ui) | `[KNOWN]` |
| **Triple-key enforcement via composite indexes** (Triple-Key Iron Law R12) | `[KNOWN]` |

### Dimension C: Index

**Finding: Explicit No-Finding (GOOD DESIGN)**

| Evidence | Tag |
|---|---|
| 8 indexes present (most rich in DB) | `[KNOWN]` |
| `IX_sa_dict_triple (tenant_id, project_id, pipeline_id)` — Triple-Key support | `[KNOWN]` |
| `idx_sa_dict_dfd / bpm` — incoming FK support | `[KNOWN]` |
| `idx_sa_dict_tenant / project / validation / pattern_src` — query optimization | `[KNOWN]` |
| **EXCELLENT indexing strategy — no recommendations needed** | `[COMPUTED]` |

### Dimension D: Lifecycle

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| Custom lifecycle: `is_current bit`, `valid_from datetime2`, `valid_to datetime2` | `[KNOWN]` |
| **Temporal table pattern (SCD Type 2)** — versioned by valid_from/valid_to | `[INFERRED]` |
| version (int) — version increment | `[KNOWN]` |
| Standard created_at/updated_at/created_by/updated_by | `[KNOWN]` |
| **NO F_DELETE_MARK** — uses is_deleted bit + deleted_at | `[KNOWN]` |

### Dimension E: CRUD/Query

**Finding: SAFE-REFACTOR observed**

| Evidence | Tag |
|---|---|
| Query patterns (inferred from indexes): | |
| — List by triple-key (current state) | `[INFERRED]` |
| — Get by event_id (event sourcing) | `[INFERRED]` |
| — Get by dfd_id (FK navigation) | `[INFERRED]` |
| — Filter by validation_status (quality) | `[INFERRED]` |
| — Filter by is_pattern_source (pattern mining) | `[INFERRED]` |

### Dimension F: DDD

**Finding: SAFE-REFACTOR observed (CRITICAL)**

| Evidence | Tag |
|---|---|
| sa_data_dictionary is **shared projection** — read by 5 other SA tables | `[KNOWN]` |
| Pattern Tags (pattern_tags, is_pattern_source) — Knowledge Graph integration | `[KNOWN]` |
| LLM Confidence (llm_confidence) — AI-generated data quality tracking | `[KNOWN]` |
| Human Confirmed (human_confirmed) — human-AI collaboration trace | `[KNOWN]` |
| **This is NOT a typical JNPF aggregate** — it's a projection table for cross-domain analysis | `[DESIGN]` |

**Aggregate Boundary Concern**:
- sa_data_dictionary is the CENTRAL node in SA's read model
- 5 SA tables reference it
- It's also the source for KG pattern mining
- **DDD boundary**: Treat as Shared Projection (read model), NOT aggregate root

### Dimension G: Consumer / Target Readiness

**Finding: DEFERRED recommended**

| Evidence | Tag |
|---|---|
| Foundry Target Profile (ISoftDeleteEntity) maps is_deleted bit → IsDeleted — direct | `[KNOWN]` |
| **BUT**: Foundry profile assumes JNPF-style F_DELETE_MARK int (1=deleted) pattern | `[KNOWN]` |
| **Schema divergence**: sa_* tables use bit, JNPF uses int | `[KNOWN]` |
| **Cannot apply Universal Target Profile directly** without Foundry Profile extension | `[DESIGN]` |
| **HG#5 candidate**: Business semantics of NULL vs 1 vs 0 in is_deleted bit vs f_delete_mark int requires Human Decision | `[DESIGN]` |

## 3. Risk Classification

**Risk Level: R3+** — Confidence: HIGH (≥80%)

Rationale:
- 5 incoming FKs (highest coupling in DB)
- No Entity mapping (dynamic access only)
- Schema pattern divergence from JNPF main tables
- Triple-Key Iron Law (R12) constraint
- Projection table semantics (NOT typical aggregate)
- Foundry Target Profile mismatch

**Critical**: This table is the most complex to refactor — it touches schema divergence, dynamic access, projection semantics, and target profile mapping all at once.

## 4. Hard Gate

**None directly triggered**, but borderline on HG#5:

- HG#1 (tenant isolation): NOT triggered — tenant_id present
- HG#2 (data integrity): NOT triggered — schema enforces relationships
- HG#3 (migration): NOT triggered — schema is correct as-is
- HG#4 (cross-module): borderline — referenced by 5 SA tables + KG module
- HG#5 (business ambiguity): **borderline** — is_deleted bit vs f_delete_mark int semantic equivalence requires Human Decision

**Hard Gate HG#5**: Pattern divergence requires Decision Brief before any refactor attempt. Currently NOT auto-triggering but flagged for Human Decision at next P8-B stability review.

## 5. Recommended Action

```
DEFERRED — Awaiting HG#5 Decision Brief
```

Recommended Decision Brief contents:
1. Evidence: schema divergence is real (bit vs int, F_ prefix vs bare names)
2. Options:
   - Option A: Keep sa_* tables as separate schema (current state — preserves pattern)
   - Option B: Migrate sa_* to F_* pattern (full Foundry compliance, breaking change)
   - Option C: Foundry Profile extension for SA pattern (compromise — adds complexity)
3. Recommendation: Option A (keep current, Foundry Profile extends)

## 6. Recommended Closure

```
DEFERRED with explicit reason: HG#5 — Pattern divergence requires Human Decision
```

In Shadow Mode, this is recorded as DEFERRED, NOT CLOSED.

## 7. Extension Routing

| Finding | Routing |
|---|---|
| Schema divergence (bit vs int, no F_ prefix) | JNPF Extension (or Foundry Profile Extension) |
| Triple-Key (tenant_id, project_id, pipeline_id) | Triple-Key Iron Law (R12) — Master Spec Evolution (already defined) |
| Pattern Tags / is_pattern_source / llm_confidence | JNPF Extension (SA-specific) |
| Temporal columns (valid_from, valid_to, is_current) | JNPF Extension (SA-specific SCD pattern) |
| human_confirmed (human-AI collaboration) | JNPF Extension (SA-specific) |

## 8. Universal Core Purity

✅ Zero contamination. All SA-specific fields routed to JNPF Extension / Foundry Profile. Triple-Key support is already Master Spec-compliant.

**Important**: This table tests the Skill's ability to recognize "no Entity mapping does NOT mean unimportant" — sa_data_dictionary is critically important despite no C# Entity.
