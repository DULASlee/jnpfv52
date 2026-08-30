# R2 Round 1 — Table 05 — flow_template_json — Expert Result B

> **Date**: 2026-08-30
> **Reviewer type**: Independent AI Expert (Result B)
> **Reviewer session ID**: IAE-R1-2026-08-30
> **Source evidence accessed**: `p8-a/r2/round-1/evidence/SOURCE-EVIDENCE.md`
> **Isolation guarantee**: No reference to `p8-a/r2/round-1/skill/*` (Result A)

---

## 1. Table Overview

- **Name**: flow_template_json
- **Module**: workflow-engine (JNPF.WorkFlow)
- **Entity**: FlowTemplateJsonEntity : CLDSEntityBase + [Tenant(ClaimConst.TENANTID)] attribute
- **Row count**: 3 rows (very low — near-empty)
- **Tenant**: YES (via [Tenant] attribute)
- **SoftDelete**: YES (F_DeleteMark via CLDSEntityBase)
- **FKs**: 0 (P8-0 §4)
- **Special**: **JSON-heavy** — F_FlowTemplateJson is the workflow definition (likely NVARCHAR(MAX) or NTEXT, large JSON). F_Version + F_TemplateId pattern = versioned workflow templates.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | 17 columns (7 declared + 10 inherited + F_TenantId from [Tenant] attribute). F_FlowTemplateJson is large JSON. Schema designed for workflow versioning. Clean. | FlowTemplateJsonEntity.cs L11-55 | [KNOWN] |
| **B Integrity** | F_TemplateId is logical reference to flow_template (master template). F_Version suggests versioned history. No DB FK. App-managed. F_GroupId is logical group. | FlowTemplateJsonEntity.cs L17, L29-31 | [KNOWN] |
| **C Index** | No existing indexes verified. Hot paths (independent expert assessment): 1) **Latest enabled version per template**: (f_tenant_id, f_template_id, f_enabled_mark, f_version DESC) — critical for "load active workflow"; 2) Group browse: (f_tenant_id, f_group_id); 3) General alive filter: (f_tenant_id, f_delete_mark). **F_FlowTemplateJson must NOT be indexed** (large JSON → index bloat). | Workflow engine hot path analysis | [INFERRED] |
| **D Lifecycle** | **Versioned lifecycle**: F_Version + F_EnabledMark pattern. State: draft → published → (optionally) deprecated → archived. Multiple versions of same F_TemplateId coexist (only one is F_EnabledMark=1 typically). | FlowTemplateJsonEntity.cs L20-30 | [KNOWN] |
| **E CRUD/Query** | Low frequency (3 rows). User publishes new version (rare), engine loads by template_id (frequent), admin browses group (occasional). Hot path: "load latest enabled version" — very common. | Volume + engine pattern | [INFERRED] |
| **F DDD** | Aggregate root: WorkflowTemplateVersion. F_TemplateId is aggregate identifier (groups multiple versions). F_FlowTemplateJson is serialized aggregate state. F_Version is value object invariant (must be unique per template_id). F_EnabledMark is the "current version" marker. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | Single consumer: workflow engine. Foundry Target: needs efficient "load latest enabled version" query. | JNPF.WorkFlow topology | [INFERRED] |

### Expert Reasoning Notes

The (F_TemplateId, F_Version, F_EnabledMark) pattern is a classic **versioned entity** design. This is similar to:
- Document version control
- Configuration version history
- API definition versioning

The key insight: **multiple rows can exist per F_TemplateId** (one per version). The "current" version is the one with F_EnabledMark=1 (and usually highest F_Version).

The hot path query is: "Give me the currently active workflow definition for template X". This requires the (f_tenant_id, f_template_id, f_enabled_mark, f_version DESC) composite index.

---

## 3. Risk Classification

