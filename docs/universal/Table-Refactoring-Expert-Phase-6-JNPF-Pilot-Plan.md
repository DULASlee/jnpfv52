# Phase 6 — JNPF Pilot Plan

**Phase**: 6 — JNPF Pilot
**Status**: PLAN → 提交用户审批后执行
**Upstream**: Phase 5 FROZEN
**Scope**: 3 tables (AiPipelineEntity, KnowledgeNode+Edge pair, FlowTaskEntity)

---

## 0. Pilot Mission

Validate that **Universal Core + JNPF Extension + Foundry Target Profile** can correctly drive a real JNPF table through the full Table Refactoring lifecycle:

```
DISCOVERED → ASSESSED → DESIGNED → READY → REFACTORED → VERIFIED → CLOSED
```

**Primary question**: Does the Extension correctly handle JNPF-specific facts without polluting the Universal Core reasoning?

**Secondary question**: Does the Foundry Target Profile usefully assess readiness for Foundry.Data migration?

---

## 1. Pilot Tables

### Table 1: `BASE_AI_PIPELINE` — AiPipelineEntity

| Attribute | Value |
|---|---|
| Entity | `AiPipelineEntity` |
| Base class | `TenantCLDSEntityBase` |
| Table | `BASE_AI_PIPELINE` |
| Module | InteAssistant |
| Why this table | Complex custom lifecycle (Frozen/Resume/Checkpoint) independent of soft-delete; multi-state; Version; JSON checkpoint; custom status/stage enums; SourcePipelineId FK |
| Expected risk | R3 (Human Approval — custom lifecycle decision) |
| Hard Gate candidates | #5 (lifecycle vs soft-delete ambiguity), #8 (aggregate boundary with ProjectId) |

### Table 2a: `BASE_KNOWLEDGE_NODE` — KnowledgeNodeEntity

| Attribute | Value |
|---|---|
| Entity | `KnowledgeNodeEntity` |
| Base class | `TenantCLDSEntityBase` |
| Table | `BASE_KNOWLEDGE_NODE` |
| Module | InteAssistant |
| Why this table | Aggregate root candidate? FK-free (no child refs in entity itself); JSON Properties; Version for UPSERT; Label field (string enum: entity/field/component) |
| Expected risk | R2 (Evidence-Driven Auto-Apply) |
| Hard Gate candidates | #4 (Version field — optimistic locking vs standard versioning?) |

### Table 2b: `BASE_KNOWLEDGE_EDGE` — KnowledgeEdgeEntity

| Attribute | Value |
|---|---|
| Entity | `KnowledgeEdgeEntity` |
| Base class | `TenantCLDSEntityBase` |
| Table | `BASE_KNOWLEDGE_EDGE` |
| Module | InteAssistant |
| Why this table | FK pair (SourceNodeId + TargetNodeId → BASE_KNOWLEDGE_NODE); aggregate boundary ambiguous; cascade unknown; RelationshipType string field |
| Expected risk | R3 (Human Approval — FK cascade + aggregate boundary) |
| Hard Gate candidates | #2 (missing FK constraint at DB level), #5 (Label semantics), #8 (aggregate boundary with KnowledgeNode) |

**Note**: Tables 2a and 2b are assessed together as a logical Aggregate pair.

### Table 3: `FLOW_TASK` — FlowTaskEntity

| Attribute | Value |
|---|---|
| Entity | `FlowTaskEntity` |
| Base class | `CLDSEntityBase` (note: NOT TenantCLDSEntityBase — uses EntityBase with TenantId via inheritance) |
| Table | `FLOW_TASK` |
| Module | WorkFlow |
| Why this table | 26 fields; 6-state workflow machine (draft/processing/passed/rejected/revoked/terminated); Restore flag; Suspend flag; self-referential ParentId; multiple FKs; lifecycle vs soft-delete interaction |
| Expected risk | R3–R4 (Cross-table + custom lifecycle) |
| Hard Gate candidates | #3 (lifecycle state machine vs standard CRUD lifecycle), #5 (Restore + Suspend flags vs soft-delete interaction), #8 (multiple FKs) |

---

## 2. Execution Protocol

Per Universal Core (Execution Manual §3), each table goes through 5 Steps:

