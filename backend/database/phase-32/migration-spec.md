# Migration Specification — Phase 32 Bundle

> **Status**: READ-ONLY design (NO DDL)
> **Authorization**: Chief Architect 2026-08-31 Batch 31 Decision Acceptance
> **Date**: 2026-08-31
> **Strict boundary**: This document is design only. No ALTER / DROP / UPDATE until Phase 33 explicitly authorized.

---

## 1. Scope

This bundle covers **2 APPROVED migrations** out of 17 G1_MAJOR Gaps from Batch 31:

| Item | Decision | Migration Type |
|------|----------|-----------------|
| `base_signature` Missing PK | MIGRATION_REQUIRED | Single-column PK on `f_id` (surrogate) |
| `base_signature_user` Missing PK | MIGRATION_REQUIRED | Composite PK on `(f_signature_id, f_user_id)` |

**Excluded from Phase 32**:
- 15 tenant indexes → DEFERRED (insufficient production workload evidence per Batch 31)
- 5 audit field false positives → NO_CHANGE / CLOSED (no migration needed)
- Dynamic tables → 0 (no classification gate required)

---

## 2. M32-01: `base_signature` PK Migration

### 2.1 Current Schema

```sql
CREATE TABLE dbo.base_signature (
    f_id nvarchar(50) NOT NULL,
    f_full_name nvarchar(200) NULL,
    f_en_code nvarchar(50) NULL,
    f_icon nvarchar(max) NULL,
    f_enabled_mark int NULL,
    f_sort_code bigint NULL,
    f_description nvarchar(500) NULL,
    f_creator_time datetime2(7) NULL,
    f_creator_user_id nvarchar(50) NULL,
    f_last_modify_time datetime2(7) NULL,
    f_last_modify_user_id nvarchar(50) NULL,
    f_delete_time datetime2(7) NULL,
    f_delete_user_id nvarchar(50) NULL,
    f_delete_mark int NULL,
    f_tenant_id nvarchar(50) NULL,
    f_zx_system_id nvarchar(50) NULL
    -- NO PRIMARY KEY
);
```

### 2.2 Target Schema

```sql
ALTER TABLE dbo.base_signature
    ADD CONSTRAINT PK_base_signature PRIMARY KEY CLUSTERED (f_id);
```

### 2.3 Pre-conditions (validated by `migration-preflight.sql`)

| Check | Required | Per Batch 31.1 evidence |
|-------|----------|--------------------------|
| Row count | 0 (empty) | ✅ 0 rows |
| `f_id` NOT NULL | 100% | ✅ 16 candidates incl. f_id have 0 NULLs |
| `f_id` UNIQUE | 0 duplicates | ✅ 0 duplicates across 16 candidates |
| Existing FK references to this table | 0 | ✅ 0 FKs (per `sys.foreign_keys` query) |
| Views referencing this table | 0 | ✅ 0 views (per `sys.sql_expression_dependencies` query) |
| Procs/Functions referencing this table | 0 | ✅ 0 procs (per `sys.sql_expression_dependencies` query) |
| Triggers on this table | 0 | ✅ 0 triggers (per `sys.triggers` query) |
| Production code references | SqlSugar Entity | ✅ `SignatureEntity` extends `CLDSEntityBase` (provides f_id) |

### 2.4 Migration SQL

See `migration.sql` lines 21-44.

### 2.5 Rollback SQL

See `rollback.sql` lines 24-37.

### 2.6 Post-validation

See `migration-validation.sql` Validation 1-5.

### 2.7 Runtime Impact

**SqlSugar ORM Impact**:
- `SignatureEntity` is decorated with `[SugarTable("BASE_SIGNATURE")]` (JNPF.Systems.Entitys/Entity/System/SignatureEntity.cs:9)
- Extends `CLDSEntityBase` which provides `Id` (mapped to `f_id`)
- Currently **NO** Insertable/Updateable/Deleteable annotation — PK addition **enables** these operations
- Pre-migration: `db.Insertable(entity).ExecuteCommand()` would FAIL with "no primary key"
- Post-migration: SqlSugar Insertable/Updateable/Deleteable all functional

**Dapper ORM Impact**:
- Dapper requires explicit SQL; no auto-mapping impact
- Existing queries (if any) unaffected

