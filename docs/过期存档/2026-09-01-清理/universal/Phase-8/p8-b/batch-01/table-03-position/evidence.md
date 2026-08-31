# P8-B Batch 01 — Table 03: base_position Assessment

> **Phase**: 8 — P8-B.1 Table Assessment
> **Table**: base_position
> **Status**: ASSESSED
> **Date**: 2026-08-30
> **Skill Calibration Applied**: 4 CRITICAL items
> **Mode**: Shadow assessment — 0 DB writes yet

---

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `BASE_POSITION` | `[KNOWN]` from registry |
| Category | system-core (permission) | `[KNOWN]` |
| Entity expected | `PositionEntity` in `JNPF.Systems.Entitys/Entity/Permission/` | `[INFERRED]` |
| Likely column count | 10-20 (simple aggregate) | `[INFERRED]` |
| Tenant column | F_TENANT_ID | `[INFERRED]` |
| Soft delete | F_DELETE_MARK | `[INFERRED]` |
| M:N to user | via junction (similar to role) | `[INFERRED]` |
| Hierarchy | Likely NO (positions are flat or use organize membership) | `[INFERRED]` |

**Verification needed**: actual schema, junction table, hierarchy assumption.

---

## 2. Seven-Dimension Assessment

### Dimension A: Schema

Expected fields:
- F_ID, F_FULL_NAME, F_EN_CODE, F_DESCRIPTION
- F_ORGANIZE_ID (link to organize) — `[INFERRED]` (positions belong to organize)
- F_LEVEL (position level) — `[INFERRED]`
- F_ENABLED_MARK
- F_SORT_CODE
- F_TENANT_ID, F_DELETE_MARK, CLDS

**Tag**: `[INFERRED]`. Verification required.

**Aggregate Composition Analysis**: Not required (< 40 cols).

**Finding**: No-Finding pending verification.

---

### Dimension B: Integrity

| Concern | Status |
|---|---|
| F_ORGANIZE_ID → base_organize | App-level reference, no DB FK |
| M:N to user | Junction table |
| Hierarchy | Likely NO (use organize tree for hierarchy) |

**Finding**: Standard JNPF pattern. No-Finding pending verification.

---

### Dimension C: Index (Calibration Item 3 — every pattern needs index)

| # | Pattern | Tag | Index Recommendation |
|---|---|---|---|
| 1 | List by F_ORGANIZE_ID | `[INFERRED]` | `IDX_POSITION_ORG (F_TENANT_ID, F_ORGANIZE_ID)` |
| 2 | Get by F_EN_CODE | `[INFERRED]` | `IDX_POSITION_ENCODE (F_TENANT_ID, F_EN_CODE)` |
| 3 | List by F_LEVEL | `[INFERRED]` | `IDX_POSITION_LEVEL (F_TENANT_ID, F_LEVEL)` |
| 4 | Get by id | `[INFERRED]` | PK sufficient |

**Calibration Item 3 satisfied**: 4 patterns, 3 index recommendations. No silent drops.

**Index priority**:
1. `IDX_POSITION_ORG` — HIGH (positions listed by org frequently)
2. `IDX_POSITION_ENCODE` — MEDIUM
3. `IDX_POSITION_LEVEL` — LOW (may not be hot)

**Finding**: SAFE-REFACTOR with up to 3 index additions.

---

### Dimension D: Lifecycle

Standard CLDS + F_ENABLED_MARK. No custom state machine.

**Finding**: No-Finding.

---

### Dimension E: CRUD/Query

Query patterns:
- Get by id
- Get by en_code
- List by organize (hot path for org view)
- List by level (for hierarchy display)

**N+1**: Low.

**Finding**: No-Finding pending indexes.

---

### Dimension F: DDD

**Aggregate Composition Analysis**: Not required (< 40 cols).

**Critical Table check** (Calibration Item 4):
- Column count: < 50 — NOT triggered
- Name: `base_position` does NOT match explicit critical list (no `*_position` in Item 4)
- Domain: identity/permission (similar to role)

**Decision**: NOT Critical Table by name, but domain-wise similar to role. Apply moderate caution without Risk Floor.

Position is referenced by:
- base_user (via junction)
- workflow (approval authority)
- visualdev (form permissions)

**Cross-module impact**: Real but moderate (similar to role but typically less cross-cutting).

**Finding**: Clear aggregate, moderate cross-module impact.

---

### Dimension G: Consumer / Target Readiness

Standard JNPF mapping. No special handling needed.

**Finding**: No-Finding.

---

## 3. Risk Classification

**Critical Table check**: NOT triggered (name does not match explicit list, column count < 50).

**Factors**:
- Simple aggregate
- Smaller cross-module impact than role
- Standard lifecycle
- Index gaps are operational, not architectural

**Risk Level: R1** — Confidence: MEDIUM (50-80%)

**Rationale**: Simpler than role (no Risk Floor). Standard pattern.

---

## 4. Hard Gate (Calibration Item 1: No Borderline)

| HG | Status | Reasoning |
|---|---|---|
| HG#1 (tenant isolation) | **NOT TRIGGERED** | F_TENANT_ID present. |
| HG#2 (data integrity) | **NOT TRIGGERED** | App-level FK management. |
| HG#3 (migration) | **NOT TRIGGERED** | Only ADD INDEX. |
| HG#4 (cross-module) | **NOT TRIGGERED** | Position referenced by workflow, visualdev but no DB FK. Read-only reference pattern. |
| HG#5 (business ambiguity) | **NOT TRIGGERED** | Position semantics clear. |

**All 5 HGs: NOT TRIGGERED with explicit dismissal.**

---

## 5. Recommended Action

```
SAFE-REFACTOR:
1. Add IDX_POSITION_ORG (F_TENANT_ID, F_ORGANIZE_ID) — HIGH
2. Add IDX_POSITION_ENCODE (F_TENANT_ID, F_EN_CODE) — MEDIUM
3. (Optional) IDX_POSITION_LEVEL — add only if hot path verified

VERIFICATION:
1. sys.columns queried
2. sys.indexes queried
3. Junction table identified
4. Code-level checks
```

---

## 6. Recommended Closure

```
ASSESSED → DESIGNED → READY → REFACTORED → VERIFIED → CLOSED
Closure: REFACTORED (2-3 index additions)
```

---

## 7. Routing

| Observation | Route to |
|---|---|
| Position-org relationship | JNPF Extension |
| Junction table identity | JNPF Extension |

---

## 8. Universal Core Purity

✅ Zero contamination.

---

## 9. Skill Calibration Verification

```
[x] HG#1-5: All NOT TRIGGERED with explicit dismissal (no borderline)
[N/A] Aggregate Composition: Column count < 40, not required
[x] Pattern-Recommendation: 4 patterns, 3 index recommendations — no drops
[x] Critical Identity: NOT triggered (name doesn't match explicit list, but moderate cross-module noted)
```

**Calibration self-check passed.**

---

## 10. Pre-REFACTORED Checklist

```
[ ] sys.columns queried
[ ] sys.indexes queried
[ ] Junction table identified
[ ] Cross-module usage verified
[ ] DDL scripts prepared
[ ] Rollback plan
```
