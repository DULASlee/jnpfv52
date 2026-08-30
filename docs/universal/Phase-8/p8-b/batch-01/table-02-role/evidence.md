# P8-B Batch 01 — Table 02: base_role Assessment

> **Phase**: 8 — P8-B.1 Table Assessment
> **Table**: base_role
> **Status**: ASSESSED
> **Date**: 2026-08-30
> **Skill Calibration Applied**: 4 CRITICAL items
> **Mode**: Shadow assessment — 0 DB writes yet

---

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `BASE_ROLE` | `[KNOWN]` from registry |
| Category | system-core (permission) | `[KNOWN]` |
| Entity expected | `RoleEntity` in `JNPF.Systems.Entitys/Entity/Permission/` | `[INFERRED]` |
| Likely column count | 15-25 (simple identity aggregate) | `[INFERRED]` |
| Tenant column | F_TENANT_ID | `[INFERRED]` |
| Soft delete | F_DELETE_MARK | `[INFERRED]` |
| M:N to user | via junction table (probably base_user_role OR base_user_relation) | `[INFERRED]` |

**Verification needed before REFACTORED**: actual schema and junction table identity.

---

## 2. Seven-Dimension Assessment

### Dimension A: Schema

Expected fields:
- F_ID, F_FULL_NAME, F_EN_CODE, F_DESCRIPTION
- F_CATEGORY (role type: system/business/custom)
- F_ENABLED_MARK
- F_SORT_CODE
- F_TENANT_ID, F_DELETE_MARK, CLDS fields

**Tag**: `[INFERRED]` for all. Verification required.

**Aggregate Composition Analysis** (Calibration Item 2):
- Column count likely < 40 — analysis NOT required
- Role is a clear aggregate (single concept)

**Finding**: No-Finding pending verification.

---

### Dimension B: Integrity