**Navigation Impact**:
- `SignatureEntity` has `[Navigate(NavigateType.OneToMany, nameof(SignatureUserEntity.SignatureId), nameof(Id))]`
- After PK addition, navigation from SignatureEntity to SignatureUserEntity works
- `nameof(Id)` resolves to `f_id` (per `CLDSEntityBase`)

**Performance Impact**:
- Empty table (0 rows): no measurable difference
- For 1M rows: PK index adds ~8KB per page, 1-2ns per lookup
- Tenant semantics (`f_tenant_id`): NOT included in PK (separate tenant index is a separate concern, currently DEFERRED)

### 2.8 Data Safety

| Risk | Mitigation |
|------|-----------|
| Duplicate `f_id` at migration time | Pre-flight check `COUNT(*) - COUNT(DISTINCT f_id) = 0` |
| NULL `f_id` at migration time | Pre-flight check `COUNT(*) WHERE f_id IS NULL = 0` |
| Lock timeout on large table | Currently 0 rows; for >100k rows, schedule maintenance window |
| Schema metadata lock conflict | Run during low-traffic window |

### 2.9 Migration Complexity

**LOW**:
- 0 rows of data
- No FK references
- No views/procs
- SqlSugar-compatible
- Single ALTER TABLE statement
- Estimated execution time: < 100ms

---

## 3. M32-02: `base_signature_user` Composite PK Migration

### 3.1 Current Schema

```sql
CREATE TABLE dbo.base_signature_user (
    f_id nvarchar(50) NOT NULL,
    f_signature_id nvarchar(50) NULL,
    f_user_id nvarchar(50) NULL,
    f_enabled_mark int NULL,
    f_sort_code bigint NULL,
    f_description nvarchar(500) NULL,
    f_creator_time datetime2(7) NULL,
    f_creator_user_id nvarchar(50) NULL,
    f_last_modify_time datetime2(7) NULL,
    f_last_modify_user_id nvarchar(50) NULL,
    f_delete_time datetime2(7) NULL,
    f_delete_user_id nvarchar(50) NULL,
    f_delete_mark int NULL,
    f_tenant_id nvarchar(50) NULL,
    f_zx_system_id nvarchar(50) NULL
    -- NO PRIMARY KEY
);
```

### 3.2 Target Schema

```sql
ALTER TABLE dbo.base_signature_user
    ADD CONSTRAINT PK_base_signature_user PRIMARY KEY CLUSTERED (f_signature_id, f_user_id);
```

### 3.3 Composite vs Surrogate Trade-off (Chief Architect 2026-08-31 decision)

| Option | Pros | Cons |
|--------|------|------|
| **Composite (f_signature_id, f_user_id)** ✓ CHOSEN | Matches Signature↔User association semantic; no surrogate id needed; prevents duplicate signatures from same user | Requires SqlSugar composite key config; standard SqlSugar [SugarColumn] doesn't auto-detect |
| Surrogate (f_id) | Simple SqlSugar default | Adds surrogate id to association table (anti-pattern); loses natural uniqueness constraint |

**Rationale**: This table is a pure association (Signature ↔ User) per `SignatureEntity` Navigate annotation. Composite PK enforces business invariant: a user can have at most one relationship with a signature.

### 3.4 Pre-conditions (validated by `migration-preflight.sql`)

| Check | Required | Per Batch 31.1 evidence |
|-------|----------|--------------------------|
| Row count | 0 (empty) | ✅ 0 rows |
| `f_signature_id` NOT NULL | 100% | ✅ 0 NULLs |
| `f_user_id` NOT NULL | 100% | ✅ 0 NULLs |
| `(f_signature_id, f_user_id)` UNIQUE | 0 duplicate pairs | ✅ 0 duplicate pairs |
| Existing FK references | 0 | ✅ 0 FKs |
| Views referencing this table | 0 | ✅ 0 views |
| Procs/Functions | 0 | ✅ 0 procs |

### 3.5 Migration SQL

See `migration.sql` lines 50-79.

### 3.6 Rollback SQL

See `rollback.sql` lines 14-23.

### 3.7 Post-validation

See `migration-validation.sql` Validation 1-7.

### 3.8 Runtime Impact (CRITICAL ANALYSIS)

