# R2 Round 1 — Source Evidence Package

> **Phase**: 8 — P8-A.6 R2-COMP Round 1
> **Date**: 2026-08-30
> **Authority**: Both Skill and Independent AI Expert read from this package
> **Constraint**: Both MUST NOT see each other's output (skill/ or expert/ directories)

---

## Shared Source Evidence

This is the **only** evidence both Skill and Independent AI Expert may read before producing their Result.

Both paths:
- MUST read this package
- MUST NOT read each other's output (`p8-a/r2/round-1/skill/` or `p8-a/r2/round-1/expert/`)
- MUST produce their result in their own directory

---

## 1. Table Unit Inventory (P8-0)

From `p8-0/table-unit-registry-final.md`:

- 289 total tables
- 187 with F_TENANT_ID
- 150 with F_DELETE_MARK (soft delete)
- 14 FK edges (all in inteAssistant SA / KG modules)
- 164 with explicit Entity mapping, 125 without (dynamic)

---

## 2. Round 1 Table Summary

| # | Table | Module | Entity | Rows | Tenant | SoftDelete | FKs |
|---|-------|--------|--------|------|--------|------------|-----|
| 01 | base_message | system-core | YES | 1229 | YES (f_tenant_id) | YES | 0 |
| 02 | ext_product_goods | system-extension | YES | 10 | YES (f_tenant_id) | YES | 0 |
| 03 | base_advanced_query_scheme | system-core | YES | 2 | YES (f_tenant_id) | YES | 0 |
| 04 | base_file | system-core | **NO (dynamic)** | 0 | YES (f_tenant_id) | YES | 0 |
| 05 | flow_template_json | workflow-engine | YES | 3 | YES (f_tenant_id) | YES | 0 |

---

## 3. Entity Inheritance Pattern

All entity classes extend one of these base classes:

```csharp
// From D:\JNPF-v52\backend\modularity\common\JNPF.Common\Contracts\

CLDEntityBase           → F_Id, F_CreatorTime, F_CreatorUserId, F_LastModifyTime,
                           F_LastModifyUserId, F_DeleteMark, F_DeleteTime,
                           F_DeleteUserId, F_SortCode (NO Tenant, NO EnabledMark)

CLDSEntityBase : CLDEntityBase → adds F_EnabledMark (0=disabled, 1=enabled)

TenantCLDSEntityBase : TenantEntityBase<string> → F_Id, F_TenantId + same CLD fields + F_EnabledMark
```

### 3.1 Per-Table Entity Base Class

| Table | Entity Class | Base Class | Fields Inherited |
|-------|--------------|------------|------------------|
| base_message | MessageEntity | TenantCLDSEntityBase | F_Id, F_TenantId, F_CreatorTime, F_CreatorUserId, F_LastModifyTime, F_LastModifyUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId, F_SortCode, F_EnabledMark |
| ext_product_goods | ProductGoodsEntity | CLDEntityBase | F_Id, F_CreatorTime, F_CreatorUserId, F_LastModifyTime, F_LastModifyUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId, F_SortCode |
| base_advanced_query_scheme | AdvancedQuerySchemeEntity | CLDEntityBase | F_Id, F_CreatorTime, F_CreatorUserId, F_LastModifyTime, F_LastModifyUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId, F_SortCode |
| flow_template_json | FlowTemplateJsonEntity | CLDSEntityBase | F_Id, F_CreatorTime, F_CreatorUserId, F_LastModifyTime, F_LastModifyUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId, F_SortCode, F_EnabledMark |

> **Note**: ext_product_goods and base_advanced_query_scheme extend `CLDEntityBase` (NOT TenantCLDSEntityBase), but P8-0 registry shows f_tenant_id IS present in their tables. This is the [Tenant(ClaimConst.TENANTID)] attribute or runtime filter that adds tenant column. Reviewer should flag this as divergence or note it.

---

## 4. Per-Table Entity Source

### 4.1 base_message