```
Step 1: Discover       — Input: table name → Output: DDL + entity + query samples
Step 2: Assess         — Input: discovery output → Output: 7-dimension Findings (A–G)
Step 3: Design         — Input: assessment → Output: DESIGNs + Approval Gate
Step 4: Refactor        — Input: approved design → Output: refactor actions (or no-change)
Step 5: Verify         — Input: refactor output → Output: 13 DoDs + Closed Gate
```

### 2.1 JNPF Extension Loading (per table)

For each table:

```
1. Load Universal Core (Master Spec + Execution Manual)
2. Load JNPF Extension
3. Load Foundry Target Profile (for readiness assessment)
4. For KnowledgeNode+Edge: assess as Aggregate pair (F → F.1)
5. For FlowTaskEntity: check EntityBase vs TenantEntityBase for Tenant filter
```

### 2.2 Key Extension calls per table

**Table 1 — AiPipelineEntity:**
- Extension §3: map `TenantCLDSEntityBase` → Marker Concepts: Tenant + Audit + SoftDelete + EnabledMark
- Extension §4: ITenantFilter = PRESENT (TenantCLDSEntityBase extends TenantEntityBase)
- Extension §7: Repository = ISqlSugarRepository<T> (not IRepository)
- Extension §9: Target Readiness = **Adapter-Ready**
- **Key question**: Frozen flag (`F_FROZEN bool`) is NOT the same as soft-delete. Is this a separate lifecycle? Is it within table scope or beyond?
- **Key question**: `Checkpoint nvarchar(max) JSON` — is this a JSON column that affects refactorability?
- **Key question**: `FailureCounts JSON` — same

**Table 2a+2b — KnowledgeNode + Edge:**
- Extension §3: Both are `TenantCLDSEntityBase`
- Extension §4: ITenantFilter = PRESENT
- Extension §9: Target Readiness = **Adapter-Ready** for both
- **Key question**: No DB-level FK constraint on SourceNodeId/TargetNodeId — is this a Hard Gate #2?
- **Key question**: KnowledgeNode is deleted — what happens to KnowledgeEdge? Is there code-level cascade? No DB constraint?
- **Key question**: Aggregate boundary: Is KnowledgeNode the Aggregate Root and Edge a Child? Or are both separate Reference Data entities?

**Table 3 — FlowTaskEntity:**
- Extension §3: `CLDSEntityBase` → Marker Concepts: Tenant (via EntityBase) + Audit + SoftDelete + EnabledMark
- Extension §4: ITenantFilter = PRESENT (CLDSEntityBase extends EntityBase which implements ITenantFilter)
- **Key question**: `CLDSEntityBase` (not `TenantCLDSEntityBase`) — but EntityBase has TenantId field. Is ITenantFilter active for FLOW_TASK?
- **Key question**: `F_RESTORE int?` (0=can restore, 1=cannot) and `F_SUSPEND int?` (0=no, 1=suspended) — these are lifecycle flags beyond standard soft-delete. Are these within table scope?
- **Key question**: `F_STATUS int` (0/1/2/3/4/5) — 6-state machine. Is this within table scope or does it cross module boundaries?
- **Key question**: FlowTaskEntity has `ProcessId`, `TemplateId`, `RejectDataId` — multiple FK references. Hard Gate #8?

### 2.3 Foundry Target Profile calls per table

