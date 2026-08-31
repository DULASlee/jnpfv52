# Runtime Impact Analysis — Phase 32

> **Purpose**: Document runtime impact of M32-01 and M32-02 migrations
> **Method**: SqlSugar + Dapper + Repository + Dynamic SQL analysis

---

## 1. SqlSugar ORM Impact

### 1.1 M32-01: `base_signature` PK on `f_id`

**Entity File**: `backend/modularity/system/JNPF.Systems.Entitys/Entity/System/SignatureEntity.cs`

```csharp
[SugarTable("BASE_SIGNATURE")]
public class SignatureEntity : CLDSEntityBase  // CLDSEntityBase provides Id (→ f_id)
{
    [SugarColumn(ColumnName = "F_FULL_NAME")] public string FullName { get; set; }
    [SugarColumn(ColumnName = "F_EN_CODE")]   public string EnCode { get; set; }
    [SugarColumn(ColumnName = "F_ICON")]     public string Icon { get; set; }
    [Navigate(NavigateType.OneToMany, nameof(SignatureUserEntity.SignatureId), nameof(Id))]
    public List<SignatureUserEntity> SignatureUser { get; set; }
}
```

**Impact**: ✅ POSITIVE
- Currently `SignatureEntity` cannot use `Insertable<>().ExecuteCommand()` because no PK
- After M32-01: SqlSugar recognizes `f_id` as PK (inherited from `CLDSEntityBase`)
- All standard CRUD operations (Insertable, Updateable, Deleteable, Selectable) become functional
- Navigation to `SignatureUser` (one-to-many via `SignatureId`) is enabled

**Action Required**: None (PK addition is sufficient; no Entity class change needed)

### 1.2 M32-02: `base_signature_user` Composite PK on `(f_signature_id, f_user_id)`

**Entity File**: `backend/modularity/system/JNPF.Systems.Entitys/Entity/System/SignatureUserEntity.cs`

```csharp
[SugarTable("BASE_SIGNATURE_USER")]
public class SignatureUserEntity : CLDSEntityBase  // f_id assumed PK
{
    [SugarColumn(ColumnName = "F_SIGNATURE_ID")] public string SignatureId { get; set; }
    [SugarColumn(ColumnName = "F_USER_ID")]      public string UserId { get; set; }
}
```

**Impact**: ⚠️ **Entity change REQUIRED**

Current state: `CLDSEntityBase` provides `Id` (mapped to `f_id`) as PK.
Required state: `(f_signature_id, f_user_id)` should be the composite PK.

**Entity change specification** (for Phase 32 bundle):

```csharp
[SugarTable("BASE_SIGNATURE_USER")]
public class SignatureUserEntity : CLDSEntityBase
{
    [SugarColumn(ColumnName = "F_SIGNATURE_ID", IsPrimaryKey = true)]
    public string SignatureId { get; set; }

    [SugarColumn(ColumnName = "F_USER_ID", IsPrimaryKey = true)]
    public string UserId { get; set; }

    [SugarColumn(ColumnName = "F_ENABLED_MARK")] public int? EnabledMark { get; set; }
    [SugarColumn(ColumnName = "F_SORT_CODE")]    public long? SortCode { get; set; }
    // ... other fields
}
```

**Note**: `CLDSEntityBase` provides `Id` (mapped to `f_id`); after composite PK addition, SqlSugar may ignore `Id` and use the explicit `[SugarColumn(IsPrimaryKey = true)]` annotations.

**Verification Required at Phase 33**:
- Test `db.Insertable(entity).ExecuteCommand()` works
- Test `db.Queryable<SignatureUserEntity>().Where(u => u.SignatureId == "X").ToList()` works
- Test `db.Updateable(entity).ExecuteCommand()` works
- Test navigation `SignatureEntity.SignatureUser` works (should be empty for this test since no real data)

---

## 2. Dapper ORM Impact

**Current Usage**: Per Batch 31.4 codebase search, NO Dapper SQL references found for `base_signature` or `base_signature_user`.

**Impact**: None (no existing Dapper queries to break)

**Post-migration**: Future Dapper queries can use:
```sql
-- Single-table query (uses PK)
SELECT * FROM base_signature WHERE f_id = @id;

-- Association query (uses composite PK)
SELECT * FROM base_signature_user WHERE f_signature_id = @sig_id AND f_user_id = @user_id;
```

---

## 3. Repository Pattern Impact

**Current Usage**: `SignatureService.cs` exists in `backend/modularity/system/JNPF.Systems/System/`. Need to inspect for Repository usage.

**Estimated Impact** (based on similar pattern in `base_user`):
- `ISignatureRepository` likely extends `IRepositoryBase<SignatureEntity>`
- This provides default `Insert`, `Update`, `Delete`, `Select` methods
- These methods will now work (previously broken without PK)
- Custom Repository methods need review for composite PK pattern

**Verification Required at Phase 33**:
- Run `SignatureService.GetList()` (or equivalent)
- Run `SignatureService.Save()` 
- Run association methods that touch `base_signature_user`

---

## 4. Dynamic SQL Impact

**Current Usage**: Per Batch 31.4 codebase search, NO dynamic SQL with `WHERE f_signature_id` or `WHERE f_user_id` found in `base_signature` / `base_signature_user` queries.

