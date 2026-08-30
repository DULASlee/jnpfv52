# R2 Round 2 — Table 03 — WM_BillDetail — Skill Result A

> **Date**: 2026-08-30
> **Reviewer type**: table-refactor-expert Skill (Result A)
> **Source evidence**: `p8-a/r2/round-2/evidence/SOURCE-EVIDENCE.md`

---

## 1. Table Overview

- **Name**: WM_BillDetail (UPPERCASE prefix — legacy naming)
- **Module**: system-warehouse-legacy
- **Entity**: **NONE — Dynamic SQL only**
- **Row count**: 1629 rows (highest in Round 2)
- **Tenant**: NO (legacy pattern — P8-0 §5.1 confirmed 0/39 tenant)
- **SoftDelete**: NO (legacy pattern)
- **FKs in/out**: 0 (no FKs in legacy warehouse pattern)
- **Special**: **Legacy naming convention** (WM_*, no F_ prefix). High volume. No entity. No tenant.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | NO entity. Legacy naming pattern (WM_* prefix, no F_ column prefix). Inferred: BillId (no F_), BillNo, MaterialId, Qty, UnitPrice, Amount, Remark. **Column names are legacy style** — not JNPF F_ convention. | P8-0 §2; WM_* naming pattern | [GUESS] for specifics, [KNOWN] for legacy pattern |
| **B Integrity** | **No DB FKs**. App-managed relationships. Legacy warehouse module pre-dates JNPF framework. | P8-0 §5.1 (legacy has 0 FKs) | [KNOWN] |
| **C Index** | No entity → no application-layer evidence. Inferred hot paths: by BillId (PK), by BillNo, by MaterialId. 1629 rows = real production volume. | Volume + convention | [INFERRED] |
| **D Lifecycle** | Standard CRUD. No state machine. Legacy pattern. | Legacy pattern | [INFERRED] |
| **E CRUD/Query** | High-volume CRUD: INSERT on bill entry, SELECT on bill listing. 1629 rows = real production traffic. | Volume | [INFERRED] |
| **F DDD** | Aggregate: BillDetail. Part of legacy warehouse domain. External aggregates: Bill, Material (likely). | DDD analysis | [INFERRED] |
| **G Consumer/Target** | Warehouse module only (single module). No cross-module consumer (legacy isolation). Foundry Target: standard indexes for high-volume CRUD. | Legacy isolation | [INFERRED] |

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: HIGH
- **Rationale**:
  - No entity (Skill cannot make strong recommendations)
  - Legacy naming (not JNPF convention)
  - High volume (1629 rows)
  - No FKs (orphan risk unmanaged at DB level)
  - Legacy module = may have undocumented business rules

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason |
|----|-----------|--------|
| HG#1 Tenant Isolation | NO | Legacy pattern. No tenant by design. |
| HG#2 Data Integrity | **borderline** | No DB FKs. App-managed. 1629 rows = real data, but no DB-level orphan protection. |
| HG#3 Migration | NO | Only ADD INDEX proposed. |
| HG#4 Cross-Module | NO | Legacy isolation. Single module. |
| HG#5 Business Ambiguity | **borderline** | Legacy module = possibly undocumented business rules. WM_BillDetail may have hidden state semantics. |

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — pending evidence
- **Closure**: DEFERRED

### Why Defer

1. **No entity** = undefined situation (Skill §2.2)
2. **Legacy pattern** = needs special handling
3. **HG#2 borderline** = needs architectural review
4. **HG#5 borderline** = legacy business rules may be undocumented

### Required Evidence

1. SQL query for existing indexes
2. SQL query for actual column names (verify legacy pattern)
3. Architectural review: is WM_BillDetail still actively used? Should it be migrated to F_ prefix?

### Potential Indexes (deferred)

```sql
-- Index 1: Bill header lookup (likely needed)
CREATE NONCLUSTERED INDEX IDX_WM_BILLDETAIL_BILLID ON WM_BillDetail (BillId);

-- Index 2: Material lookup (likely needed)
CREATE NONCLUSTERED INDEX IDX_WM_BILLDETAIL_MATERIAL ON WM_BillDetail (MaterialId);
```

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §3.3
  - P8-0 §2 (registry — NO entity)
  - P8-0 §5.1 (legacy pattern — no tenant, no soft delete)
  - No entity file found
- **Evidence tags used**: [KNOWN] 2, [INFERRED] 4, [GUESS] 1
- **Stop condition met**: PARTIAL

---

## 7. State Machine Status

```
Current State: DEFERRED (HUMAN APPROVAL required)
```

---

**Skill Result A complete for Table 03 — WM_BillDetail**
