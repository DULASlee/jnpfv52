# JNPF Extension — table-refactor-expert

**Phase**: 5 — JNPF Extension
**Status**: DRAFT → 提交用户审批
**Upstream**: Phase 4 FROZEN
**Downstream**: Phase 6 JNPF Pilot

---

## 0. Purpose

This document adds **JNPF-specific knowledge** to the Universal `table-refactor-expert` skill.

**This Extension is PROVABLY SEPARABLE from the Universal Core.** The Universal Core (Master Spec + Execution Manual + SKILL.md) operates on pure relational theory with zero JNPF vocabulary. This document is loaded **only after** the Universal Core and is **read-only** in scope — it adds mapping knowledge, it does not change any Universal Rule.

If this Extension is removed, the Universal Core continues to function correctly on any database/ORM/system.

---

## 1. How to Load This Extension

Per SKILL.md §2.3 (Extension Isolation):

```
1. Universal Core (Master Spec + Execution Manual) — always
2. JNPF Extension (this document) — loaded when target system = JNPF
3. Foundry Target Profile — loaded when target ORM = Foundry.Data
```

When a JNPF table is assessed, the Skill:
1. Loads Universal Core
2. Loads this Extension (JNPF-specific mappings)
3. Loads Foundry Target Profile if applicable
4. Assesses the table using Universal Capabilities A–G
5. Uses Extension mappings to translate JNPF-specific concepts into Universal evidence

---

## 2. JNPF Entity Hierarchy

JNPF uses a layered entity base class hierarchy. All audit fields use `F_*` naming convention (uppercase).

### 2.1 Complete Hierarchy Map

```
IEntity<TKey>
└── IdEntityBase<TKey>                        [Id only]
    └── FwCLDEntityBase                        [Id — no TenantId, no ZxSystemId]
        └── FwCLDSEntityBase                   [+ EnabledMark]
    └── EntityBase<TKey>                      [Id + TenantId + ZxSystemId]
        └── CLDEntityBase                      [+ 8 CLD fields]
            └── CLDSEntityBase                 [+ EnabledMark]
    └── TenantEntityBase<TKey>                [Id + TenantId — no ZxSystemId]
        └── TenantCLDSEntityBase              [+ 8 CLD fields + EnabledMark]
    └── SystemEntityBase<TKey>                [Id + ZxSystemId — no TenantId]
        └── SystemCLDSEntityBase              [+ 8 CLD fields + EnabledMark]
```

### 2.2 Base Class Fields (Minimal — Id only)

| Class | Key fields | SQL Column |
|---|---|---|
| `IdEntityBase<TKey>` | `Id` | `F_ID` |
| `EntityBase<TKey>` | `Id` + `TenantId` + `ZxSystemId` | `F_ID` + `F_TENANT_ID` + `F_ZX_SYSTEM_ID` |
| `TenantEntityBase<TKey>` | `Id` + `TenantId` | `F_ID` + `F_TENANT_ID` |
| `SystemEntityBase<TKey>` | `Id` + `ZxSystemId` | `F_ID` + `F_ZX_SYSTEM_ID` |

### 2.3 CLDEntityBase — 9 Audit Fields

`CLDEntityBase` adds 9 audit fields to its parent (`EntityBase`):

| Field | C# Property | SQL Column | Description |
|---|---|---|---|
| Creator | `CreatorTime` | `F_CREATOR_TIME` | Creation timestamp |
| Creator | `CreatorUserId` | `F_CREATOR_USER_ID` | User who created the record |
| Modifier | `LastModifyTime` | `F_LAST_MODIFY_TIME` | Last modification timestamp |
| Modifier | `LastModifyUserId` | `F_LAST_MODIFY_USER_ID` | User who last modified |
| Deleter | `DeleteMark` | `F_DELETE_MARK` | Soft-delete flag (1 = deleted, NULL = active) |
| Deleter | `DeleteTime` | `F_DELETE_TIME` | Soft-delete timestamp |
| Deleter | `DeleteUserId` | `F_DELETE_USER_ID` | User who deleted |
| General | `SortCode` | `F_SORT_CODE` | Sort order |

**Total fields on `CLDEntityBase`**: 9 (inherited 3 from EntityBase + 9 = 12 base fields before adding EnabledMark).

### 2.4 CLDSEntityBase — +EnabledMark

`CLDSEntityBase` extends `CLDEntityBase` adding one field:

| Field | C# Property | SQL Column | Description |
|---|---|---|---|
| Status | `EnabledMark` | `F_ENABLED_MARK` | Enable flag (1 = enabled, 0 = disabled, NULL = enabled) |

### 2.5 TenantCLDSEntityBase

`TenantCLDSEntityBase` extends `TenantEntityBase` (not `EntityBase`) and adds:
- 8 CLD fields (same as CLDEntityBase)
- `EnabledMark`

