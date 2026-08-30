# R2 Round 1 — Table 02 — ext_product_goods — Expert Result B

> **Date**: 2026-08-30
> **Reviewer type**: Independent AI Expert (Result B)
> **Reviewer session ID**: IAE-R1-2026-08-30
> **Source evidence accessed**: `p8-a/r2/round-1/evidence/SOURCE-EVIDENCE.md`
> **Isolation guarantee**: No reference to `p8-a/r2/round-1/skill/*` (Result A)

---

## 1. Table Overview

- **Name**: ext_product_goods
- **Module**: system-extension (JNPF.Extend)
- **Entity**: ProductGoodsEntity : CLDEntityBase + [Tenant(ClaimConst.TENANTID)] attribute
- **Row count**: 10 rows (very low — likely test/sample data)
- **Tenant**: YES (via [Tenant] attribute, not via base class)
- **SoftDelete**: YES (F_DeleteMark via CLDEntityBase)
- **FKs**: 0 (P8-0 §4 — 业务表几乎无 FK)
- **Special**: **F_Money and F_Amount stored as `string`** (not decimal). This is a notable schema design choice.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | 17 columns (8 declared + 9 inherited, plus F_TenantId from [Tenant] attribute). **Schema anomaly**: F_Money and F_Amount are typed as `string`, not `decimal`. This is unusual — likely for display formatting or precision concerns. NVARCHAR storage of monetary values is a domain design pattern but raises query/index concerns. | ProductgoodsEntity.cs L46-60 | [KNOWN] |
| **B Integrity** | F_ClassifyId is logical association to ext_product_classify. F_EnCode is the business key (product code) — should be UNIQUE per tenant, but no DB constraint. 10 rows suggests test data; production constraints would matter at scale. | ProductgoodsEntity.cs L17, L22-24 | [KNOWN] |
| **C Index** | **Expert independent assessment** of hot paths: 1) Classify browse (e.g., "list all products under category X"); 2) Product search by EnCode (business key lookup); 3) Product name search (F_FullName with LIKE). Low volume (10 rows) means index overhead may not pay off yet, but production scaling is the design intent. | JNPF.Extend module pattern; volume vs design gap | [INFERRED] |
| **D Lifecycle** | Standard CRUD lifecycle. F_DeleteMark handles soft delete. No state machine. F_Qty tracks inventory but isn't a state field per se. | Standard pattern | [KNOWN] |
| **E CRUD/Query** | At 10 rows, all queries are O(1) regardless of indexes. However, schema is designed for production scale. Recommend forward-looking indexes. | Volume analysis | [INFERRED] |
| **F DDD** | Aggregate root: ProductGoods. F_ClassifyId is external association. F_Qty is invariant (must be ≥ 0) but no DB CHECK constraint. **F_Money/F_Amount as string is anti-DDD**: Money should be a Value Object with type safety. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | Single module consumer: JNPF.Extend. Foundry Target: standard tenant-scoped indexes needed for category browse. | JNPF.Extend service topology | [INFERRED] |

### Expert Reasoning Notes

The F_Money/F_Amount as string is the most notable design issue. There are two interpretations:
1. **Display optimization**: avoid decimal → string conversion in UI layer
2. **Precision concern**: decimal may not handle the precision required

Either interpretation suggests a deliberate design choice, not an oversight. **However**, this choice has consequences:
- Arithmetic must be done in app code (CAST → decimal → calculate → CAST back)
- Cannot use SUM/AVG aggregates at DB level efficiently
- Cannot create CHECK constraints on numeric ranges

For R2 risk assessment, this is a **finding** but not a Hard Gate.

---

## 3. Risk Classification