**File**: `D:\JNPF-v52\backend\modularity\message\JNPF.Message.Entitys\Entity\MessageEntity.cs`
**SugarTable**: `BASE_MESSAGE`
**Class**: `MessageEntity : TenantCLDSEntityBase`

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| Type | F_TYPE | int? | 类别：1-通知公告，2-系统消息、3-私信消息 |
| Title | F_TITLE | string | 标题 |
| FlowType | F_FLOW_TYPE | int? | 流程跳转类型 1:审批 2:委托 |
| UserId | F_USER_ID | string | 用户主键 |
| IsRead | F_IS_READ | int? | 是否阅读 |
| ReadTime | F_READ_TIME | DateTime? | 阅读时间 |
| ReadCount | F_READ_COUNT | int? | 阅读次数 |
| BodyText | F_BODY_TEXT | string | 正文 |

**Inherited (from TenantCLDSEntityBase)**: F_Id, F_TenantId, F_CreatorTime, F_CreatorUserId, F_LastModifyTime, F_LastModifyUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId, F_SortCode, F_EnabledMark

**Total columns**: ~19 (8 declared + 11 inherited)

### 4.2 ext_product_goods

**File**: `D:\JNPF-v52\backend\modularity\extend\JNPF.Extend.Entitys\Entity\ProductgoodsEntity.cs`
**SugarTable**: `EXT_PRODUCT_GOODS`
**Class**: `ProductGoodsEntity : CLDEntityBase`
**Note**: `[Tenant(ClaimConst.TENANTID)]` attribute is present → runtime tenant filter

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| ClassifyId | F_CLASSIFY_ID | string | 分类主键 (relationship to ext_product_classify) |
| EnCode | F_EN_CODE | string | 产品编号 (business key) |
| FullName | F_FULL_NAME | string | 产品名称 |
| Type | F_TYPE | string | 订货类型 |
| ProductSpecification | F_PRODUCTSPECIFICATION | string | 产品规格 |
| Money | F_MONEY | string | 单价 (note: stored as string, NOT decimal) |
| Qty | F_QTY | int | 库存数 |
| Amount | F_AMOUNT | string | 金额 (note: stored as string) |

**Inherited (from CLDEntityBase)**: F_Id, F_CreatorTime, F_CreatorUserId, F_LastModifyTime, F_LastModifyUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId, F_SortCode

**Total columns**: ~17 (8 declared + 9 inherited; F_TenantId added via [Tenant] attribute)

### 4.3 base_advanced_query_scheme

**File**: `D:\JNPF-v52\backend\modularity\system\JNPF.Systems.Entitys\Entity\System\AdvancedQuerySchemeEntity.cs`
**SugarTable**: `BASE_ADVANCED_QUERY_SCHEME`
**Class**: `AdvancedQuerySchemeEntity : CLDEntityBase`
**Note**: No `[Tenant]` attribute visible — but P8-0 shows F_TenantId IS present (verify)

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| FullName | F_FULL_NAME | string | 方案名称 |
| MatchLogic | F_MATCH_LOGIC | string | 匹配逻辑 |
| ConditionJson | F_CONDITION_JSON | string | 条件规则Json (JSON data) |
| ModuleId | F_MODULE_ID | string | 菜单主键 (relationship to base_module) |

**Inherited (from CLDEntityBase)**: F_Id, F_CreatorTime, F_CreatorUserId, F_LastModifyTime, F_LastModifyUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId, F_SortCode

**Total columns**: ~13 (4 declared + 9 inherited)

### 4.4 base_file

**File**: NONE — no entity file found
**SugarTable**: `BASE_FILE` (assumed from naming convention)
**Access Pattern**: Dynamic SQL only — no C# entity

**Inferred columns** (based on naming convention and JNPF pattern):
- F_Id (PK)
- F_TenantId
- F_FileName, F_FilePath, F_FileType, F_FileSize, F_FileExtension
- F_Thumbnail (or similar)
- F_CreatorTime, F_CreatorUserId
- F_DeleteMark, F_DeleteTime, F_DeleteUserId

**Total columns**: ~13 (inferred)

### 4.5 flow_template_json

**File**: `D:\JNPF-v52\backend\modularity\workflow\JNPF.WorkFlow.Entitys\Entity\FlowTemplateJsonEntity.cs`
**SugarTable**: `FLOW_TEMPLATE_JSON`
**Class**: `FlowTemplateJsonEntity : CLDSEntityBase`
**Note**: `[Tenant(ClaimConst.TENANTID)]` attribute is present