**Note**: `TenantCLDSEntityBase` does NOT have `ZxSystemId` (unlike `CLDSEntityBase` which inherits it from `EntityBase`).

### 2.6 SystemCLDSEntityBase

`SystemCLDSEntityBase` extends `SystemEntityBase` (not `EntityBase`) and adds:
- 8 CLD fields (same as CLDEntityBase)
- `EnabledMark`

**Note**: `SystemCLDSEntityBase` does NOT have `TenantId` (unlike `CLDSEntityBase` which inherits it from `EntityBase`).

### 2.7 FwCLDEntityBase / FwCLDSEntityBase

Framework-level base classes. No `TenantId`, no `ZxSystemId`. Used for framework-level system tables.

- `FwCLDEntityBase`: Id + 8 CLD fields
- `FwCLDSEntityBase`: Id + 8 CLD fields + EnabledMark

---

## 3. JNPF Marker Concepts → Universal Capability D Mapping

### 3.1 Marker Concept Mapping Table

| Universal Concept (Master Spec §6) | JNPF Implementation | SQL Column |
|---|---|---|
| **Tenant** | `ITenantFilter` + `TenantId` field | `F_TENANT_ID` |
| **Soft-Delete** | `DeleteMark` = 1 (int, not bool) + `DeleteTime` + `DeleteUserId` | `F_DELETE_MARK` + `F_DELETE_TIME` + `F_DELETE_USER_ID` |
| **Audit** | `CreatorTime` + `CreatorUserId` + `LastModifyTime` + `LastModifyUserId` | `F_CREATOR_TIME` + `F_CREATOR_USER_ID` + `F_LAST_MODIFY_TIME` + `F_LAST_MODIFY_USER_ID` |
| **Sort Code** | `SortCode` | `F_SORT_CODE` |
| **Enabled Mark** | `EnabledMark` (1=enabled, 0=disabled, NULL=enabled) | `F_ENABLED_MARK` |
| **System Isolation** | `IZxSystemFilter` + `ZxSystemId` | `F_ZX_SYSTEM_ID` |

### 3.2 JNPF Soft-Delete vs Standard Soft-Delete

**JNPF uses an integer flag, not a boolean:**

| Aspect | Standard | JNPF |
|---|---|---|
| Column type | `IsDeleted BOOLEAN` | `DeleteMark INT` |
| Deleted value | `true` | `1` |
| Active value | `false` | `NULL` |
| DeletedAt | `DELETED_AT TIMESTAMP` | `F_DELETE_TIME TIMESTAMP` |
| DeletedBy | `DELETED_BY VARCHAR` | `F_DELETE_USER_ID VARCHAR` |

**Evidence collection for JNPF soft-delete:**
- Check `[SugarColumn(ColumnName = "F_DELETE_MARK")]` on the C# entity
- If `DeleteMark` is `int?` and mapped to `F_DELETE_MARK INT`, the soft-delete concept is present
- Query filter: `WHERE DeleteMark != 1` (not `WHERE IsDeleted = false`)

---

## 4. ITenantFilter — Multi-Tenant Isolation

### 4.1 What ITenantFilter Does

`ITenantFilter` is a SqlSugar global filter that **automatically injects** `F_TENANT_ID = currentTenantId` into every query. It is registered at application startup and cannot be bypassed by application code.

### 4.2 How to Identify ITenantFilter Presence

| Check | Method | Evidence |
|---|---|---|
| C# entity | Class extends `EntityBase<TKey>` or `TenantEntityBase<TKey>` or `TenantCLDSEntityBase` | `[SugarColumn(ColumnName = "F_TENANT_ID")]` present on `TenantId` property |
| SQL column | DDL has `F_TENANT_ID` column | `F_TENANT_ID VARCHAR` present |
| Query filter | SqlSugar query log shows `WHERE F_TENANT_ID = @currentTenantId` | Runtime evidence |

### 4.3 ITenantFilter Risk Implication

Per Master Spec §6.1 (Tenant concept): Every SqlSugar query MUST verify `ITenantFilter` is active. Missing filter = cross-tenant data leak.

**In JNPF**: The filter is always active for entities extending `EntityBase` / `TenantEntityBase`. There is no code path to disable it. This is a **JNPF design guarantee** — the risk of cross-tenant leak via missing filter is architecturally prevented.

**Extension Exception**: For `FwCLDEntityBase` / `FwCLDSEntityBase` (no TenantId), ITenantFilter is not applied. These are framework-internal tables.

### 4.4 Extension Rule

When assessing a JNPF table:

```
IF entity extends EntityBase OR TenantEntityBase OR TenantCLDSEntityBase:
    → ITenantFilter is PRESENT
    → Tenant isolation requirement = SATISFIED by architecture
    → Capability D (Tenant) = MET without further evidence
ELSE IF entity extends IdEntityBase OR FwCLDEntityBase:
    → ITenantFilter is NOT applicable (framework table)
    → Capability D (Tenant) = N/A
```

