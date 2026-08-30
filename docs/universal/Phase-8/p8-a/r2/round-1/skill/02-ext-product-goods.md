# R2 Round 1 — Table 02 — ext_product_goods — Skill Result A

> **Date**: 2026-08-30
> **Reviewer type**: table-refactor-expert Skill (Result A)
> **Source evidence**: `p8-a/r2/round-1/evidence/SOURCE-EVIDENCE.md`

---

## 1. Table Overview

- **Name**: ext_product_goods (EXT_PRODUCT_GOODS)
- **Module**: system-extension (JNPF.Extend)
- **Entity**: ProductGoodsEntity : CLDEntityBase (with [Tenant(ClaimConst.TENANTID)] attribute)
- **Row count**: 10 rows (very low volume)
- **Tenant**: YES (F_TenantId via [Tenant] attribute on entity, NOT base class)
- **SoftDelete**: YES (F_DeleteMark via CLDEntityBase)
- **FKs in/out**: 0 (per P8-0 §4 — 业务表几乎无 FK)
- **Special**: F_ClassifyId is logical reference to ext_product_classify (NOT a DB FK). F_Money and F_Amount stored as STRING (not decimal) — domain design issue.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | 17 columns (8 declared + 9 inherited + F_TenantId from [Tenant] attribute). **F_Money and F_Amount stored as `string` type** — domain design issue (should be decimal for arithmetic). NOT a Hard Gate but worth flagging. | ProductgoodsEntity.cs L46-60 | [KNOWN] |
| **B Integrity** | F_ClassifyId is logical reference to ext_product_classify. No DB FK. App-managed. F_EnCode should be UNIQUE within tenant (business key), no constraint found. 10 rows is test data, not active. | ProductgoodsEntity.cs L17, L22-24 | [KNOWN] |
| **C Index** | No existing indexes per P8-0. Critical hot paths: (a) classify browse: f_tenant_id + f_classify_id; (b) product lookup by code: f_tenant_id + f_en_code; (c) product search by name: f_tenant_id + f_full_name. | JNPF.Extend module pattern | [INFERRED] |
| **D Lifecycle** | Standard CRUD. No state machine. F_DeleteMark handles soft delete. No lifecycle complexity. | Standard pattern | [KNOWN] |
| **E CRUD/Query** | Low frequency (10 rows, test data). But schema designed for production scale. Indexes matter when scaling. | Volume vs design gap | [INFERRED] |
| **F DDD** | Aggregate: ProductGoods. F_ClassifyId is association, not part of aggregate. Money/Amount as string is DDD Value Object concern (should be Money type). Standard aggregate. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | Consumers: extend module (UI), possibly workflow/integrate. Target Foundry Profile: needs f_tenant_id+f_classify_id index for category browse. | JNPF.Extend service | [INFERRED] |

---

## 3. Risk Classification

- **Risk**: R2
- **Confidence**: HIGH
- **Rationale**: Low volume (10 rows) suggests R0/R1, but schema is production-grade with classification relationship. F_Money/F_Amount as string is design issue. F_EnCode should be UNIQUE. Standard R2 evidence-driven with audit.

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason |
|----|-----------|--------|
| HG#1 Tenant Isolation | NO | F_TenantId present via [Tenant] attribute. Multi-tenant isolation OK. |
| HG#2 Data Integrity | **borderline** | F_EnCode should be UNIQUE per tenant (business key) but no DB constraint. F_Money/F_Amount as string is domain concern but not DB integrity violation. |
| HG#3 Migration | NO | Only ADD INDEX proposed. No schema change. |
| HG#4 Cross-Module | NO | Single module (extend). F_ClassifyId is internal association. |
| HG#5 Business Ambiguity | NO | Standard CRUD. No multi-state fields. |

---

## 5. Recommended Action

- **Action**: EVIDENCE-DRIVEN (R2) — needs audit trail
- **Closure**: REFACTOR — apply 3 indexes + flag F_Money/Amount type issue as future work

### Recommended Indexes

```sql
-- Index 1: Classify browse (most common)
CREATE NONCLUSTERED INDEX IDX_PRODUCTGOODS_CLASSIFY
ON ext_product_goods (f_tenant_id, f_classify_id)
INCLUDE (f_id, f_en_code, f_full_name, f_qty, f_money);

-- Index 2: Product lookup by code (business key)
CREATE NONCLUSTERED INDEX IDX_PRODUCTGOODS_ENCODE
ON ext_product_goods (f_tenant_id, f_en_code)
INCLUDE (f_id, f_full_name, f_classify_id);

-- Index 3: Soft-delete filter
CREATE NONCLUSTERED INDEX IDX_PRODUCTGOODS_TENANT_ALIVE
ON ext_product_goods (f_tenant_id, f_delete_mark)
INCLUDE (f_id, f_classify_id, f_en_code);
```

### Deferred Items

- F_Money/F_Amount type change (string → decimal): Master Spec §8 says DESTRUCTIVE; requires data migration. NOT in current scope.
- F_EnCode UNIQUE constraint: would require data audit. NOT in current scope.

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §4.2
  - `D:\JNPF-v52\backend\modularity\extend\JNPF.Extend.Entitys\Entity\ProductgoodsEntity.cs`
  - `D:\JNPF-v52\backend\modularity\common\JNPF.Common\Contracts\CLDEntityBase.cs`
- **Evidence tags used**: [KNOWN] 4, [INFERRED] 3
- **Stop condition met**: YES

---

## 7. State Machine Status

```
DISCOVERED  → ✅
ASSESSED    → ✅
DESIGNED    → ✅
READY       → ✅ (3 indexes + 2 deferred items)
REFACTORED  → ⏸ deferred
VERIFIED    → ⏸ pending
CLOSED      → ⏸ pending
```

**Current State**: DESIGNED → READY (3 indexes proposed)

---

**Skill Result A complete for Table 02 — ext_product_goods**
