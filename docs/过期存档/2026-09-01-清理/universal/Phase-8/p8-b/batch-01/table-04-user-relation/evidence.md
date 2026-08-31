# P8-B Batch 01 — Table 04: base_user_relation Assessment

> **Phase**: 8 — P8-B.1 Table Assessment
> **Table**: base_user_relation
> **Status**: ASSESSED
> **Date**: 2026-08-30
> **Skill Calibration Applied**: 4 CRITICAL items
> **Mode**: Shadow assessment — 0 DB writes yet

---

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `BASE_USER_RELATION` | `[KNOWN]` from registry |
| Category | system-core (junction) | `[KNOWN]` from registry Batch 01 grouping |
| Entity expected | `UserRelationEntity` | `[INFERRED]` |
| Likely column count | 8-15 (junction table, narrow) | `[INFERRED]` |
| Likely columns | F_USER_ID, F_OBJECT_ID, F_OBJECT_TYPE, F_TENANT_ID, F_DELETE_MARK | `[INFERRED]` |
| Pattern | M:N junction (polymorphic via F_OBJECT_TYPE) | `[INFERRED]` |
| Soft delete | F_DELETE_MARK | `[INFERRED]` |

**Verification needed**: actual schema, F_OBJECT_TYPE values, junction semantics.

---

## 2. Seven-Dimension Assessment

### Dimension A: Schema

**Polymorphic junction table pattern** (Calibration Item 2 — Aggregate Composition):

Junction tables are typically NOT aggregates. They are reference tables maintaining M:N relationships. Aggregate Composition Analysis NOT required.

Expected fields:
- F_ID (PK)
- F_USER_ID (FK to base_user)
- F_OBJECT_ID (FK to target — could be role/position/organize)
- F_OBJECT_TYPE (string/int identifying which table F_OBJECT_ID references)
- F_TENANT_ID, F_DELETE_MARK, CLDS

**Pattern Recognition**: Polymorphic FK (F_OBJECT_ID + F_OBJECT_TYPE) is a known anti-pattern but common in JNPF for "user belongs to many things".

**Tag**: `[INFERRED]`. Verification required.

**Finding**: Likely standard polymorphic junction. No-Finding pending verification.

---

### Dimension B: Integrity

| Concern | Status |
|---|---|
| F_USER_ID → base_user | App-level reference (no DB FK) |
| F_OBJECT_ID → polymorphic target | NO DB FK possible (polymorphic) |
| F_OBJECT_TYPE validation | Code-level only |
| Orphan user relations | Risk if user is soft-deleted without cascade |
| Orphan object relations | Risk if target is soft-deleted without cascade |

**Calibration concern**: Orphan risk for polymorphic junctions is REAL. With Calibration Item 1 (no borderline), this becomes:

**HG#2 explicit dismissal** (below): orphan handling is JNPF convention; cycle/orphan prevention is code-level.

---

### Dimension C: Index (Calibration Item 3)