| Property | Column | Type | Notes |
|----------|--------|------|-------|
| TemplateId | F_TEMPLATE_ID | string? | 流程编码 (relationship to flow_template) |
| VisibleType | F_VISIBLE_TYPE | int? | 可见类型 |
| Version | F_VERSION | string? | 流程版本 |
| FlowTemplateJson | F_FLOW_TEMPLATE_JSON | string? | 流程模板 (LARGE JSON field) |
| FullName | F_FULL_NAME | string? | 流程名称 |
| GroupId | F_GROUP_ID | string? | 分组id |
| SendConfigIds | F_SEND_CONFIG_IDS | string? | 消息配置id |

**Inherited (from CLDSEntityBase)**: F_Id, F_CreatorTime, F_CreatorUserId, F_LastModifyTime, F_LastModifyUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId, F_SortCode, F_EnabledMark

**Total columns**: ~17 (7 declared + 10 inherited; F_TenantId added via [Tenant] attribute)

---

## 5. Existing Indexes (from DB metadata — pre-Round 1 state)

For all 5 tables, assume the indexes that already exist from P8-B execution or pre-existing DB state:

- base_message: no P8-B execution; assume 0 indexes (table needs assessment)
- ext_product_goods: no P8-B execution; assume 0 indexes
- base_advanced_query_scheme: no P8-B execution; assume 0 indexes
- base_file: no P8-B execution; assume 0 indexes
- flow_template_json: no P8-B execution; assume 0 indexes

> **Note**: Both Skill and Expert should verify via SQL if indexes exist. Without SQL execution, the assessment is based on logical patterns only.

---

## 6. Frozen Context (REUSED)

Both paths may reference:

- **Universal Skill v1.0** — Hard Gates, 5 HGs, Master Spec §10.3
- **JNPF Extension v1.0** — Project-specific field mapping (F_TenantId, F_DeleteMark, etc.)
- **Foundry Target Profile v1.0** — Target contract requirements (Audit, Index conventions)

---

## 7. Hard Gates Reference (Master Spec §10.3)

| HG | Trigger Condition |
|----|-------------------|
| HG#1 | Tenant isolation missing or violated (no F_TenantId on multi-tenant table) |
| HG#2 | Data integrity at risk (no FK where app expects FK, orphan risk, missing UNIQUE) |
| HG#3 | Migration risk high (data type change, nullable → non-nullable, large data backfill) |
| HG#4 | Cross-module dependency detected (table referenced by 3+ modules via application logic, no DB FK indexes) |
| HG#5 | Business ambiguity (state machine unclear, multiple boolean fields, undocumented semantics) |

---

## 8. Risk Levels (Master Spec §10)

| Risk | Definition |
|------|------------|
| R0 | Auto-Close: no change needed; trivial table |
| R1 | Auto-Apply: simple index, low-risk DDL, no audit trail needed |
| R2 | Evidence-Driven: needs evidence-backed decision, audit trail recorded |
| R3+ | Human Approval required: complex change, cross-table impact, ambiguous business |

---

## 9. Output Requirements

For each Table Unit, BOTH Skill and Independent AI Expert must produce:

1. **Table Identity**: Name, module, entity, row count, base class
2. **A–G Assessment**: 7 dimensions (Schema, Integrity, Index, Lifecycle, CRUD/Query, DDD, Consumer/Target)
3. **Finding / Explicit No-Finding**: What was found, with evidence tag ([KNOWN]/[COMPUTED]/[INFERRED]/[GUESS])
4. **Evidence**: Source files / SQL queries used
5. **Risk**: R0/R1/R2/R3+ with rationale
6. **Hard Gate**: All 5 HGs assessed (YES/NO/borderline)
7. **Recommended Action**: One of 6 gates (AUTO-CLOSE / AUTO-APPLY / EVIDENCE-DRIVEN / HUMAN APPROVAL / CROSS-TABLE / DESTRUCTIVE)
8. **Recommended Closure**: One of 4 (NO-CHANGE / REFACTOR / DEFERRED / ACCEPT-AS-IS)

---

## 10. Evidence Sufficiency Stop Rule

Per Master Spec §11.3:
- Need ≥1 KNOWN/COMPUTED evidence per Finding
- INFERRED/GUESS evidence requires explicit acknowledgment
- Stop searching once sufficiency met (no over-searching)

---

**Package compiled**: 2026-08-30
**Used by**: Both Skill (Result A) and Independent AI Expert (Result B)
**Isolation guarantee**: Neither path sees the other's output