---

## 5. JNPF Table Naming Convention

### 5.1 Standard JNPF Table Naming

| Module | Prefix | Example |
|---|---|---|
| System | `BASE_` | `BASE_USER`, `BASE_ROLE`, `BASE_DEPT` |
| Flow/Workflow | `WF_` | `WF_TASK`, `WF_INSTANCE` |
| Form | `FORM_` | `FORM_INSTANCE` |
| Code Gen | `CODegen_` | `CODegen_PROJECT` |
| Visual Dev | `VISUALDEV_` | `VISUALDEV_PROJECT` |

### 5.2 Field Naming Convention

All JNPF fields use `F_*` prefix with `UPPER_SNAKE_CASE`:

| Pattern | Example |
|---|---|
| Primary key | `F_ID` |
| Tenant FK | `F_TENANT_ID` |
| User FK | `F_CREATOR_USER_ID`, `F_LAST_MODIFY_USER_ID`, `F_DELETE_USER_ID` |
| Timestamp | `F_CREATOR_TIME`, `F_LAST_MODIFY_TIME`, `F_DELETE_TIME` |
| Sort | `F_SORT_CODE` |
| Enabled | `F_ENABLED_MARK` |
| Delete | `F_DELETE_MARK`, `F_DELETE_TIME`, `F_DELETE_USER_ID` |

---

## 6. JNPF ID Generation — Snowflake

### 6.1 Snowflake ID in JNPF

JNPF uses `SnowflakeIdHelper.NextId()` for ID generation. The result is a **long** (64-bit), but it is stored as a **string** in the database (`VARCHAR` or `NVARCHAR`).

### 6.2 Evidence Collection for Snowflake ID

| Aspect | Evidence |
|---|---|
| ID type in C# | `public TKey Id { get; set; }` — typically `string` when using Snowflake |
| ID column in DB | `F_ID VARCHAR(50)` or `F_ID BIGINT` |
| Snowflake call site | `Id = SnowflakeIdHelper.NextId()` in entity `Creator()` / `Create()` method |
| ID generator class | `SnowflakeIdHelper` in `JNPF.Common.Security` |

### 6.3 Extension Rule