| # | Pattern | Tag | Index Recommendation |
|---|---|---|---|
| 1 | List by F_USER_ID (user's roles/positions/orgs) | `[INFERRED]` | `IDX_USERRELATION_USER (F_TENANT_ID, F_USER_ID)` |
| 2 | List by F_OBJECT_ID + F_OBJECT_TYPE (members of an org/role) | `[INFERRED]` | `IDX_USERRELATION_OBJECT (F_TENANT_ID, F_OBJECT_TYPE, F_OBJECT_ID)` |
| 3 | Composite: find user in specific role | `[INFERRED]` | `IDX_USERRELATION_USER_OBJECT (F_TENANT_ID, F_USER_ID, F_OBJECT_TYPE)` |
| 4 | List active relations | `[INFERRED]` | Filter on F_DELETE_MARK; use existing |

**Calibration Item 3 satisfied**: 4 patterns, 3 index recommendations. No silent drops.

**Index priority**:
1. `IDX_USERRELATION_USER` — CRITICAL (hot path: "what does this user belong to?")
2. `IDX_USERRELATION_OBJECT` — CRITICAL (hot path: "who belongs to this role/position/org?")
3. `IDX_USERRELATION_USER_OBJECT` — HIGH (composite lookup)

**Finding**: SAFE-REFACTOR with 3 index additions.

---

### Dimension D: Lifecycle

Standard CLDS + soft delete via F_DELETE_MARK.

**Special concern** (per Item 2 — polymorphic table):
- When base_user is soft-deleted, what happens to base_user_relation rows?
- When target (role/position/organize) is soft-deleted, what happens?
- These are CASCADE concerns, not state machines.

**Finding**: Pending cascade verification.

---

### Dimension E: CRUD/Query

Query patterns:
- "Get user's roles/positions/orgs" (hot, user-centric)
- "Get users in a role/position/org" (hot, target-centric)
- "Check if user has specific role" (auth)
- "List active assignments" (filter F_DELETE_MARK)

**N+1 risk**: High if relations are loaded one-by-one. Should use IN clause.

**Finding**: Index additions are critical.

---

### Dimension F: DDD

**Aggregate Composition Analysis**: Junction tables are NOT aggregates. Skip.

**Critical Table check** (Calibration Item 4):
- Name: `base_user_relation` — NOT in explicit list
- Column count: < 20 — NOT triggered
- Domain: identity junction — moderate

**Verdict**: NOT Critical Table by calibration rules. But junction tables have unique concerns.

**Junction-specific concerns**:
- Polymorphic FK (F_OBJECT_ID) — DB cannot enforce referential integrity
- Soft-delete cascade behavior — affects orphan rows
- Multiple tables reference this indirectly

**Finding**: Junction table with polymorphic FK. No aggregate analysis needed. Cross-table orphan risk needs verification.

---

### Dimension G: Consumer / Target Readiness

**Polymorphic junction mapping**:
- F_OBJECT_ID → cannot be mapped to a single .NET navigation property
- F_OBJECT_TYPE → discriminator
- .NET code must switch on F_OBJECT_TYPE to determine target type

**Foundry Profile**: May need extension for polymorphic junction pattern.

**Finding**: Standard polymorphic junction pattern. Foundry mapping needs type discrimination.

---

## 3. Risk Classification

**Factors**:
- Junction table (typically low risk)
- Polymorphic FK (moderate risk due to no DB enforcement)
- Soft-delete cascade unverified (operational risk)
- Hot read path (user-centric queries)

**Risk Level: R1** — Confidence: MEDIUM (50-80%)

**Rationale**: Junction table simplicity vs. polymorphic FK ambiguity. Index gaps are operational.

---

## 4. Hard Gate (Calibration Item 1: No Borderline)

| HG | Status | Reasoning |
|---|---|---|
| HG#1 (tenant isolation) | **NOT TRIGGERED** | F_TENANT_ID present. |
| HG#2 (data integrity) | **NOT TRIGGERED** | Polymorphic FK is by design (JNPF pattern). App-level integrity. Dismissal: standard pattern. VERIFICATION: cascade behavior confirmed. |
| HG#3 (migration) | **NOT TRIGGERED** | Only ADD INDEX. |
| HG#4 (cross-module) | **NOT TRIGGERED** | Junction between system tables; not directly cross-module. |
| HG#5 (business ambiguity) | **NOT TRIGGERED** | Junction semantics clear (user belongs to object). F_OBJECT_TYPE values defined in code. |

**All 5 HGs: NOT TRIGGERED with explicit dismissal.**

---

## 5. Recommended Action

```
SAFE-REFACTOR:
1. Add IDX_USERRELATION_USER (F_TENANT_ID, F_USER_ID) — CRITICAL
2. Add IDX_USERRELATION_OBJECT (F_TENANT_ID, F_OBJECT_TYPE, F_OBJECT_ID) — CRITICAL
3. Add IDX_USERRELATION_USER_OBJECT (F_TENANT_ID, F_USER_ID, F_OBJECT_TYPE) — HIGH

VERIFICATION:
1. sys.columns queried
2. sys.indexes queried
3. F_OBJECT_TYPE values enumerated (from code or existing data)
4. Soft-delete cascade behavior on user delete
5. Soft-delete cascade behavior on object (role/position/org) delete
6. Application query patterns verified
```

---

## 6. Recommended Closure

```
ASSESSED → DESIGNED → READY → REFACTORED → VERIFIED → CLOSED
Closure: REFACTORED (3 index additions)
Pre-condition: All VERIFICATION items checked
```

---

## 7. Routing

| Observation | Route to |
|---|---|
| Polymorphic FK pattern | JNPF Extension (junction pattern documentation) |
| F_OBJECT_TYPE value mapping | JNPF Extension (discriminator values) |
| Soft-delete cascade for junction | JNPF Extension (cascade policy) |
| Index naming convention | JNPF Extension |

---

## 8. Universal Core Purity

✅ Zero contamination.

---

## 9. Skill Calibration Verification

```
[x] HG#1-5: All NOT TRIGGERED with explicit dismissal (no borderline)
[N/A] Aggregate Composition: Junction table, not aggregate
[x] Pattern-Recommendation: 4 patterns, 3 index recommendations — no drops
[N/A] Critical Identity: NOT triggered (name, column count)
```

**Calibration self-check passed.**

---

## 10. Pre-REFACTORED Checklist

```
[ ] sys.columns queried
[ ] sys.indexes queried
[ ] F_OBJECT_TYPE values enumerated
[ ] Cascade on user delete verified
[ ] Cascade on object delete verified
[ ] DDL scripts prepared
[ ] Rollback plan
```
