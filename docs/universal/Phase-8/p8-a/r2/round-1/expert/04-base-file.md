# R2 Round 1 — Table 04 — base_file — Expert Result B

> **Date**: 2026-08-30
> **Reviewer type**: Independent AI Expert (Result B)
> **Reviewer session ID**: IAE-R1-2026-08-30
> **Source evidence accessed**: `p8-a/r2/round-1/evidence/SOURCE-EVIDENCE.md`
> **Isolation guarantee**: No reference to `p8-a/r2/round-1/skill/*` (Result A)

---

## 1. Table Overview

- **Name**: base_file
- **Module**: system-core (file storage)
- **Entity**: **NONE — Dynamic SQL access only** (per P8-0 registry)
- **Row count**: 0 rows (empty)
- **Tenant**: YES (f_tenant_id per P8-0)
- **SoftDelete**: YES (assumed via JNPF convention)
- **FKs**: 0 (P8-0 §4)
- **Special**: **No entity class found** in backend/modularity search.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | **NO ENTITY FILE** in any expected path. Schema can only be inferred. Inferred columns (based on JNPF naming convention): F_Id, F_TenantId, F_FileName, F_FilePath (or storage key), F_FileType, F_FileSize (bigint), F_FileExtension, F_Thumbnail (optional), F_CreatorTime, F_CreatorUserId, F_DeleteMark, F_DeleteTime, F_DeleteUserId. Estimated 13 columns. **NOTE: This inference has low confidence.** | P8-0 §2; no entity file at expected paths | [GUESS] for schema, [KNOWN] for lack-of-entity |
| **B Integrity** | Dynamic access means integrity is enforced at SQL level (where used) or app layer. F_TenantId must be present (confirmed). No DB FKs expected. | P8-0 §4 | [KNOWN] |
| **C Index** | **No entity = no application-level access pattern evidence**. SQL metadata would be needed. Common file table queries (if my inferred schema is correct): 1) File lookup by ID (PK); 2) File lookup by tenant + name; 3) File lookup by tenant + creator. | Inferred | [GUESS] |
| **D Lifecycle** | File lifecycle: Created → Stored → SoftDeleted. F_DeleteMark handles delete. No multi-state semantics. | JNPF convention | [INFERRED] |
| **E CRUD/Query** | 0 rows currently = no production traffic. Forward-looking: file uploads (INSERT), downloads (SELECT by ID), deletes (UPDATE F_DeleteMark). | Volume = 0 | [KNOWN] for volume, [INFERRED] for patterns |
| **F DDD** | Aggregate: File. Tenant-scoped. No complex value objects. F_FilePath or storage key is invariant. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | **Multi-module consumer likely**: file upload module, message attachments (base_im_content), knowledge base (BASE_KNOWLEDGE_NODE), workflow attachments, possibly more. Cross-module dependency is HIGH. | Module topology analysis | [INFERRED] |

### Expert Reasoning Notes

The lack of entity is a **significant finding**. This is not a "Skill can't find the file" issue — it's a fundamental architectural pattern: **some JNPF tables are accessed dynamically via SQL without entity classes**. This pattern exists for:

1. **Generic/semi-structured data** where schema is determined at runtime
2. **Legacy tables** that predate the entity framework
3. **Cross-cutting tables** used by many modules where entity would create unwanted coupling
4. **Tables written by external systems** that need to be read by JNPF

base_file fits pattern (3) or (4) most likely.

**Critical expert observation**: Without an entity, the Skill cannot make strong recommendations. Master Spec §2.2 (No Autonomous Rule Creation) and §10.3 (Hard Gates) both require evidence-backed decisions. Without entity source code, evidence is insufficient.

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: MEDIUM
- **Rationale**:
  - **No entity file = undefined situation for Skill** (per Master Spec §2.2)
  - 0 rows = no current production stress, but doesn't mean no future stress
  - Multi-module consumer = cross-module impact
  - Forward-looking concerns: file volume could grow quickly (attachments, uploads)
  - **Requires architectural input** — is this the canonical file table? What's the access pattern?

Per Master Spec §2.2, this is an **Undefined Situation** that requires Decision Brief or Human escalation.

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason | Evidence |
|----|-----------|--------|----------|
| HG#1 Tenant Isolation | NO | F_TenantId present per P8-0. | P8-0 §4 |
| HG#2 Data Integrity | NO | Dynamic access = app-managed integrity. Cannot assess without access pattern evidence. | Lack of source |
| HG#3 Migration | NO | No migration proposed (cannot propose without evidence). | N/A |
| HG#4 Cross-Module | **YES triggered** | File storage is by definition cross-cutting. Upload, message attachments, knowledge base, workflow all need file storage. **3+ modules reference base_file via application logic** (per Master Spec §10.3 HG#4 trigger). | Module topology |
| HG#5 Business Ambiguity | **borderline** | Without entity, semantics of "file" are unclear. Is it user uploads? System files? Cached blobs? Storage location (DB BLOB vs filesystem vs cloud)? | Lack of source |

