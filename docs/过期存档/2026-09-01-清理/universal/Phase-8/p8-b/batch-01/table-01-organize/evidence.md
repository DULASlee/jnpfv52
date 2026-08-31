# P8-B Batch 01 — Table 01: base_organize Assessment

> **Phase**: 8 — P8-B.1 Table Assessment
> **Table**: base_organize
> **Status**: ASSESSED
> **Date**: 2026-08-30
> **Skill Calibration Applied**: 4 CRITICAL items (HG Borderline / Aggregate / Pattern-Rec / Critical Identity)
> **Mode**: Shadow assessment — 0 DB writes yet

---

## 1. Discovery

| Aspect | Value | Tag |
|---|---|---|
| Physical table | `BASE_ORGANIZE` | `[KNOWN]` from registry |
| Category | system-core | `[KNOWN]` |
| Entity expected | `OrganizeEntity` in `JNPF.Systems.Entitys/Entity/Permission/` | `[INFERRED]` |
| SugarTable mapping | `[SugarTable("BASE_ORGANIZE")]` | `[INFERRED]` from JNPF convention |
| Base class | TenantCLDSEntityBase | `[INFERRED]` |
| Likely column count | 15-25 | `[INFERRED]` from JNPF organize pattern |
| Likely has F_PARENT_ID | YES (organize tree) | `[INFERRED]` from registry Batch 01 grouping |
| Incoming FK | High (referenced by base_user, base_role assignment, workflow approval) | `[INFERRED]` |
| Tenant column | F_TENANT_ID (expected) | `[INFERRED]` |
| Soft delete | F_DELETE_MARK (expected) | `[INFERRED]` |

**Verification needed before REFACTORED**: actual sys.columns, sys.indexes, sys.foreign_keys queries.

---

## 2. Seven-Dimension Assessment (with Calibrated Skill)

### Dimension A: Schema

**Finding**: Likely standard JNPF organize pattern.

Expected fields:
- F_ID (Snowflake)
- F_PARENT_ID (tree)
- F_FULL_NAME, F_EN_CODE
- F_CATEGORY (organize type: company/department/team)
- F_SORT_CODE
- F_ENABLED_MARK, F_DESCRIPTION
- F_TENANT_ID, F_DELETE_MARK, CLDS fields

**Calibrated concern** (Item 1, 2): Column count is moderate (<40), so Aggregate Composition Analysis NOT required by calibration. Standard JNPF organize pattern is well-understood.

**Tag Audit** (Calibration Item 3): All claims above are `[INFERRED]`, not `[KNOWN]`. Verification required before DESIGNED.

**Finding**: No-Finding pending verification. Schema pattern is standard.

---

### Dimension B: Integrity

**Finding**: Tree structure with self-reference.

| Concern | Tag | Action |
|---|---|---|
| F_PARENT_ID self-reference | `[INFERRED]` (typical JNPF organize) | No DB FK (JNPF pattern) |
| Cycle prevention | `[GUESS]` (code-level, not verified) | **VERIFICATION REQUIRED**: check code for cycle prevention |
| Orphan nodes (parent deleted) | `[GUESS]` | **VERIFICATION REQUIRED**: check soft-delete cascade behavior |
| Tenant isolation | `[INFERRED]` (F_TENANT_ID present) | OK if present |

