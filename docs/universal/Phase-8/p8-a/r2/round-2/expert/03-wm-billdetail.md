# R2 Round 2 — Table 03 — WM_BillDetail — Expert Result B

> **Date**: 2026-08-30
> **Reviewer type**: Independent AI Expert (Result B)
> **Reviewer session ID**: IAE-R2-2026-08-30
> **Source evidence accessed**: `p8-a/r2/round-2/evidence/SOURCE-EVIDENCE.md`
> **Isolation guarantee**: No reference to `p8-a/r2/round-2/skill/*` (Result A)

---

## 1. Table Overview

- **Name**: WM_BillDetail (UPPERCASE legacy prefix)
- **Module**: system-warehouse-legacy
- **Entity**: **NONE** — dynamic SQL only
- **Row count**: 1629 (highest in Round 2)
- **Tenant**: NO (legacy pattern — 0/39 per P8-0 §5.1)
- **SoftDelete**: NO
- **FKs**: 0 (legacy isolation)
- **Special**: Legacy naming (WM_*, no F_ column prefix). High volume. No entity.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | NO entity file. Legacy naming convention (WM_* prefix, no F_ column prefix). Inferred: BillId (no F_), BillNo, MaterialId, Qty, UnitPrice, Amount, Remark. ~10-15 columns. | P8-0 §2; WM_* pattern | [GUESS] for specifics, [KNOWN] for legacy pattern |
| **B Integrity** | **No DB FKs**. App-managed relationships. Legacy warehouse module pre-dates JNPF framework conventions. | P8-0 §5.1 | [KNOWN] |
| **C Index** | No entity → no application-layer evidence. 1629 rows = real production volume. Inferred hot paths: by BillId (PK), by BillNo, by MaterialId. | Volume + legacy convention | [INFERRED] |
| **D Lifecycle** | Standard CRUD. No state machine. Legacy pattern. | Legacy pattern | [INFERRED] |
| **E CRUD/Query** | High-volume CRUD. 1629 rows = real production traffic. INSERT on bill entry (frequent), SELECT on bill listing (frequent). | Volume | [INFERRED] |
| **F DDD** | Aggregate: BillDetail. Part of legacy warehouse domain. External aggregates: Bill, Material (assumed). | DDD analysis | [INFERRED] |
| **G Consumer/Target** | Warehouse module only (legacy isolation). No cross-module consumer. Foundry Target: standard high-volume CRUD indexes. | Legacy isolation | [INFERRED] |

### Expert Reasoning Notes

**Legacy modules are a different beast**:
- Pre-date JNPF framework
- No F_ prefix on column names
- No entity classes (raw SQL only)
- No tenant (legacy)
- No soft delete (legacy)
- 39 such tables in registry

**Round 2 test for legacy pattern**: Does Skill correctly:
1. Recognize legacy naming convention
2. Not assume F_ prefix on columns
3. Not assume tenant column exists
4. Not assume soft delete exists
5. Recognize legacy = high maintenance risk

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: HIGH
- **Rationale**:
  - No entity (Skill cannot make strong recommendations)
  - Legacy naming (not JNPF convention)
  - High volume (1629 rows)
  - No FKs (orphan risk unmanaged)
  - Legacy module may have undocumented business rules

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason | Evidence |
|----|-----------|--------|----------|
| HG#1 Tenant Isolation | NO | Legacy pattern. No tenant by design. | P8-0 §5.1 |
| HG#2 Data Integrity | **borderline** | No DB FKs. App-managed. 1629 rows of real data with no DB-level orphan protection. | P8-0 §5.1 |
| HG#3 Migration | NO | No migration proposed. | N/A |
| HG#4 Cross-Module | NO | Legacy isolation. Single warehouse module. | Legacy pattern |
| HG#5 Business Ambiguity | **borderline** | Legacy module may have undocumented business rules. WM_BillDetail could have hidden state semantics. | Legacy pattern |

### Expert Note on HG#2 vs HG#5

Both are borderline, but for different reasons:
- **HG#2 borderline**: No FKs = no DB-level integrity enforcement, but app-managed = no immediate issue
- **HG#5 borderline**: Legacy = possible undocumented business rules

Neither triggers outright, but both warrant attention.

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — pending evidence
- **Closure**: DEFERRED

### Why Defer

1. No entity = undefined
2. Legacy pattern = needs special handling
3. HG#2 + HG#5 both borderline = needs architectural review

### Required Evidence

1. SQL query for actual column names (verify legacy pattern)
2. SQL query for existing indexes
3. Architectural review: is WM_BillDetail still actively used? Migration plan?

### Conditional Recommendation

If BillId and MaterialId are not indexed:
```sql
CREATE NONCLUSTERED INDEX IDX_WM_BILLDETAIL_BILLID ON WM_BillDetail (BillId);
CREATE NONCLUSTERED INDEX IDX_WM_BILLDETAIL_MATERIAL ON WM_BillDetail (MaterialId);
```

These are conservative — should not break anything, may improve performance.

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §3.3
  - P8-0 §2 (registry — NO entity)
  - P8-0 §5.1 (legacy pattern)
- **Evidence tags used**: [KNOWN] 2, [INFERRED] 4, [GUESS] 1
- **Stop condition met**: PARTIAL

---

## 7. Additional Reasoning (Expert Commentary)

### Legacy Module Lifecycle Question

WM_BillDetail raises a strategic question:
- Is it still actively used?
- Should it be migrated to JNPF conventions (F_ prefix, tenant column, soft delete)?
- Or should it be deprecated?

**This is a strategic decision**, not a tactical one. Even with SQL evidence, the question of "what to do with legacy modules" requires business input.

### Index Recommendations for Legacy Tables

Legacy tables often lack indexes that modern tables have. The conservative approach:
1. Identify PK (usually present)
2. Identify common JOIN columns (BillId, MaterialId here)
3. Add indexes for those columns
4. Don't change schema unless explicitly requested

This matches my recommendation above.

### Round 2 Test: Legacy Pattern Recognition

This is a critical Round 2 test. The legacy pattern is fundamentally different from modern JNPF tables:
- No F_ prefix
- No tenant
- No soft delete
- No entity
- Different naming convention (UPPERCASE prefix)

Both Skill and Expert should recognize this and apply **legacy-appropriate** recommendations (not modern JNPF conventions).

---

**Expert Result B complete for Table 03 — WM_BillDetail**
