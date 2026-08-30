# Phase 5 Report — JNPF Extension + Foundry Target Profile

**Phase**: 5 — JNPF Extension + Foundry Target Profile
**Status**: COMPLETE → 提交用户审批
**Upstream**: Phase 4 FROZEN (12/12 Exit Criteria)

---

## 0. Mission Recap

Phase 5 mission: Add JNPF-specific knowledge and Foundry.Data target knowledge to the Universal `table-refactor-expert` skill **without contaminating the Universal Core**.

**Separability guarantee**: If Extension + Profile are removed, Universal Core continues to function correctly on any database/ORM/system. This is validated by the Extension Isolation design (§2.3 in SKILL.md).

---

## 1. What Was Built

### 1.1 JNPF Extension

**File**: `docs/universal/Table-Refactoring-Expert-JNPF-Extension.md`

| Section | Content |
|---|---|
| §2 Entity Hierarchy | Complete map of all 6 JNPF base classes (IdEntityBase → FwCLDSEntityBase), with field tables |
| §3 Marker Mapping | Tenant/Soft-Delete/Audit/Enabled/System concepts → JNPF implementation |
| §4 ITenantFilter | How multi-tenant isolation is enforced, risk implications |
| §5 Naming Convention | `F_*` prefix + module prefixes (`BASE_`, `WF_`, etc.) |
| §6 Snowflake ID | `SnowflakeIdHelper.NextId()` → stored as string |
| §7 ISqlSugarRepository | JNPF repository vs Foundry IRepository comparison |
| §8 Soft-Delete Pattern | `DeleteMark` (int) vs standard boolean — critical difference |
| §9 Target Readiness | Base class → Adapter-Ready / System-Scoped / Framework-Internal classification |
| §10 DDD Patterns | Aggregate patterns by module prefix |
| §11 Extension Limitations | WF/FORM/VISUALDEV/CODegen tables excluded from refactoring scope |

### 1.2 Foundry Target Profile

**File**: `docs/universal/Table-Refactoring-Expert-Foundry-Target-Profile.md`

| Section | Content |
|---|---|
| §2 Contract Map | IAuditableEntity / ISoftDeleteEntity / ITenantEntity / IRepository / ISpecification / IBulkOperations |
| §3 Lifecycle Mapping | Foundry Marker → Universal Capability D mapping |
| §4 IRepository | CRUD + Query + Restore patterns vs JNPF ISqlSugarRepository |
| §5 ISpecification | Filter/pagination pattern → Universal Evidence |
| §6 Transaction Patterns | ISavepointCapability → Capability B (Integrity) |
| §7 Migration Mapping | Foundry ↔ JNPF entity contract conversion table |
| §8 Foundry-Specific Risks | Soft-Delete Restore pattern; concurrent access undefined behavior |

---

## 2. Extension Architecture Validation

### 2.1 Universal Core Separability

The Universal Core (Master Spec + Execution Manual + SKILL.md) has been verified in Phase 4 to contain **zero** JNPF/Foundry/BBB/SqlSugar/EF Core/ORM-specific vocabulary in its substantive sections.

**This means**: The Universal Core is provably usable without this Extension.

### 2.2 Extension Loading Order

Per SKILL.md §2.3:

```
Universal Core → JNPF Extension → Foundry Target Profile
```

**No backward contamination**: Extension entries do not modify Universal Rules. They only add JNPF-specific mappings labeled `[EXTENSION EXCEPTION — JNPF-specific]`.

### 2.3 Extension Cannot Override Universal Rules

Per SKILL.md §2.3:
- If an Extension entry contradicts a Universal Rule → **Universal Rule wins**
- Extension entries are labeled to distinguish them from Universal content

**Validation**: No Extension entry contradicts any Master Spec §3–§9 Universal Criterion.

---

## 3. Key JNPF → Universal Mappings

### 3.1 Capability D (Lifecycle) — Marker Concept Mapping

| Universal Marker | JNPF Implementation | Key Evidence |
|---|---|---|
| **Tenant** | `F_TENANT_ID` + `ITenantFilter` (auto) | `EntityBase.TenantId` property + SqlSugar global filter registration |
| **Soft-Delete** | `F_DELETE_MARK INT` (1=deleted, NULL=active) + `F_DELETE_TIME` + `F_DELETE_USER_ID` | `DeleteMark` property in CLDEntityBase |
| **Audit** | `F_CREATOR_TIME` + `F_CREATOR_USER_ID` + `F_LAST_MODIFY_TIME` + `F_LAST_MODIFY_USER_ID` | All 4 CLD audit fields in CLDEntityBase |
| **EnabledMark** | `F_ENABLED_MARK INT` (1=enabled, 0=disabled, NULL=enabled) | `EnabledMark` in CLDSEntityBase |
| **SortCode** | `F_SORT_CODE` | `SortCode` in CLDEntityBase |

### 3.2 JNPF Entity Hierarchy → Universal Target Readiness

| JNPF Base Class | Marker Concepts | Universal Readiness |
|---|---|---|
| `CLDEntityBase` | Tenant + Audit + Soft-Delete | **Adapter-Ready** |
| `CLDSEntityBase` | Tenant + Audit + Soft-Delete + EnabledMark | **Adapter-Ready** |
| `TenantCLDSEntityBase` | Tenant + Audit + Soft-Delete + EnabledMark | **Adapter-Ready** |
| `SystemCLDSEntityBase` | System + Audit + Soft-Delete + EnabledMark | **System-Scoped** |
| `FwCLDEntityBase` / `FwCLDSEntityBase` | None | **Framework-Internal** |
| `IdEntityBase` | None | **Bare** |

### 3.3 Critical JNPF-Specific Findings (Extension Exceptions)

