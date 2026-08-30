# Foundry.Data Target Profile — table-refactor-expert

**Phase**: 5 — Foundry Target Profile
**Status**: DRAFT → 提交用户审批
**Upstream**: Phase 4 FROZEN
**Downstream**: Phase 6 JNPF Pilot

---

## 0. Purpose

This document adds **Foundry.Data target-specific knowledge** to the Universal `table-refactor-expert` skill.

**Foundry.Data** is a data access library providing `IRepository<T>` / `ISpecification<T>` / `IAuditableEntity` / `ISoftDeleteEntity` / `ITenantEntity` contracts. It is a **target** that the Universal Skill can assess tables against.

**This Target Profile is PROVABLY SEPARABLE from the Universal Core.** The Universal Core (Master Spec + Execution Manual + SKILL.md) operates on pure relational theory. This document is loaded **only after** the Universal Core and maps Foundry.Data concepts to Universal evidence.

If this Target Profile is removed, the Universal Core continues to function correctly on any database/ORM/system.

---

## 1. How to Load This Profile

Per SKILL.md §2.3 (Extension Isolation):

```
1. Universal Core (Master Spec + Execution Manual) — always
2. Project Extension (JNPF Extension) — loaded when target system = JNPF
3. Foundry Target Profile — loaded when target ORM = Foundry.Data
```

When a Foundry.Data table is assessed, the Skill:
1. Loads Universal Core
2. Loads Project Extension if applicable
3. Loads this Target Profile (Foundry.Data contract mappings)
4. Assesses the table using Universal Capabilities A–G
5. Uses Profile mappings to translate Foundry.Data-specific concepts into Universal evidence

---

## 2. Foundry.Data Contract Map

### 2.1 Contract Overview

Foundry.Data provides five primary contracts relevant to table refactoring:

| Contract | Purpose | Relevant Capability |
|---|---|---|
| `IAuditableEntity` | CreatedAt/CreatedBy + ModifiedAt/ModifiedBy | Capability D (Lifecycle — Audit) |
| `ISoftDeleteEntity` | IsDeleted + DeletedAt | Capability D (Lifecycle — Soft-Delete) |
| `ITenantEntity` | TenantId | Capability D (Lifecycle — Tenant) |
| `IRepository<TEntity>` | CRUD + Query + Restore | Capability E (CRUD/Query) |
| `ISpecification<TEntity>` | Filter/pagination pattern | Capability E (Query) |
| `IBulkOperationsCapability` | Bulk insert/update/delete | Capability E (Bulk operations) |
| `ISavepointCapability` | Transaction savepoints | Capability B (Transaction integrity) |
| `IUpsertCapability` | Upsert semantics | Capability E |
| `IQueryableCapability` | `IQueryable<T>` access | Capability E |

### 2.2 IAuditableEntity — Contract

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }        // → Universal: Created timestamp
    string CreatedBy { get; set; }          // → Universal: Created by user
    DateTime? ModifiedAt { get; set; }      // → Universal: Modified timestamp
    string? ModifiedBy { get; set; }        // → Universal: Modified by user
}
```

**Universal Mapping**:

| Foundry Field | Universal Concept | JNPF Equivalent |
|---|---|---|
| `CreatedAt` | Creation timestamp | `CreatorTime` (`F_CREATOR_TIME`) |
| `CreatedBy` | Creation user | `CreatorUserId` (`F_CREATOR_USER_ID`) |
| `ModifiedAt` | Last modification timestamp | `LastModifyTime` (`F_LAST_MODIFY_TIME`) |
| `ModifiedBy` | Last modification user | `LastModifyUserId` (`F_LAST_MODIFY_USER_ID`) |

### 2.3 ISoftDeleteEntity — Contract

```csharp
public interface ISoftDeleteEntity
{
    bool IsDeleted { get; set; }             // → Universal: soft-delete flag
    DateTime? DeletedAt { get; set; }       // → Universal: deletion timestamp
}
```

**Universal Mapping**:

| Foundry Field | Universal Concept | JNPF Equivalent |
|---|---|---|
| `IsDeleted` | Soft-delete flag (boolean) | `DeleteMark` (int: 1=deleted, NULL=active) |
| `DeletedAt` | Deletion timestamp | `DeleteTime` (`F_DELETE_TIME`) |
| `DeletedBy` | Deletion user | **NOT in Foundry contract** — JNPF-specific extension |

**Key Difference**: Foundry uses `bool IsDeleted`; JNPF uses `int? DeleteMark`. The semantic is the same (deleted vs active) but the storage type differs.

### 2.4 ITenantEntity — Contract

```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }           // → Universal: tenant identifier
}
```

**Universal Mapping**:

| Foundry Field | Universal Concept | JNPF Equivalent |
|---|---|---|
| `TenantId` | Tenant identifier | `TenantId` (`F_TENANT_ID`) |

**Note**: Foundry's `ITenantEntity` is the same semantic as JNPF's `TenantId` field. Both represent multi-tenant isolation at the row level.

---

## 3. Foundry → Universal Capability D Mapping (Lifecycle)

### 3.1 Marker Concept → Foundry Contract Mapping

| Universal Marker (Master Spec §6) | Foundry Contract | Foundry Field |
|---|---|---|
| **Audit** | `IAuditableEntity` | `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` |
| **Soft-Delete** | `ISoftDeleteEntity` | `IsDeleted`, `DeletedAt` |
| **Tenant** | `ITenantEntity` | `TenantId` |
| **All three** | `IAuditableEntity` + `ISoftDeleteEntity` + `ITenantEntity` | All above |

### 3.2 Evidence Collection for Foundry Tables

When assessing a Foundry.Data table:

```
1. Check if entity implements IAuditableEntity
   → If YES: Audit Marker = PRESENT
   → Evidence: interface declaration or attribute