**Impact**: None

**Post-migration**: Future dynamic SQL can use:
```sql
-- With composite PK on (f_signature_id, f_user_id):
SELECT * FROM base_signature_user WHERE f_signature_id = @sig_id;  -- leftmost prefix works
SELECT * FROM base_signature_user WHERE f_user_id = @user_id;     -- does NOT use composite index
```

If `f_user_id`-only queries become common, consider adding a separate `IX_base_signature_user_user_id` index (separate Phase).

---

## 5. Performance Impact (Estimated)

### 5.1 Empty Tables (current state)

- PK addition: ~8KB metadata overhead per table
- No query performance change (no data to query)

### 5.2 For 1M Association Rows (estimated projection)

| Query | Before PK | After PK (Composite) | Speedup |
|-------|-----------|---------------------|---------|
| `WHERE f_id = @id` (SignatureEntity) | Table scan (no index) | Index seek | ~1000x |
| `WHERE f_signature_id = @id AND f_user_id = @id` (SignatureUserEntity) | Table scan | Index seek on composite | ~1000x |
| `WHERE f_signature_id = @id` (leftmost prefix) | Table scan | Index seek (leftmost) | ~100x |
| `WHERE f_user_id = @id` (NOT leftmost prefix) | Table scan | Table scan | 1x (no improvement) |

**Conclusion**: Composite PK provides performance benefit for natural access patterns. Single-column `f_user_id` queries would need a separate index (deferred to Phase 33+ if pattern emerges).

---

## 6. Migration Lock Impact

### 6.1 Lock Duration

| Table | Current Rows | Estimated Lock Time | Impact |
|-------|--------------|----------------------|--------|
| `base_signature` | 0 | < 100ms | None (empty table) |
| `base_signature_user` | 0 | < 100ms | None (empty table) |

### 6.2 Lock Type

- `ALTER TABLE ... ADD PRIMARY KEY` requires **Schema Modification Lock (Sch-M)**
- Sch-M lock blocks ALL DML (SELECT, INSERT, UPDATE, DELETE) on the table during execution
- For empty tables: < 100ms total
- For non-empty tables (hypothetical): proportional to table size

### 6.3 Production Window

**Recommended**: Run during low-traffic window (off-peak hours) to minimize user-facing impact
**Not critical** for current empty-table state

---

## 7. Tenant Semantics Impact

Both tables have `f_tenant_id` column (per Batch 29 evidence).

**M32-01 PK on `f_id`** does NOT include `f_tenant_id` in PK. Tenant isolation relies on application-layer filter.

**M32-02 Composite PK on `(f_signature_id, f_user_id)`** does NOT include `f_tenant_id` either. Tenant isolation relies on application-layer filter.

**Per IRON-TABLE-04 Security Boundary First**: PK addition does not weaken tenant security. Tenant index (f_tenant_id) is a separate concern, currently DEFERRED per Batch 31.

---

## 8. Summary of Runtime Impact

| Impact Area | M32-01 | M32-02 |
|-------------|--------|--------|
| SqlSugar ORM | ✅ Positive (enables CRUD) | ⚠️ Entity change REQUIRED |
| Dapper | None (no existing usage) | None |
| Repository | ✅ Enables default methods | ⚠️ Custom methods may need review |
| Dynamic SQL | None | None |
| Performance | Index seek enabled (for f_id queries) | Index seek enabled (for composite queries) |
| Lock | < 100ms (empty) | < 100ms (empty) |
| Tenant security | Unchanged | Unchanged |
| **Action required before Phase 33** | None | **Entity class modification** |

---

## 9. Required Verification (Phase 33 Post-Execution)

```csharp
// 1. Insertable test
var sig = new SignatureEntity { FId = "test-id-001", FFullName = "Test" };
db.Insertable(sig).ExecuteCommand();  // Should succeed post-migration

// 2. Queryable test
var retrieved = db.Queryable<SignatureEntity>().InSingle("test-id-001");
Assert.IsNotNull(retrieved);

// 3. Composite PK test for base_signature_user
var sigUser = new SignatureUserEntity { 
    FSignatureId = "sig-001", 
    FUserId = "user-001",
    FEnabledMark = 1 
};
db.Insertable(sigUser).ExecuteCommand();  // Should succeed post-Entity-change

// 4. Duplicate composite rejection
try {
    db.Insertable(new SignatureUserEntity { 
        FSignatureId = "sig-001", 
        FUserId = "user-001"  // Same as above
    }).ExecuteCommand();
    Assert.Fail("Should throw duplicate key violation");
} catch (SqlSugarException ex) {
    Assert.IsTrue(ex.Message.Contains("PRIMARY KEY"));
}

// 5. Navigation test (after inserting data)
var sigWithUsers = db.Queryable<SignatureEntity>()
    .Includes(s => s.SignatureUser)
    .InSingle("test-id-001");
Assert.IsNotNull(sigWithUsers);
Assert.AreEqual(1, sigWithUsers.SignatureUser.Count);
```

**All these tests will be run as part of Phase 34 (Unified Validation).**

---

**STOP. Awaiting Phase 32 Migration Acceptance Gate.**

**Critical Pre-Phase 33 Action**: Entity class modification for `SignatureUserEntity` must be designed and approved before Phase 33 execution.