When the entity's `Creator()` method contains `SnowflakeIdHelper.NextId()`:
- **This is a JNPF-specific ID generation strategy**
- It does not affect the Universal Capability assessment (Capability A: ID generation is a detail of the implementation, not a schema correctness concern)
- It IS relevant for: data migration planning (converting string IDs to BIGINT or vice versa would be a Destructive change — Hard Gate #3 applies)

---

## 7. JNPF Repository — ISqlSugarRepository<T>

### 7.1 JNPF Uses ISqlSugarRepository, Not IRepository

JNPF does NOT use `IRepository<T>` from Foundry.Data. JNPF uses `ISqlSugarRepository<T>`:

```
// JNPF repository interface (not the same as Foundry.Data IRepository<T>)
public interface ISqlSugarRepository<T> : IRepository<T> where T : class, new()
```

### 7.2 SqlSugar-Specific Patterns

| Pattern | JNPF Implementation |
|---|---|
| Query | `SqlSugarQueryable<T>.Where(...)` with auto ITenantFilter |
| Insert | `Db.Insertable<T>(entity).ExecuteCommandAsync()` |
| Update | `Db.Updateable<T>(entity).ExecuteCommandAsync()` |
| Delete (soft) | Entity `Delete()` method sets `DeleteMark=1`, then `Updateable` |
| Delete (hard) | `Db.Deleteable<T>(expression).ExecuteCommandAsync()` — rare |
| Bulk | `Db.FastSyantax.Insert(List<T>)` — SqlSugar fast insert |

### 7.3 Soft-Delete Query Pattern in JNPF

JNPF does NOT use a global query filter for soft-delete in the traditional sense. Instead:
- `Delete()` method on the entity sets `DeleteMark = 1`
- All query code should include `.Where(x => x.DeleteMark != 1)`
- The framework provides `ISqlSugarRepository<T>.AsQueryable()` which should apply this automatically

**Evidence collection**: Check if service/repository code contains `DeleteMark != 1` or `DeleteMark == 1` filters.

---

## 8. Extension Findings — Capability D (Lifecycle) Mapping

### 8.1 JNPF Marker Concept Completeness

| Marker | Present in JNPF? | JNPF Implementation |
|---|---|---|
| Tenant | ✅ Yes | `F_TENANT_ID` + `ITenantFilter` |
| Soft-Delete | ✅ Yes | `F_DELETE_MARK` (int flag) + `F_DELETE_TIME` + `F_DELETE_USER_ID` |
| Audit | ✅ Yes | Full CLD (CreatorTime/User, LastModifyTime/User) |
| Tenant + Soft-Delete | ✅ Both | `TenantCLDSEntityBase` |
| All three | ✅ | `TenantCLDSEntityBase` |
| None (framework) | ✅ | `FwCLDSEntityBase` |

### 8.2 Extension-Specific Risk Notes

| Finding | JNPF-Specific Risk | Risk Grade |
|---|---|---|
| `DeleteMark` is `INT`, not `BOOLEAN` | Converting to `BOOLEAN` requires schema change + data migration | R3 |
| `EnabledMark` NULL = enabled (not disabled) | Default value interpretation differs from typical bit convention | R1 |
| ITenantFilter is architectural guarantee | Cannot be disabled — cross-tenant leak risk is architecturally mitigated | R1 |
| `FwCLDEntityBase` has no Tenant | Framework tables are system-wide — this is intentional | R2 |

---

## 9. Extension Findings — Capability G (Target Readiness) Mapping

### 9.1 JNPF Entity → Target Readiness Classification

| JNPF Base Class | Universal Marker Concept Status | Target Readiness |
|---|---|---|
| `CLDEntityBase` | Tenant + Audit + Soft-Delete | Adapter-Ready (multi-tenant) |
| `CLDSEntityBase` | Tenant + Audit + Soft-Delete + EnabledMark | Adapter-Ready (multi-tenant + status) |
| `TenantCLDSEntityBase` | Tenant + Audit + Soft-Delete + EnabledMark | Adapter-Ready (multi-tenant + status) |
| `SystemCLDSEntityBase` | System + Audit + Soft-Delete + EnabledMark | System-Scoped |
| `FwCLDEntityBase` | None (framework only) | Framework-Internal |
| `FwCLDSEntityBase` | None (framework only) | Framework-Internal |
| `IdEntityBase` | None | Bare |

### 9.2 Extension Rule for Target Readiness

When assessing a JNPF table, the Skill maps the base class to Target Readiness as follows:

```
CLDEntityBase / CLDSEntityBase / TenantCLDSEntityBase:
    → Marker Concepts: Tenant + Audit + Soft-Delete
    → Target Readiness = Adapter-Ready (can integrate with Foundry IAuditableEntity / ISoftDeleteEntity)

SystemCLDSEntityBase:
    → Marker Concepts: System + Audit + Soft-Delete
    → Target Readiness = System-Scoped (same-system reference only)

FwCLDEntityBase / FwCLDSEntityBase:
    → Marker Concepts: None
    → Target Readiness = Framework-Internal (not refactored as part of application table work)

IdEntityBase:
    → Marker Concepts: None
    → Target Readiness = Bare (requires Marker Concept injection before integration)
```

---

## 10. Extension Findings — Capability F (DDD) Mapping

### 10.1 JNPF Aggregate Patterns

| Pattern | JNPF Implementation | Universal Equivalent |
|---|---|---|
| Aggregate Root | Entity extending a base class, with repository | Aggregate Root |
| Child Entity | Same base class, referenced via FK | Child Entity |
| Reference Data | Small table, `FwCLDEntityBase` or `IdEntityBase`, no soft-delete | Reference Data |
| Framework Table | `FwCLDSEntityBase`, system-wide | Framework-Internal |

### 10.2 JNPF Module Table Prefixes (for Aggregate identification)

| Prefix | Module | Likely Aggregate |
|---|---|---|
| `BASE_` | System | User, Role, Department, Dictionary — these are aggregates |
| `WF_` | Workflow | Task, Instance — workflow engine aggregates |
| `FORM_` | Form | Form definition + instances — separate aggregates |
| `CODegen_` | Code Gen | Project, Table — code generation aggregates |

---

## 11. Extension Limitations

### 11.1 What This Extension Does NOT Cover

1. **JNPF Workflow Engine tables** — WF_* tables are managed by the workflow engine; refactoring them may have unintended side effects on running流程.
2. **JNPF Dynamic Form tables** — FORM_* tables generated by the form designer; schema changes may be overwritten by the designer.
3. **JNPF Visual Dev tables** — VISUALDEV_* tables are visual development artifacts.
4. **JNPF Code Gen tables** — CODegen_* tables are code generation metadata.

### 11.2 Extension Exception Label

Any finding that is JNPF-specific (not covered by Universal Core) must be labeled:

```
[EXTENSION EXCEPTION — JNPF-specific]
```

This label indicates the finding is JNPF-specific knowledge, not a Universal Rule violation.

---

## 12. Version History

| Version | Date | Change |
|---|---|---|
| v1.0 (draft) | 2026-08-29 | First JNPF Extension for table-refactor-expert. Maps entity hierarchy, ITenantFilter, Marker Concepts, naming conventions. |