- **Risk**: R2
- **Confidence**: HIGH
- **Rationale**:
  - Volume (10 rows) suggests R0/R1
  - But schema designed for production scale → forward-looking R2
  - F_EnCode UNIQUE constraint is a missing integrity feature (R2 concern)
  - F_Money/F_Amount string typing is a design finding (R2 concern)
  - Standard evidence-driven approach with audit trail

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason | Evidence |
|----|-----------|--------|----------|
| HG#1 Tenant Isolation | NO | F_TenantId present via [Tenant] attribute. | ProductgoodsEntity.cs L11 |
| HG#2 Data Integrity | **borderline** | F_EnCode should be UNIQUE per tenant (business key convention) but no DB constraint. F_Money/F_Amount as string is design concern. Neither is HG#2 trigger condition (orphan risk, missing FK where app expects FK). **Verdict: borderline → NOT triggered** | ProductgoodsEntity.cs L22-24 |
| HG#3 Migration | NO | Only ADD INDEX proposed. F_Money/F_Amount type change would be DESTRUCTIVE (Master Spec §8), out of scope. | Recommended action scope |
| HG#4 Cross-Module | NO | Single module (extend). F_ClassifyId is internal association to ext_product_classify. | P8-0 §4; JNPF.Extend scope |
| HG#5 Business Ambiguity | NO | Standard CRUD. F_Qty is invariant but not a state field. F_DeleteMark is clear soft delete. | Standard pattern |

### Expert Note on HG#2

The "missing UNIQUE constraint" is a real concern but Master Spec §10.3 specifies HG#2 trigger as:
- No FK where app expects FK (orphan risk)
- Missing UNIQUE that causes data corruption risk

F_EnCode uniqueness is a **business rule** that could be enforced at app layer. At 10 rows, no corruption risk exists. At production scale, the constraint would be recommended. **Verdict: NOT triggered at R2; recommend constraint as forward-looking work.**

---

## 5. Recommended Action

- **Action**: EVIDENCE-DRIVEN AUTO (R2)
- **Closure**: REFACTOR — apply 3 indexes, flag F_Money/Amount design concern

### Recommended Indexes

```sql
-- Index 1: Classify browse (most common in UI: "show products in this category")
CREATE NONCLUSTERED INDEX IDX_EXT_PRODGOODS_CLASSIFY
ON ext_product_goods (f_tenant_id, f_classify_id)
INCLUDE (f_id, f_en_code, f_full_name, f_type, f_qty, f_money);

-- Index 2: Product by EnCode (business key lookup)
CREATE NONCLUSTERED INDEX IDX_EXT_PRODGOODS_ENCODE
ON ext_product_goods (f_tenant_id, f_en_code)
INCLUDE (f_id, f_full_name, f_classify_id);

-- Index 3: Soft-delete filter (general alive-records query)
CREATE NONCLUSTERED INDEX IDX_EXT_PRODGOODS_ALIVE
ON ext_product_goods (f_tenant_id, f_delete_mark)
INCLUDE (f_id, f_classify_id, f_en_code, f_full_name);
```

### Deferred Items (NOT in current scope)

1. **F_Money/F_Amount type change**: Master Spec §8 — DESTRUCTIVE. Requires data migration. Out of scope for R2.
2. **F_EnCode UNIQUE constraint**: requires data audit (existing duplicates?). Out of scope until production scale.
3. **F_Qty CHECK constraint (>= 0)**: DDL change with data validation. Low priority.

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §4.2
  - `D:\JNPF-v52\backend\modularity\extend\JNPF.Extend.Entitys\Entity\ProductgoodsEntity.cs`
  - `D:\JNPF-v52\backend\modularity\common\JNPF.Common\Contracts\CLDEntityBase.cs`
- **Evidence tags used**: [KNOWN] 5, [INFERRED] 2
- **Stop condition met**: YES

---

## 7. Additional Reasoning (Expert Commentary)

### Volume vs Design Gap

The 10-row count strongly suggests this is **test/seed data**. The schema is production-grade. This is a common pattern in JNPF: full schema with sample data for demo. The implication:
- Indexes are forward-looking
- At current volume, no performance benefit
- But adding indexes NOW is cheaper than later (DDL is cheap, refactoring under load is expensive)

### Comparison with ext_product (38 cols)

ext_product (P8-A Shadow §4.5 noted) has 38 columns; ext_product_goods has 17 columns. The "goods" table appears to be a **line-item** or **SKU** table under the master "product" table. The naming pattern suggests:
- ext_product = master (catalog)
- ext_product_classify = category
- ext_product_goods = SKU/variant
- ext_product_entry = inventory entry

This is a typical e-commerce schema pattern. The F_ClassifyId foreign key to ext_product_classify confirms this.

### F_Money vs F_Amount

Both stored as string. F_Money is "单价" (unit price), F_Amount is "金额" (amount). The naming suggests F_Amount is calculated (qty × money) — but stored as string for display precision. Acceptable pattern in some Chinese accounting systems.

---

**Expert Result B complete for Table 02 — ext_product_goods**