| Concern | Status |
|---|---|
| M:N to user | Junction table exists (likely `base_user_role` or `base_user_relation`) — VERIFY |
| Self-reference | No (roles don't form hierarchy typically) |
| Cross-module | Roles referenced by workflow, visualdev for permissions |

**Finding**: Pending junction table verification. App-level FK management per JNPF convention.

---

### Dimension C: Index (Calibration Item 3 — every pattern needs index)

| # | Pattern | Tag | Index Recommendation |
|---|---|---|---|
| 1 | List by F_TENANT_ID | `[INFERRED]` | PK index sufficient |
| 2 | Get by F_EN_CODE (runtime lookup) | `[INFERRED]` | `IDX_ROLE_ENCODE (F_TENANT_ID, F_EN_CODE)` |
| 3 | List by F_CATEGORY | `[INFERRED]` | `IDX_ROLE_CATEGORY (F_TENANT_ID, F_CATEGORY)` |
| 4 | List enabled roles | `[INFERRED]` | Filter on F_ENABLED_MARK; no separate index needed unless hot |

**Calibration Item 3 satisfied**: 4 patterns, 2 index recommendations, 2 reuse. No silent drops.

**Index priority**:
1. `IDX_ROLE_ENCODE` — HIGH (runtime lookup)
2. `IDX_ROLE_CATEGORY` — MEDIUM

**Finding**: SAFE-REFACTOR with 2 index additions.

---

### Dimension D: Lifecycle

**Expected**: Standard CLDS + F_ENABLED_MARK. No custom state machine.

**Finding**: No-Finding. Standard pattern.

---

### Dimension E: CRUD/Query

Query patterns:
- Get by id (PK) — covered
- Get by en_code (runtime) — needs index
- List by category — needs index
- List enabled roles for user assignment — covered by F_ENABLED_MARK + category

**N+1 risk**: Low. Single-row lookups.

**Finding**: No-Finding pending indexes.

---

### Dimension F: DDD

**Aggregate Composition Analysis** (Calibration Item 2): Not required (< 40 cols expected).

**Critical Table check** (Calibration Item 4):
- Column count: < 50 — NOT triggered
- Name: `base_role` matches the calibration Item 4 explicit list `*_role`
- Domain: identity/auth/permission (YES)

**Critical Table Risk Floor APPLIED**: Role is in the explicit critical list.

Per Calibration Item 4:
- Risk Floor: R2 minimum (NOT R0/R1)
- Aggregate Composition: NOT required (< 40 cols)
- Junction table detection: MANDATORY
- Cross-module impact: MANDATORY (roles are cross-module referenced)

**Junction table detection** (mandatory per Item 4):
- Expected junction: `base_user_role` OR `base_user_relation`
- VERIFY which junction is used

**Cross-module impact** (mandatory per Item 4):
- Roles referenced by: workflow (approval authority), visualdev (form permissions), inteAssistant (AI role)
- Adding/removing/changing roles affects all these modules

**Finding**: Critical Identity Table — aggregate boundary clear, but junction and cross-module analysis required.

---

### Dimension G: Consumer / Target Readiness

Standard JNPF mapping:
- F_TENANT_ID → TenantId
- F_DELETE_MARK → IsDeleted
- F_ENABLED_MARK → IsEnabled
- F_EN_CODE → RoleCode (JNPF-specific naming)

**Finding**: No-Finding. Standard mapping.

---

## 3. Risk Classification

**Critical Table Risk Floor Applied** (Calibration Item 4): R2 minimum.

**Factors**:
- Critical Identity Table by name match
- M:N to user (junction table exists, unknown which)
- Cross-module referenced (workflow, visualdev, inteAssistant)
- Role deletion has cascading permission impact

**Risk Level: R2** — Confidence: MEDIUM (50-80%)

**Rationale**: Critical table by name; cross-module impact elevates risk; junction table verification needed.

---

## 4. Hard Gate (Calibration Item 1: No Borderline)

| HG | Status | Reasoning |
|---|---|---|
| HG#1 (tenant isolation) | **NOT TRIGGERED** | F_TENANT_ID present; standard JNPF ITenantFilter. Dismissal: standard pattern. |
| HG#2 (data integrity) | **NOT TRIGGERED** | App-level FK management. Junction table assumed to exist. Dismissal: standard pattern. VERIFICATION: junction table exists and is correctly used. |
| HG#3 (migration) | **NOT TRIGGERED** | Only ADD INDEX. |
| HG#4 (cross-module) | **NOT TRIGGERED** with EXPLICIT DISMISSAL | Multiple modules reference role (workflow, visualdev, inteAssistant). However, no FK at DB level; app manages consistency. Dismissal: cross-module impact is read-only/permission-assignment only, not schema dependency. If schema change to base_role, dependent modules only need re-evaluation, not refactor. |
| HG#5 (business ambiguity) | **NOT TRIGGERED** | Role semantics are clear (group of permissions). F_CATEGORY distinguishes role types. Dismissal: standard pattern. |

**All 5 HGs: NOT TRIGGERED with explicit reasoning.**

**Note on HG#4**: The cross-module impact is real but does NOT cross the HG#4 trigger threshold (which is "schema dependency requiring coordinated refactor"). Schema changes here are independent.

---

## 5. Recommended Action

```
SAFE-REFACTOR:
1. Add IDX_ROLE_ENCODE (F_TENANT_ID, F_EN_CODE) — HIGH for runtime lookup
2. Add IDX_ROLE_CATEGORY (F_TENANT_ID, F_CATEGORY) — MEDIUM for filter

VERIFICATION (before READY):
1. sys.columns queried — actual schema confirmed
2. sys.indexes queried — current index state
3. Junction table IDENTIFIED: base_user_role OR base_user_relation OR other
4. Cross-module usage documented: workflow, visualdev, inteAssistant reference points
5. Code-level check: ITenantFilter wiring
6. Soft-delete cascade: role deletion should not orphan user-role assignments
```

---

## 6. Recommended Closure

```
ASSESSED → DESIGNED → READY → REFACTORED → VERIFIED → CLOSED
Closure: REFACTORED (index additions)
Pre-condition: All VERIFICATION items checked
```

---

## 7. Routing

| Observation | Route to |
|---|---|
| Junction table identity verification | JNPF Extension (permission model documentation) |
| Cross-module role usage | JNPF Extension (cross-module contract) |
| Standard role pattern | JNPF Extension (code pattern library) |

---

## 8. Universal Core Purity

✅ Zero contamination. Standard JNPF pattern.

---

## 9. Skill Calibration Verification

```
[x] HG#1-5: All NOT TRIGGERED with explicit dismissal (no borderline)
[N/A] Aggregate Composition: Column count < 40, not required (but Critical check applied)
[x] Pattern-Recommendation: 4 patterns, 2 index recommendations, 2 reuse — no drops
[x] Critical Identity: APPLIED — name `base_role` matches, Risk Floor R2 enforced, junction + cross-module required
```

**Calibration self-check passed.**

---

## 10. Pre-REFACTORED Checklist

```
[ ] sys.columns queried — actual schema confirmed
[ ] sys.indexes queried — current index state confirmed
[ ] Junction table identified (base_user_role / base_user_relation / other)
[ ] Cross-module usage enumerated
[ ] Code-level check: cycle prevention (N/A — not hierarchical)
[ ] Code-level check: ITenantFilter wiring
[ ] Code-level check: role deletion cascade behavior
[ ] DDL scripts prepared and reviewed
[ ] Rollback plan: DROP INDEX scripts
```
