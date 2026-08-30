# R2 Round 1 — Table 05 — flow_template_json — Skill Result A

> **Date**: 2026-08-30
> **Reviewer type**: table-refactor-expert Skill (Result A)
> **Source evidence**: `p8-a/r2/round-1/evidence/SOURCE-EVIDENCE.md`

---

## 1. Table Overview

- **Name**: flow_template_json (FLOW_TEMPLATE_JSON)
- **Module**: workflow-engine (JNPF.WorkFlow)
- **Entity**: FlowTemplateJsonEntity : CLDSEntityBase (with [Tenant] attribute)
- **Row count**: 3 rows (very low — near-empty)
- **Tenant**: YES (F_TenantId via [Tenant] attribute)
- **SoftDelete**: YES (F_DeleteMark via base class)
- **FKs in/out**: 0 (per P8-0 §4)
- **Special**: **JSON-heavy** — F_FlowTemplateJson contains the actual workflow definition (likely large JSON, NTEXT or NVARCHAR(MAX)). F_TemplateId is logical reference to flow_template.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | 17 columns (7 declared + 10 inherited + F_TenantId from [Tenant] attribute). F_FlowTemplateJson is the workflow definition (LARGE TEXT/JSON). Schema designed for version control. | FlowTemplateJsonEntity.cs L33-37 | [KNOWN] |
| **B Integrity** | F_TemplateId is logical reference to flow_template. F_Version + F_TemplateId suggest versioned workflow. No DB FK. App-managed. F_GroupId is logical group association. | FlowTemplateJsonEntity.cs L17, L29, L47-49 | [KNOWN] |
| **C Index** | No existing indexes per P8-0. Critical hot paths: (a) version lookup: f_tenant_id + f_template_id + f_version; (b) enabled workflows: f_tenant_id + f_enabled_mark; (c) group browse: f_tenant_id + f_group_id. **F_FlowTemplateJson itself should NOT be indexed (large JSON).** | Workflow engine pattern | [INFERRED] |
| **D Lifecycle** | Workflow template versioning: F_Version + F_EnabledMark. State: draft → published → archived. Multiple versions of same template_id. F_EnabledMark (0/1) for active version. | FlowTemplateJsonEntity.cs L20-30 | [KNOWN] |
| **E CRUD/Query** | Low frequency (3 rows). User creates template, queries by template_id+version. Hot path: load workflow by template_id (latest version). Indexes needed when scaling. | Volume + pattern | [INFERRED] |
| **F DDD** | Aggregate: WorkflowTemplate (versioned). F_FlowTemplateJson is serialized aggregate (could be Event Sourcing pattern). Version is value object invariant. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | Single consumer: workflow engine. Foundry Target: needs f_tenant_id+f_template_id+f_enabled_mark for "load latest enabled version" query. | JNPF.WorkFlow | [INFERRED] |

---

## 3. Risk Classification

- **Risk**: R2
- **Confidence**: HIGH
- **Rationale**:
  - 3 rows = no current stress, but production-grade schema
  - JSON-heavy workflow definition = careful indexing (don't index JSON itself)
  - Version control pattern requires specific composite index
  - Single module consumer (workflow)
  - Standard R2 with audit trail

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason |
|----|-----------|--------|
| HG#1 Tenant Isolation | NO | F_TenantId via [Tenant] attribute. |
| HG#2 Data Integrity | NO | No DB FKs but versioned design manages integrity. |
| HG#3 Migration | NO | Only ADD INDEX proposed. |
| HG#4 Cross-Module | NO | Single module (workflow). F_TemplateId is internal association. |
| HG#5 Business Ambiguity | NO | F_Version + F_EnabledMark pattern is clear versioning. |

---

## 5. Recommended Action

- **Action**: EVIDENCE-DRIVEN (R2) — needs audit trail
- **Closure**: REFACTOR — apply 3 indexes

### Recommended Indexes

```sql
-- Index 1: Latest enabled version lookup (hot path: "load active workflow")
CREATE NONCLUSTERED INDEX IDX_FLOWTJSON_TEMPLATE_ACTIVE
ON flow_template_json (f_tenant_id, f_template_id, f_enabled_mark, f_version DESC)
INCLUDE (f_id, f_full_name, f_flow_template_json);

-- Index 2: Tenant alive filter (general CRUD)
CREATE NONCLUSTERED INDEX IDX_FLOWTJSON_TENANT_ALIVE
ON flow_template_json (f_tenant_id, f_delete_mark)
INCLUDE (f_id, f_template_id, f_version, f_full_name);

-- Index 3: Group browse (UI navigation)
CREATE NONCLUSTERED INDEX IDX_FLOWTJSON_GROUP
ON flow_template_json (f_tenant_id, f_group_id)
INCLUDE (f_id, f_full_name, f_template_id, f_version);
```

### Important Notes

- **DO NOT index F_FlowTemplateJson itself** (large JSON would bloat index)
- The composite (f_tenant_id, f_template_id, f_enabled_mark, f_version DESC) supports "get latest active version" pattern
- INCLUDE clause contains F_FlowTemplateJson for covering index (avoids key lookup)

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §4.5
  - `D:\JNPF-v52\backend\modularity\workflow\JNPF.WorkFlow.Entitys\Entity\FlowTemplateJsonEntity.cs`
  - `D:\JNPF-v52\backend\modularity\common\JNPF.Common\Contracts\CLDSEntityBase.cs`
- **Evidence tags used**: [KNOWN] 4, [INFERRED] 3
- **Stop condition met**: YES

---

## 7. State Machine Status

```
DISCOVERED  → ✅
ASSESSED    → ✅
DESIGNED    → ✅ (3 indexes aligned with workflow version pattern)
READY       → ✅
REFACTORED  → ⏸ deferred
VERIFIED    → ⏸ pending
CLOSED      → ⏸ pending
```

**Current State**: DESIGNED → READY (3 indexes proposed)

---

**Skill Result A complete for Table 05 — flow_template_json**