**SqlSugar ORM Impact — REQUIRES ENTITY UPDATE**:

The current `SignatureUserEntity` (JNPF.Systems.Entitys/Entity/System/SignatureUserEntity.cs) uses:
- `f_id` as primary key (inferred from `CLDSEntityBase`)
- `f_signature_id` and `f_user_id` as regular columns

**For composite PK to work with SqlSugar, the Entity needs explicit configuration**:

```csharp
[SugarTable("BASE_SIGNATURE_USER")]
public class SignatureUserEntity : CLDSEntityBase
{
    [SugarColumn(ColumnName = "F_SIGNATURE_ID", IsPrimaryKey = true)]
    public string SignatureId { get; set; }

    [SugarColumn(ColumnName = "F_USER_ID", IsPrimaryKey = true)]
    public string UserId { get; set; }
    // ... other fields
}
```

**This Entity change is REQUIRED for SqlSugar to recognize the composite PK.**

**CRITICAL: Per Master Plan v2.1 §14 — Entity change is part of Phase 32 (Migration Specification), NOT Phase 33 (Execution)**. Entity changes are design specifications at this stage; the actual `SignatureUserEntity.cs` modification must wait for Phase 32 Acceptance Gate.

**Dapper ORM Impact**:
- Dapper SQL is explicit; no auto-mapping change
- Existing queries: NONE found in current codebase search
- New queries must use composite key in WHERE clauses

**Navigation Impact**:
- `SignatureEntity` has `Navigate(NavigateType.OneToMany, nameof(SignatureUserEntity.SignatureId), nameof(Id))`
- After composite PK addition, navigation semantics change:
  - Pre-migration: Many SignatureUserEntity rows per SignatureEntity (via f_signature_id)
  - Post-migration: Same (composite PK doesn't affect FK relationship)
- Navigation still works as long as Entity is properly annotated

**Performance Impact**:
- Empty table: no measurable difference
- For 1M association rows: composite index adds ~8KB per page, 2-3ns per composite lookup
- Single-column `f_signature_id` queries: can use index seek on first column of composite
- Single-column `f_user_id` queries: CANNOT use this index efficiently (composite index is leftmost-prefix)

### 3.9 Data Safety

| Risk | Mitigation |
|------|-----------|
| Duplicate `(signature_id, user_id)` at migration | Pre-flight check |
| NULL `f_signature_id` or `f_user_id` | Pre-flight check (both required) |
| Lock timeout | Empty table; instant migration |
| SqlSugar compatibility | Entity change REQUIRED (see §3.8) — must be done as part of Phase 32 bundle |
| Query pattern regression | `f_user_id`-only queries would no longer use this index; need separate index if pattern exists |

### 3.10 Migration Complexity

**MEDIUM**:
- 0 rows of data (safe data-wise)
- BUT requires Entity class modification for SqlSugar composite key support
- Rollback requires reverting Entity class + dropping constraint
- Estimated execution time: < 100ms (DDL only)

---

## 4. Bundle Order & Atomicity

```text
Order: M32-01 first, then M32-02
Atomicity: M32-01 + M32-02 in single TRANSACTION (rollback on any failure)
```

Per `migration.sql`, both migrations are in a single BEGIN TRANSACTION / COMMIT block. If M32-02 fails (e.g., duplicate composite pairs found in real data), M32-01 is also rolled back.

For empty tables (current state), this is safe and atomic.

---

## 5. Phase 32 → Phase 33 Handoff

**Required for Phase 33 Authorization**:
1. `migration-spec.md` (this file) ✓
2. `migration-preflight.sql` ✓ (passes with current data)
3. `migration.sql` ✓ (reviewed, idempotent)
4. `rollback.sql` ✓ (reverse order verified)
5. `migration-validation.sql` ✓ (all checks defined)
6. `runtime-impact.md` ✓ (SqlSugar + Dapper + navigation analyzed)
7. `migration-evidence-plan.md` ✓
8. `phase-32-handoff.md` ✓
9. **Entity class change spec for `SignatureUserEntity`** (REQUIRED before Phase 33)
10. **Pre-flight execution result** (current state: 0 rows, all checks pass)

---

**STOP. Awaiting Phase 32 Migration Acceptance Gate.**

No DDL will be executed until Chief Architect approval.