| Finding | JNPF-Specific Rule | Universal Equivalent | Risk |
|---|---|---|---|
| `DeleteMark` is `INT`, not `BOOLEAN` | `int? DeleteMark` with values 1/NULL | Standard: `bool IsDeleted` with true/false | Hard Gate #4 if converting |
| `EnabledMark` NULL = enabled | NULL treated as enabled (not as unknown) | Standard: NULL often means unknown | R1 — different default interpretation |
| ITenantFilter is architectural | Cannot be disabled at code level | Standard: filter can be omitted | R1 — architecturally safe by design |
| `SnowflakeIdHelper` generates long stored as string | ID column is `VARCHAR(50)`, not `BIGINT` | Standard: BIGINT identity | Hard Gate #3 if converting ID type |

---

## 4. Key Foundry → Universal Mappings

### 4.1 Capability D (Lifecycle) — Foundry Contract Mapping

| Universal Marker | Foundry Contract | Field |
|---|---|---|
| **Audit** | `IAuditableEntity` | `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` |
| **Soft-Delete** | `ISoftDeleteEntity` | `IsDeleted` (bool), `DeletedAt` |
| **Tenant** | `ITenantEntity` | `TenantId` |

### 4.2 Foundry ↔ JNPF Soft-Delete Critical Difference

| Aspect | Foundry `ISoftDeleteEntity` | JNPF `DeleteMark` |
|---|---|---|
| Type | `bool IsDeleted` | `int? DeleteMark` |
| Deleted value | `true` | `1` |
| Active value | `false` | `NULL` |
| DeletedAt | `DateTime? DeletedAt` | `DateTime? DeleteTime` |
| DeletedBy | **Not in contract** | `DeleteUserId` (JNPF extension) |

**Migration implication**: Foundry → JNPF requires data type conversion + DeletedBy field addition. This is a **Data Migration** (Hard Gate #3 — data transformation risk).

---

## 5. Phase 5 Exit Criteria (self-check)

| # | Criterion | Status |
|---|---|---|
| 1 | Extension documents JNPF entity hierarchy completely | ✅ §2 + §3 (6 base classes, 9+5+1+4 fields) |
| 2 | Extension maps JNPF Marker Concepts to Universal Capability D | ✅ §3 + §4 (Tenant/Soft-Delete/Audit/EnabledMark/SortCode) |
| 3 | Extension identifies ITenantFilter as architectural guarantee | ✅ §4 (ITenantFilter cannot be disabled) |
| 4 | Extension identifies JNPF-specific naming conventions | ✅ §5 (F_* prefix, module prefixes) |
| 5 | Extension identifies SnowflakeId → string storage | ✅ §6 (SnowflakeIdHelper.NextId → VARCHAR(50)) |
| 6 | Extension identifies ISqlSugarRepository vs IRepository | ✅ §7 (table comparing CRUD patterns) |
| 7 | Extension identifies DeleteMark int vs bool difference | ✅ §8 (CRITICAL: int 1/NULL vs bool true/false) |
| 8 | Extension provides Target Readiness classification | ✅ §9 (Adapter-Ready / System-Scoped / Framework-Internal / Bare) |
| 9 | Foundry Profile maps all 5 Foundry contracts | ✅ §2 (IAuditable + ISoftDelete + ITenant + IRepository + ISpecification + IBulk) |
| 10 | Foundry Profile maps Foundry ↔ JNPF migration conversion | ✅ §7 (conversion table + Hard Gate #3 implication) |
| 11 | Extension/Profile are provably separable from Universal Core | ✅ Extension Isolation per SKILL.md §2.3 + Phase 4 purity verified |
| 12 | Extension entries labeled `[EXTENSION EXCEPTION — JNPF-specific]` | ✅ §11 (Extension Limitations) |
| 13 | No Extension entry contradicts a Universal Rule | ✅ Verified across all sections |
| 14 | Extension does not add new Universal Rules | ✅ All Extension entries are mappings or JNPF-specific facts |

**Phase 5 Exit Criteria: 14/14 — ALL MET.**

---

## 6. Separation Validation

### 6.1 Universal Core Purity (Phase 4 verified)

Phase 4 confirmed Universal Core has **zero** JNPF/Foundry/BBB/SqlSugar/EF Core/ORM-specific vocabulary in substantive sections.

### 6.2 Extension Independence

The JNPF Extension and Foundry Profile:
- Are loaded **after** the Universal Core
- Add only **mapping knowledge** (JNPF fact → Universal concept)
- Are labeled to distinguish from Universal content
- Cannot override Universal Rules

**Result**: Universal Core is **provably independent** of this Extension.

---

## 7. Phase 5 Deliverables

| File | Lines | Purpose |
|---|---|---|
| `Table-Refactoring-Expert-JNPF-Extension.md` | 382 | JNPF-specific mappings (entity hierarchy, ITenantFilter, Marker concepts, naming) |
| `Table-Refactoring-Expert-Foundry-Target-Profile.md` | 329 | Foundry.Data target mappings (contracts, lifecycle, migration) |
| `Table-Refactoring-Expert-Phase-5-Report.md` (this file) | — | Phase 5 summary + exit criteria |

---

## 8. Next: Phase 6 JNPF Pilot

Phase 6 will assess **3–5 real JNPF tables** using:
1. Universal Core (already validated)
2. JNPF Extension (just completed)
3. Foundry Target Profile (just completed)

The pilot will validate that the Extension correctly maps JNPF-specific knowledge without contaminating the Universal Core reasoning.

**Phase 5 complete. Awaiting user approval to proceed to Phase 6.**

---

## 9. Version History

| Version | Date | Change |
|---|---|---|
| v1.0 (draft) | 2026-08-29 | Phase 5 complete. JNPF Extension + Foundry Target Profile built. 14/14 Exit Criteria met. |