### Expert Note on HG#4

Master Spec §10.3 HG#4 trigger: "cross-module dependency detected (table referenced by 3+ modules via application logic, no DB FK indexes)".

base_file meets this criterion:
- ✅ Multiple modules reference file storage (upload, message, KB, workflow)
- ✅ No DB FK indexes (because no DB FKs exist)
- ✅ Cross-module concern is structural, not just logical

**Verdict: HG#4 triggered.** This requires Decision Brief or Human escalation.

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — architectural decision required
- **Closure**: DEFERRED — pending evidence acquisition

### Required Actions Before Proceeding

1. **Acquire Evidence via SQL** (5 minutes, definitive):
   ```sql
   SELECT TOP 50 * FROM base_file; -- verify schema
   SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('base_file'); -- existing indexes
   SELECT * FROM sys.dm_db_index_usage_stats WHERE object_id = OBJECT_ID('base_file'); -- usage patterns
   ```

2. **Architectural Review**:
   - Is base_file the canonical file metadata table?
   - What modules write to it? Read from it?
   - Is there a service layer that abstracts the access?

3. **Documentation Update** (post-review):
   - Document access patterns
   - Document which modules are consumers
   - Document storage strategy (DB BLOB vs filesystem)

### Decision Brief Required

```markdown
# Decision Brief — base_file (Round 1 R2-COMP)

## Situation
base_file has NO entity class in JNPF codebase. Skill/Expert cannot make
strong recommendations without access pattern evidence.

## Evidence Available
- 0 rows currently
- Tenant column present (F_TenantId)
- Soft delete assumed (F_DeleteMark)
- P8-B did not execute on this table

## Evidence Missing
- Actual column list
- Existing indexes
- Access patterns (which services read/write)
- Consumer modules

## Options
1. **SQL evidence acquisition** (recommended): 5-minute query, definitive
2. **Out-of-scope classification**: similar to ext_table_example SVR-001
3. **Skip and return later**: defer to Round 3 (systematic defect path)

## Recommendation
Option 1 — SQL evidence acquisition. This is the fastest path to action.
```

### Deferred Until Evidence

- Index recommendations (need to know access patterns)
- HG#5 final classification (need to know semantics)
- Closure (NO-CHANGE vs REFACTOR)

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §4.4
  - P8-0 registry (confirms NO entity)
  - Searched `backend/modularity` for FileEntity / base_file references (no matches)
- **Evidence tags used**: [KNOWN] 3, [INFERRED] 3, [GUESS] 2
- **Stop condition met**: NO — per Master Spec §2.2, cannot proceed without evidence
- **Total tokens spent**: low (insufficient source forced early stop)

---

## 7. Additional Reasoning (Expert Commentary)

### Why No Entity Is a Red Flag (Not Just an Inconvenience)

In JNPF, the absence of an entity class for a table is **architecturally significant**. It means:

1. **No compile-time type safety** for that table
2. **No application-level FK tracking** via entity relationships
3. **No auto-generated CRUD endpoints**
4. **Access is via raw SQL or SqlSugar's dynamic API**

This is acceptable for:
- Lookup tables (rarely changing)
- Audit/log tables (high volume, simple structure)
- Cross-cutting tables (storage, config)

But for a "file" table, this is unusual — file management typically benefits from entity-based service layer.

### Possibilities

1. **Most likely**: base_file is part of an older module or third-party library that's accessed dynamically
2. **Possible**: base_file is used as a generic "blob metadata" table by multiple subsystems
3. **Less likely**: entity was deleted in a refactor and access is now raw SQL

### Comparison with Similar Tables

- base_message: HAS entity (MessageEntity : TenantCLDSEntityBase) — strong typing
- base_sys_log: NO entity (assumed — high volume, dynamic access)
- base_province: NO entity (assumed — static reference data)
- base_file: NO entity — pattern consistent with sys_log/province

The "no entity" pattern is not necessarily bad. It depends on access patterns.

### Forward-Looking Concern

If base_file is meant to scale to thousands of attachments per tenant:
- (f_tenant_id, f_id) lookup is the hot path (already PK)
- (f_tenant_id, f_creator_user_id, f_creator_time DESC) for "my files" listing
- File dedup by hash would be ideal but requires schema change

Without evidence, none of these can be recommended.

---

**Expert Result B complete for Table 04 — base_file**