2. Check if entity implements ISoftDeleteEntity
   → If YES: Soft-Delete Marker = PRESENT
   → Evidence: interface declaration

3. Check if entity implements ITenantEntity
   → If YES: Tenant Marker = PRESENT
   → Evidence: interface declaration
```

### 3.3 ISoftDeleteEntity vs JNPF Soft-Delete (Key Difference)

| Aspect | Foundry ISoftDeleteEntity | JNPF DeleteMark |
|---|---|---|
| Deleted flag type | `bool IsDeleted` | `int? DeleteMark` |
| Deleted value | `true` | `1` |
| Active value | `false` | `NULL` |
| DeletedAt | `DateTime? DeletedAt` | `DateTime? DeleteTime` |
| DeletedBy | **NOT PROVIDED** | `DeleteUserId` (JNPF extension) |

**Refactoring Implication**: When migrating a Foundry table to JNPF (or vice versa), the soft-delete field type must be converted. This is a **Data Migration** refactor type (Execution Manual §8.1), not a Schema refactor type.

---

## 4. IRepository<TEntity> — Contract

```csharp
public interface IRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : class
{
    // Tracked reads
    Task<TEntity?> FindAsync(object[] keyValues, CancellationToken ct = default);
    Task<PaginatedResult<TEntity>> QueryAsync(
        Specification<TEntity> specification,
        bool includeCount = true,
        CancellationToken ct = default);

    // Writes
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task RemoveAsync(TEntity entity, CancellationToken ct = default);