**For all 3 tables:**
- Map entity's Marker Concepts → Foundry contracts
- Assess: Is this entity Foundry-ready? (implements IAuditableEntity? ISoftDeleteEntity? ITenantEntity?)
- Assess: DeleteMark → IsDeleted conversion risk (Hard Gate #3 trigger)
- Assess: Repository readiness (ISqlSugarRepository vs IRepository)

---

## 3. Pilot Validation Checklist

### 3.1 Extension Routing Validation

For each JNPF-specific fact encountered, confirm:
- [ ] Fact is sourced from **JNPF Extension** (not guessed by Skill)
- [ ] Fact is NOT added to Universal Core
- [ ] Fact is labeled `[EXTENSION EXCEPTION — JNPF-specific]`

### 3.2 DeleteMark Hard Gate Validation

- [ ] For each table with `DeleteMark`: Skill recognizes `int? DeleteMark` pattern
- [ ] Skill does NOT conclude "change to bool IsDeleted" without Hard Gate #3
- [ ] Evidence of int/bool difference is captured before any conversion decision

### 3.3 ITenantFilter Validation

- [ ] For each table: Skill correctly identifies ITenantFilter presence/absence via base class
- [ ] Skill does NOT claim cross-tenant isolation without verifying base class
- [ ] Architecture claim is distinguished from runtime proof

### 3.4 Aggregate Boundary Validation (KnowledgeNode+Edge)

- [ ] Skill recognizes FK pair without DB constraint
- [ ] Skill identifies cascade ambiguity as a Hard Gate #2 or #8
- [ ] No autonomous "assume cascade delete" conclusion

### 3.5 Custom Lifecycle Validation (AiPipeline + FlowTask)

- [ ] Skill recognizes Frozen flag (AiPipeline) as separate from soft-delete
- [ ] Skill recognizes Restore + Suspend flags (FlowTask) as separate from soft-delete
- [ ] Skill does NOT conflate custom lifecycle with standard lifecycle concepts
- [ ] Skill routes to Extension for custom lifecycle facts

### 3.6 Universal Purity Validation

- [ ] No JNPF vocabulary in Universal Core sections
- [ ] JNPF-specific facts only in Extension context
- [ ] Evidence taxonomy ([KNOWN]/[COMPUTED]/[INFERRED]/[GUESS]/[DESIGN]) used correctly
- [ ] No project-specific shortcuts in Universal reasoning

---

## 4. KPI Recording

Per table:

| Metric | Table 1 | Table 2a | Table 2b | Table 3 |
|---|---|---|---|---|
| Capability dimensions completed | A–G | A–G | A–G | A–G |
| Findings count | N | N | N | N |
| Hard Gates triggered | N | N | N | N |
| False Positives | N | N | N | N |
| False Negatives | N | N | N | N |
| Risk level assigned | R? | R? | R? | R? |
| Approval Gate required | ? | ? | ? | ? |
| Autonomous resolution | Y/N | Y/N | Y/N | Y/N |
| TABLE CLOSED | Y/N | Y/N | Y/N | Y/N |
| Completion time (AI-minutes) | N | N | N | N |
| Extension entries used | N | N | N | N |

Aggregate:

| Metric | Value |
|---|---|
| Total tables assessed | 4 (3 entities, 1 aggregate pair) |
| Total TABLE CLOSED | N |
| Universal purity violations | N |
| Extension contamination events | N |
| Median completion time | N min |
| P90 completion time | N min |
| False Positive Rate | N% |
| False Negative Rate | N% |
| Autonomous resolution rate | N% |

---

## 5. Pilot Exit Criteria

| # | Criterion | Evidence |
|---|---|---|
| 1 | All 3 tables + 1 aggregate pair complete TABLE CLOSED | Evidence Ledger for each |
| 2 | Extension routing correctly used for all JNPF facts | Extension call log per table |
| 3 | DeleteMark Hard Gate #3 correctly triggered for each int DeleteMark | Decision Brief per trigger |
| 4 | ITenantFilter assessed via base class, not assumed | Extension call evidence |
| 5 | Aggregate boundary correctly identified for KnowledgeNode+Edge | Hard Gate #2 or #8 Decision Brief |
| 6 | Custom lifecycle (Frozen/Resume/Restore/Suspend) correctly routed to Extension | Extension call evidence |
| 7 | Universal Core purity preserved (0 JNPF vocabulary in Core sections) | Purity scan per table |
| 8 | Foundry Target Profile correctly maps all Marker Concepts | Readiness assessment per table |
| 9 | No unresolved P0/P1 findings | All P0/P1 resolved or escalated |
| 10 | Pilot produced 0 Universal Core modifications | Zero Core changes logged |
| 11 | Pilot produced ≤ 3 Extension entries (additions only, no modifications) | Extension change log |
| 12 | KPI baseline recorded | Pilot KPI table complete |

---

## 6. Pilot Output

After execution, produce:
- `Table-Refactoring-Expert-Phase-6-JNPF-Pilot-Report.md` — aggregate report
- Per-table Evidence Ledgers (JSON or structured text)

---

## 7. Version History

| Version | Date | Change |
|---|---|---|
| v1.0 (draft) | 2026-08-29 | Phase 6 Pilot Plan. 3 tables + 1 aggregate pair selected. |