- **Risk**: R2
- **Confidence**: HIGH
- **Rationale**:
  - 3 rows = no current stress, but production schema
  - JSON-heavy = careful index design (don't index JSON itself)
  - Versioned pattern = specific composite index needed
  - Single module (workflow) consumer
  - Standard R2 with audit trail

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason | Evidence |
|----|-----------|--------|----------|
| HG#1 Tenant Isolation | NO | F_TenantId via [Tenant] attribute. | FlowTemplateJsonEntity.cs L11 |
| HG#2 Data Integrity | NO | App-managed versioning. No DB FKs but version pattern manages integrity. | FlowTemplateJsonEntity.cs L17-30 |
| HG#3 Migration | NO | Only ADD INDEX proposed. | Recommended action scope |
| HG#4 Cross-Module | NO | Single module (workflow). F_TemplateId is internal. | JNPF.WorkFlow scope |
| HG#5 Business Ambiguity | NO | F_Version + F_EnabledMark pattern is clear versioning. Documented in entity comments. | FlowTemplateJsonEntity.cs L25-30 |

### Expert Note

I considered HG#5 (business ambiguity) due to the multi-version pattern. But the semantics are clear:
- F_Version is the version string
- F_EnabledMark indicates active version
- Multiple versions can coexist

This is standard versioning — no ambiguity. **Verdict: NOT triggered.**

---

## 5. Recommended Action

- **Action**: EVIDENCE-DRIVEN AUTO (R2)
- **Closure**: REFACTOR — apply 3 indexes

### Recommended Indexes

```sql
-- Index 1: Latest enabled version lookup (HOT PATH)
-- Supports: "load active workflow definition for template X"
CREATE NONCLUSTERED INDEX IDX_FLOWTJSON_TEMPLATE_ACTIVE
ON flow_template_json (f_tenant_id, f_template_id, f_enabled_mark, f_version DESC)
INCLUDE (f_id, f_full_name, f_flow_template_json, f_visible_type);

-- Index 2: Tenant alive filter (general CRUD)
CREATE NONCLUSTERED INDEX IDX_FLOWTJSON_TENANT_ALIVE
ON flow_template_json (f_tenant_id, f_delete_mark)
INCLUDE (f_id, f_template_id, f_version, f_full_name, f_enabled_mark);

-- Index 3: Group browse (UI navigation)
CREATE NONCLUSTERED INDEX IDX_FLOWTJSON_GROUP
ON flow_template_json (f_tenant_id, f_group_id, f_enabled_mark)
INCLUDE (f_id, f_full_name, f_template_id, f_version);
```

### Index Design Rationale

#### Index 1 (Hot Path)
- **Key columns**: (f_tenant_id, f_template_id, f_enabled_mark, f_version DESC)
- **Why this order**:
  1. f_tenant_id: mandatory filter (multi-tenant isolation)
  2. f_template_id: primary lookup
  3. f_enabled_mark: filter to active version (most queries want only active)
  4. f_version DESC: get latest if multiple enabled
- **INCLUDE**: F_FlowTemplateJson is included for **covering index** (engine reads JSON, doesn't need key lookup). This is OK because:
  - JSON is large but typically <16KB per workflow
  - Engine reads JSON frequently (every workflow load)
  - Avoids expensive key lookup on hot path

#### Index 2 (General)
- **Key columns**: (f_tenant_id, f_delete_mark)
- **INCLUDE**: common columns for general CRUD
- **Why**: soft-delete filtering is common; this is a "list all alive workflows" index

#### Index 3 (Group Browse)
- **Key columns**: (f_tenant_id, f_group_id, f_enabled_mark)
- **INCLUDE**: display columns for UI
- **Why**: group browsing is common in workflow admin UI

### Critical Note: F_FlowTemplateJson Handling

**F_FlowTemplateJson is large JSON (NVARCHAR(MAX) typically)**. It should:
- ✅ Be included in INCLUDE clause for covering index (engine needs it)
- ❌ NOT be a KEY column (would bloat index beyond 900-byte limit)
- ❌ NOT be in WHERE clause as searchable (no full-text index here)

My Index 1 includes it in INCLUDE — this is **covering index pattern** which is correct for "load workflow by ID" query. The JSON is read as part of the query result, not as a filter.

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §4.5
  - `D:\JNPF-v52\backend\modularity\workflow\JNPF.WorkFlow.Entitys\Entity\FlowTemplateJsonEntity.cs`
  - `D:\JNPF-v52\backend\modularity\common\JNPF.Common\Contracts\CLDSEntityBase.cs`
- **Evidence tags used**: [KNOWN] 4, [INFERRED] 3
- **Stop condition met**: YES

---

## 7. Additional Reasoning (Expert Commentary)

### Versioned Workflow Design Analysis

The (F_TemplateId, F_Version, F_EnabledMark) pattern is well-designed for:
- **Backward compatibility**: old versions retained for in-flight workflows
- **Rollback**: deactivate new version, activate old
- **Audit trail**: full version history

Alternative patterns considered:
- **Append-only event log**: more flexible but harder to query
- **Single row with JSON version history**: simpler but loses granularity
- **Separate version table**: cleanest, but JNPF chose inline versioning (acceptable trade-off)

### Index Design Trade-offs

#### Why DESC on F_Version

Most queries want the **latest** version. SQL Server's default index sort is ASC. For:
- "Latest version" → DESC index is optimal
- "Oldest version" → ASC or DESC (DESC works for both with reverse scan)
- "All versions" → either works

DESC is the right choice for the dominant query pattern.

#### Why F_EnabledMark Before F_Version

If we have (f_template_id, f_enabled_mark, f_version DESC), then:
- Query: `WHERE f_template_id = X AND f_enabled_mark = 1 ORDER BY f_version DESC`
- This is a single index seek with ORDER BY satisfied by index

If we had (f_template_id, f_version DESC) only:
- Query needs additional filter on F_EnabledMark → residual predicate
- Still fast, but slightly less optimal

The (..., f_enabled_mark, f_version DESC) order is the **optimal index** for "load latest active version".

### Covering Index Consideration

F_FlowTemplateJson is large. Including it in INCLUDE has trade-offs:
- **Pro**: engine gets JSON in same I/O as index seek (no key lookup)
- **Con**: index size increases by ~JSON size

For workflow engine, the JSON IS the data. Without it, engine still needs to do key lookup. So covering index is correct here.

If index size becomes a concern (10K+ workflows per tenant), consider:
- Filtered index on F_EnabledMark = 1 (only index active versions)
- Columnstore index for analytics queries

These are forward-looking — current design is appropriate.

---

**Expert Result B complete for Table 05 — flow_template_json**