    // Restore (soft-delete recovery)
    Task RestoreAsync(TEntity entity, CancellationToken ct = default);
}
```

### 4.1 IRepository vs ISqlSugarRepository (JNPF)

| Aspect | Foundry IRepository | JNPF ISqlSugarRepository |
|---|---|---|
| Tracked query | `FindAsync` + `QueryAsync` | SqlSugar change tracker |
| Specification | `ISpecification<TEntity>` pattern | SqlSugar `ISugarQueryable` |
| Add | `AddAsync` | `Insertable<T>.ExecuteCommandAsync()` |
| Update | `UpdateAsync` | `Updateable<T>.ExecuteCommandAsync()` |
| Remove | `RemoveAsync` (soft-delete via RestoreAsync) | Entity `Delete()` method + `Updateable` |
| Restore | `RestoreAsync` | Not a first-class operation |
| Bulk | via `IBulkOperationsCapability` | `Insertable<T>.ExecuteCommand()` (fast) |

### 4.2 Foundry Bulk Operations

```csharp
public interface IBulkOperationsCapability
{
    Task<int> BulkInsertAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken ct = default) where TEntity : class;
    Task<int> BulkUpdateAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken ct = default) where TEntity : class;
    Task<int> BulkDeleteAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken ct = default) where TEntity : class;
}
```

**Universal Mapping**: Bulk operations are a Capability E (CRUD/Query) consideration. When a Foundry table uses `IBulkOperationsCapability`, the refactor assessment should consider:
- Bulk insert → `BulkInsert` refactor type (Execution Manual §8.1)
- Bulk update → `BulkUpdate` refactor type
- Bulk delete → `BulkDelete` refactor type

---

## 5. ISpecification<TEntity> — Contract

```csharp
// Specification<TEntity> is a purely declarative filter/pagination pattern
// It combines: filter + include + orderBy + skip + take
// It is NOT a query executor — it is passed to IRepository.QueryAsync(Specification)
```

### 5.1 Specification Pattern → Universal Evidence

| Foundry Concept | Universal Concept | Evidence Source |
|---|---|---|
| `Specification<TEntity>.Criteria` | WHERE clause | `Where()` expression |
| `Specification<TEntity>.Includes` | JOIN / Include | `Include()` expressions |
| `Specification<TEntity>.OrderBy` | ORDER BY | `OrderBy()` / `OrderByDescending()` |
| `Specification<TEntity>.AsPagination()` | LIMIT/OFFSET | `Skip()` / `Take()` |

### 5.2 Evidence Collection for Specification Pattern

When assessing a Foundry table:
1. Find classes implementing `Specification<TEntity>`
2. Check their `Criteria` — this is the canonical query filter
3. Cross-reference with SQL generated by `QueryAsync`

---

## 6. Foundry Transaction Patterns

### 6.1 ISavepointCapability

```csharp
public interface ISavepointCapability
{
    Task CreateSavepointAsync(string name, CancellationToken ct = default);
    Task RollbackToSavepointAsync(string name, CancellationToken ct = default);
    Task ReleaseSavepointAsync(string name, CancellationToken ct = default);
}
```

### 6.2 Transaction Safety Assessment

When a Foundry table is involved in multi-step transactions:
1. Check if the service uses `ISavepointCapability`
2. Check if transactions span multiple aggregate roots
3. If YES → This is a **Capability B (Integrity)** concern — cross-entity transaction boundaries

---

## 7. Foundry → JNPF Migration Mapping

### 7.1 Entity Contract Conversion Table

| Concept | Foundry Contract | JNPF Implementation | Conversion Type |
|---|---|---|---|
| Audit Created | `IAuditableEntity.CreatedAt` | `CLDEntityBase.CreatorTime` | Timestamp → Timestamp (compatible) |
| Audit CreatedBy | `IAuditableEntity.CreatedBy` | `CLDEntityBase.CreatorUserId` | String → String (compatible) |
| Audit Modified | `IAuditableEntity.ModifiedAt` | `CLDEntityBase.LastModifyTime` | Timestamp → Timestamp (compatible) |
| Audit ModifiedBy | `IAuditableEntity.ModifiedBy` | `CLDEntityBase.LastModifyUserId` | String → String (compatible) |
| Soft-Delete Flag | `ISoftDeleteEntity.IsDeleted` (bool) | `CLDEntityBase.DeleteMark` (int?) | **Type change required** |
| Soft-Delete At | `ISoftDeleteEntity.DeletedAt` | `CLDEntityBase.DeleteTime` | Timestamp → Timestamp (compatible) |
| Soft-Delete By | **Not in Foundry** | `CLDEntityBase.DeleteUserId` | JNPF extension — no Foundry equivalent |
| Tenant | `ITenantEntity.TenantId` | `EntityBase.TenantId` | String → String (compatible) |

### 7.2 Critical Migration Issue: Soft-Delete Type

The most significant migration issue between Foundry and JNPF is the soft-delete field type:

```
Foundry:  bool IsDeleted       (true = deleted, false = active)
JNPF:     int? DeleteMark      (1 = deleted, NULL = active)
```

**Migration approach**:
1. Add new column `delete_mark INT` to Foundry table (or `is_deleted TINYINT` to JNPF table)
2. Write migration: `UPDATE t SET delete_mark = CASE WHEN is_deleted THEN 1 ELSE NULL END`
3. Drop old column
4. This is a **Data Migration** refactor type (Hard Gate #3 applies — data transformation risk)

---

## 8. Foundry-Specific Risk Notes

### 8.1 Soft-Delete Restore Pattern

Foundry's `RestoreAsync` sets `IsDeleted = false` and `DeletedAt = null`. JNPF's equivalent is calling `entity.Delete()` (sets `DeleteMark = 1`) — there is no JNPF restore method in the base class; it must be implemented manually.

**Assessment implication**: A Foundry table that uses `RestoreAsync` implies a **soft-delete lifecycle** that must be preserved in any refactor. This is a Capability D (Lifecycle) finding.

### 8.2 Concurrent Access Risk

Foundry documents that concurrent operations on the same `Repository` are **undefined behavior** (the underlying EF Core DbContext / SqlSugar Client is not thread-safe). This is a **Capability B (Integrity)** concern if the service layer uses async concurrent calls.

**Assessment implication**: If the service layer uses `Task.WhenAll` or concurrent repository calls, this is a Risk R2+ concern.

---

## 9. Profile Version Compatibility

### 9.1 Foundry.Data Version

This Target Profile is compatible with Foundry.Data **V1.0** (Contract FROZEN — 998 tests passing).

### 9.2 Profile vs Contract Versioning

If Foundry.Data contract changes (e.g., adds `DeletedBy` to `ISoftDeleteEntity`):
1. A new Target Profile version is required
2. The old profile remains valid for assessing existing Foundry tables
3. New profile version notes the delta from the old profile

---

## 10. Version History

| Version | Date | Change |
|---|---|---|
| v1.0 (draft) | 2026-08-29 | First Foundry.Data Target Profile for table-refactor-expert. Maps IAuditableEntity / ISoftDeleteEntity / ITenantEntity / IRepository / ISpecification / IBulkOperations to Universal Capabilities. |
