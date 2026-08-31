# JNPF Table Refactoring — Migration Authorization Gate

> **Gate Type**: Migration Authorization Gate (Gate A)
> **Date**: 2026-08-31T17:30:00
> **Plan Reference**: Final Completion Execution Plan v3.0
> **Matrix**: `backend/database/final-refactor/JNPF-Final-Refactoring-Matrix.json`

---

## Gate A — Migration Authorization

### Status: `STOP → AWAITING HUMAN APPROVAL`

Per WAVE 1 completion, AI has prepared all Migration Packages. This Gate requires **Chief Architect authorization** before any schema modification.

---

## 1. Migration Summary

| Gap ID | Table | Migration | Risk | Status | Authorization |
|--------|-------|-----------|------|--------|---------------|
| FR-001 | BASE_SIGNATURE | M32-01: ADD PK (F_ID) | LOW | ✅ READY | **APPROVE/DENY** |
| FR-002 | BASE_SIGNATURE_USER | M32-02: ADD composite PK (F_SIGNATURE_ID, F_USER_ID) | LOW/MEDIUM | ⏸️ DEFERRED | **HUMAN DECISION REQUIRED** |
| FR-003 to FR-017 | 15 tables | Tenant Index | LOW/MEDIUM | ⏸️ DEFERRED | **HUMAN DECISION REQUIRED** |

---

## 2. Approved Migration Package (FR-001 only)

### M32-01: BASE_SIGNATURE Primary Key

**Current State**: No PK constraint on `BASE_SIGNATURE`
**Target State**: `PRIMARY KEY (F_ID) CLUSTERED`
**ORM Contract**: `SignatureEntity` inherits `CLDSEntityBase` → `EntityBase` → `IdEntityBase<string>` with `Id` declared as `[SugarColumn(IsPrimaryKey = true)]`
**Data Safety**: Empty table (0 rows) per batch-31 evidence
**Risk**: LOW — empty table, trivial rollback

**Preflight**: `backend/database/phase-32/migration-preflight.sql` (read-only checks)
**Migration**: `backend/database/phase-32/migration.sql::M32-01`
**Validation**: `backend/database/phase-32/migration-validation.sql`
**Rollback**: `DROP CONSTRAINT PK_base_signature` (instant, no data loss)

**Execution Steps**:
```sql
-- Step 1: Run preflight (read-only)
-- backend/database/phase-32/migration-preflight.sql

-- Step 2: If preflight passes, execute
-- backend/database/phase-32/migration.sql

-- Step 3: Run validation (read-only)
-- backend/database/phase-32/migration-validation.sql
```

---

## 3. Deferred Items Requiring Human Decision

### FR-002: BASE_SIGNATURE_USER Primary Key

**Question**: Composite (F_SIGNATURE_ID, F_USER_ID) vs Surrogate (F_ID)?

| Option | Pros | Cons |
|--------|------|------|
| **A: Composite PK** (F_SIGNATURE_ID, F_USER_ID) | Business semantic correct; association table without surrogate | SqlSugar may not auto-pick composite PK for navigation |
| **B: Surrogate PK** (F_ID) | ORM-consistent; matches CLDSEntityBase convention; SqlSugar navigation works out-of-box | Requires explicit navigation config for SignatureUserEntity |

**Evidence Required for Decision**:
1. SqlSugar `Insertable`/`Updateable` behavior with composite key
2. SignatureEntity navigation test (does `[Navigate]` work with composite FK?)
3. Any existing queries that assume F_ID as PK

**If Option A (Composite)**: Execute `migration.sql::M32-02` after approval
**If Option B (Surrogate)**: New migration script needed for single-column PK

### Tenant Index Decisions (FR-003 to FR-017)

**Problem**: 15 tables flagged for missing tenant indexes, but:
- Many are empty (0 rows)
- Many have NULL tenant values (index useless)
- ORM analysis reveals many entities are NOT tenant-aware despite having TenantId

**Recommendation**: DEFER all 15 until:
1. Production data populated (>100 rows sample)
2. Tenant selectivity verified (>1% distinct)
3. Actual query workload evidence

**Operational Triggers** (for future re-evaluation):
- Production table > 1000 rows
- P95 query latency > 100ms on tenant-filtered queries
- Logical reads > 1000 on tenant predicates

---

## 4. Risk Matrix

| Migration | Table Size | Data Risk | ORM Impact | Rollback Complexity |
|-----------|-----------|-----------|------------|-------------------|
| M32-01 | 0 rows | NONE | REQUIRED for ORM | INSTANT |
| M32-02 (if approved) | 0 rows | NONE | VERIFY navigation | INSTANT |

---

## 5. Migration Dependency Graph

```
BASE_SIGNATURE (no FK dependencies)
└── Can migrate immediately

BASE_SIGNATURE_USER
├── FK from: none (no tables reference base_signature_user as FK)
└── Navigation dependency: SignatureEntity.SignatureUser → uses Id, NOT f_signature_id
    └── IF composite PK: verify [Navigate] works with composite FK
```

**No cross-dependencies**: M32-01 and M32-02 can run independently (but M32-02 is deferred anyway)

---

## 6. Authorization Request

**Chief Architect: Please decide:**

### Decision 1: FR-001 (BASE_SIGNATURE PK)
```
[ ] APPROVE M32-01: Add PK (F_ID) on BASE_SIGNATURE
[ ] DENY (reason: _______________)
```

### Decision 2: FR-002 (BASE_SIGNATURE_USER PK)
```
[ ] APPROVE Option A: Composite PK (F_SIGNATURE_ID, F_USER_ID)
[ ] APPROVE Option B: Surrogate PK (F_ID) — requires new migration script
[ ] KEEP DEFERRED until more evidence
[ ] DENY (reason: _______________)
```

### Decision 3: Tenant Index (FR-003 to FR-017)
```
[ ] APPROVE DEFER ALL — re-evaluate when production data populated
[ ] APPROVE Operational Trigger rules (see §3)
[ ] OTHER instructions: _______________
```

---

## 7. Post-Approval Instructions

After Chief Architect approval:
1. AI will execute preflight checks
2. If preflight passes, execute approved migrations
3. Run validation queries
4. Report results
5. Proceed to WAVE 2 (Runtime Validation) if all pass

---

**AI Status**: `GATE_A_BLOCKED` — awaiting Chief Architect authorization