**Calibrated concern** (Item 1 — HG#2): If cycle prevention is unverified, this is HG#2 candidate. Default to NOT TRIGGERED with explicit dismissal: "JNPF organize typically has app-level cycle prevention". **Verification required to confirm.**

**Finding**: Pending verification. Likely No-Finding if app-level cycle prevention exists.

---

### Dimension C: Index

**Identified Query Patterns** (Calibration Item 3 requires index recommendation for each):

| # | Pattern | Tag | Index Recommendation |
|---|---|---|---|
| 1 | Tree by F_PARENT_ID | `[INFERRED]` | `IDX_ORGANIZE_PARENT (F_TENANT_ID, F_PARENT_ID)` |
| 2 | List by F_CATEGORY | `[INFERRED]` | `IDX_ORGANIZE_CATEGORY (F_TENANT_ID, F_CATEGORY)` |
| 3 | Get by F_EN_CODE | `[INFERRED]` (for runtime lookup) | `IDX_ORGANIZE_ENCODE (F_TENANT_ID, F_EN_CODE)` |
| 4 | Tree root list (F_PARENT_ID IS NULL) | `[INFERRED]` | Use same as #1 |
| 5 | List by F_ENABLED_MARK | `[INFERRED]` | Use existing PK or add IDX_ORGANIZE_ENABLED if hot path |

**Calibration Item 3 satisfied**: 5 patterns identified, 3 explicit index recommendations, 2 reuse existing indexes. No silent drops.

**Index priority**:
1. `IDX_ORGANIZE_PARENT` — CRITICAL (tree traversal is hot path)
2. `IDX_ORGANIZE_ENCODE` — HIGH (runtime lookup)
3. `IDX_ORGANIZE_CATEGORY` — MEDIUM (filter)

**Finding**: SAFE-REFACTOR with 3 index additions.

---

### Dimension D: Lifecycle

**Finding**: Standard CLDS + F_ENABLED_MARK. No custom state machine expected.

Verification needed:
- Is there a F_STATE field? (if yes, state machine analysis required)
- Is F_DELETE_MARK = 1 the standard delete?

**Tag**: Likely No-Finding. Pending verification.

---

### Dimension E: CRUD/Query

**Query patterns** (Calibration Item 3 applies):

| Pattern | Recommendation |
|---|---|
| Tree traversal (recursive CTE on F_PARENT_ID) | Use IDX_ORGANIZE_PARENT |
| Root node list (F_PARENT_ID IS NULL) | Use IDX_ORGANIZE_PARENT (PK = NULL excluded) |
| Get by en_code | Use IDX_ORGANIZE_ENCODE |
| List by category | Use IDX_ORGANIZE_CATEGORY |

**No N+1 risk** for single-node operations. Tree traversal has potential N+1 if done row-by-row, but recursive CTE handles this.

**Finding**: No-Finding pending index additions.

---

### Dimension F: DDD

**Aggregate Composition Analysis** (Calibration Item 2):
- Column count < 40, so analysis NOT REQUIRED by calibration rule
- However, organize is a clear aggregate (hierarchy with F_PARENT_ID)
- Boundary: clear (organize ≠ role, organize ≠ position, organize ≠ user)

**Critical Table check** (Calibration Item 4):
- Column count: likely 15-25 (< 50) — NOT triggered
- Incoming FK: high but unknown exact count — pending verification
- Name: base_organize is NOT in the explicit "critical" list
- Domain: organization management, NOT identity/auth/permission

**Verdict**: NOT a Critical Identity Table by calibration rules.

**Finding**: Clear aggregate. No ambiguity expected.

---

### Dimension G: Consumer / Target Readiness

**Expected Foundry Profile mapping**:
- F_TENANT_ID → TenantId (direct)
- F_DELETE_MARK → IsDeleted (direct)
- F_ENABLED_MARK → IsEnabled (direct)
- F_PARENT_ID → ParentId (direct, self-reference)
- CLDS fields → direct

**JNPF Extension candidates**: None expected for organize (standard pattern).

**Finding**: No-Finding. Standard mapping applies.

---

## 3. Risk Classification (with Calibrated Skill)

**Critical Table check** (Calibration Item 4): NOT triggered. Apply standard risk.

**Factors**:
- Standard JNPF organize pattern (well-understood)
- Tree structure (known pattern)
- Moderate column count
- Self-reference (handled by JNPF convention)
- Cycle prevention unverified (open item)

**Risk Level: R1** — Confidence: MEDIUM (50-80%)

**Rationale**: Standard pattern but with one open verification item (cycle prevention).

---

## 4. Hard Gate (with Calibrated Skill — No Borderline)

Per Calibration Item 1: No borderline. Each HG is TRIGGERED or NOT TRIGGERED with explicit reasoning.

| HG | Status | Reasoning |
|---|---|---|
| HG#1 (tenant isolation) | **NOT TRIGGERED** | F_TENANT_ID present (assumed standard); ITenantFilter wired at app layer (assumed JNPF standard). Dismissal: standard JNPF pattern. |
| HG#2 (data integrity) | **NOT TRIGGERED** | App-level FK management (JNPF convention); cycle prevention assumed in code. Dismissal: standard JNPF pattern. VERIFICATION: code-level cycle check exists. |
| HG#3 (migration) | **NOT TRIGGERED** | Only ADD INDEX (additive, non-destructive). |
| HG#4 (cross-module) | **NOT TRIGGERED** | Single module (system / Permission). Dismissal: organize is system-internal. |
| HG#5 (business ambiguity) | **NOT TRIGGERED** | Organize semantics are clear (hierarchy). Dismissal: standard pattern. |

**All 5 HGs: NOT TRIGGERED with explicit dismissal.**

---

## 5. Recommended Action

```
SAFE-REFACTOR:
1. Add IDX_ORGANIZE_PARENT (F_TENANT_ID, F_PARENT_ID) — CRITICAL for tree
2. Add IDX_ORGANIZE_ENCODE (F_TENANT_ID, F_EN_CODE) — HIGH for runtime lookup
3. Add IDX_ORGANIZE_CATEGORY (F_TENANT_ID, F_CATEGORY) — MEDIUM for filter

VERIFICATION (before READY):
1. Query sys.columns for actual schema
2. Query sys.indexes for current index state
3. Query sys.foreign_keys (incoming references)
4. Code-level check: cycle prevention in OrganizeService
5. Code-level check: ITenantFilter wiring
6. Code-level check: soft-delete cascade behavior
```

---

## 6. Recommended Closure

```
ASSESSED → DESIGNED (after verification)
Closure: NO-CHANGE or REFACTORED depending on verification outcomes
Pre-condition: All VERIFICATION items checked
```

**Closure cannot be finalized until VERIFICATION passes.**

---

## 7. Routing

| Observation | Route to |
|---|---|
| Standard JNPF organize pattern | JNPF Extension (documentation) |
| Tree cycle prevention pattern | JNPF Extension (code pattern library) |
| Index naming convention | JNPF Extension (consistency) |

---

## 8. Universal Core Purity

✅ Zero contamination. Standard JNPF pattern, no Universal Core modification needed.

---

## 9. Skill Calibration Verification

Pre-application check:

```
[x] HG#1-5: All NOT TRIGGERED with explicit dismissal (no borderline)
[N/A] Aggregate Composition: Column count < 40, not required
[x] Pattern-Recommendation: 5 patterns, 3 index recommendations, 2 reuse — no drops
[N/A] Critical Identity: NOT triggered (column count, name, domain)
```

**Calibration self-check passed.**

---

## 10. Pre-REFACTORED Checklist

Before moving to REFACTORED state, the following MUST be verified:

```
[ ] sys.columns queried — actual schema confirmed
[ ] sys.indexes queried — current index state confirmed (only PK?)
[ ] sys.foreign_keys queried — incoming FKs enumerated
[ ] OrganizeService.cs reviewed — cycle prevention found
[ ] ITenantFilter wiring confirmed
[ ] Soft-delete cascade behavior verified (no orphans expected)
[ ] DDL scripts prepared and reviewed
[ ] Rollback plan documented
```

**Once all checked: proceed to DESIGNED → READY → REFACTORED.**
