# R2 Round 1 — Table 01 — base_message — Skill Result A

> **Date**: 2026-08-30
> **Reviewer type**: table-refactor-expert Skill (Result A)
> **Source evidence**: `p8-a/r2/round-1/evidence/SOURCE-EVIDENCE.md`
> **Process**: Execution Manual §3 (5-step SOP); State machine = DISCOVERED → ASSESSED → DESIGNED → READY → REFACTORED → VERIFIED → CLOSED

---

## 1. Table Overview

- **Name**: base_message (BASE_MESSAGE)
- **Module**: system-core (specifically message module: JNPF.Message)
- **Entity**: MessageEntity : TenantCLDSEntityBase (entity file present)
- **Row count**: 1229 rows (active business volume)
- **Tenant**: YES (F_TenantId via TenantCLDSEntityBase)
- **SoftDelete**: YES (F_DeleteMark via base class)
- **FKs in/out**: 0 (per P8-0 registry §4 — 业务表几乎无 FK)
- **Special**: Message lifecycle (F_IsRead, F_ReadTime, F_ReadCount) — typical R2 mid-volume CRUD

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | 19 columns (8 declared + 11 inherited). All [SugarColumn] attributes align with DB columns. F_Money/F_Amount as string in some entities, but base_message uses proper types (int?, string, DateTime?). NO schema issues. | MessageEntity.cs L13-63 | [KNOWN] |
| **B Integrity** | No DB-level FKs. F_UserId is logical reference to base_user, app-managed. No unique constraint on F_UserId+F_BodyText (could be a duplicate message). 0 incoming/outgoing FKs in registry. | P8-0 §4; MessageEntity.cs L37-38 | [KNOWN] |
| **C Index** | No existing indexes on this table per P8-0 evidence. Critical hot paths: (a) user inbox query: f_tenant_id + f_user_id + f_is_read + f_creator_time DESC; (b) unread count: f_tenant_id + f_user_id + f_is_read. Both are essential. | Inferred from JNPF message inbox pattern | [INFERRED] |
| **D Lifecycle** | Message lifecycle: Created → Sent → Read → Archived. F_IsRead (0/1), F_ReadTime, F_ReadCount. State transition: 0 → 1 (single transition). No multi-state machine. Standard R2 lifecycle. | MessageEntity.cs L43-56 | [KNOWN] |
| **E CRUD/Query** | High-frequency: INSERT on send, SELECT on user inbox, UPDATE on read. Indexes essential for SELECT performance. UPDATE on F_IsRead requires partial index. | JNPF.Message module pattern | [INFERRED] |
| **F DDD** | Aggregate root: Message. TenantId is aggregate identifier. State transition (unread → read) is the only invariant. No value objects split. Simple aggregate. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | Consumers: message-center module (UI), notification module, IM module. Target Foundry Profile: needs f_tenant_id+f_user_id+f_is_read index for inbox query. Multi-module dependency → consider HG#4. | JNPF.Message service layer | [INFERRED] |

---

## 3. Risk Classification

- **Risk**: R2
- **Confidence**: HIGH
- **Rationale**: Mid-volume (1229 rows), tenant + softdelete, lifecycle complexity (read state). NOT R0/R1 (needs index + audit trail). NOT R3+ (no cross-table ambiguity). Standard R2 evidence-driven.

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason |
|----|-----------|--------|
| HG#1 Tenant Isolation | NO | F_TenantId present via TenantCLDSEntityBase. Multi-tenant isolation OK. |
| HG#2 Data Integrity | NO | No DB FKs (app-managed), but no critical integrity violation. 1229 rows shows data is being managed. |
| HG#3 Migration | NO | Only ADD INDEX proposed. No schema change. |
| HG#4 Cross-Module | **borderline** | Multiple modules (message-center, notification, IM) reference this table. However, no DB FK indexes needed (no FKs). Cross-module is logical, not physical. |
| HG#5 Business Ambiguity | NO | Single state field (F_IsRead). Clear 0→1 transition. |

---

## 5. Recommended Action

- **Action**: EVIDENCE-DRIVEN (R2) — needs audit trail, evidence-backed index recommendation
- **Closure**: REFACTOR — apply 2 indexes

### Recommended Indexes

```sql
-- Index 1: User inbox query (most common)
CREATE NONCLUSTERED INDEX IDX_MESSAGE_USER_INBOX
ON base_message (f_tenant_id, f_user_id, f_is_read, f_creator_time DESC)
INCLUDE (f_id, f_title, f_type, f_read_time);

-- Index 2: Tenant-wide unread count (admin/notification)
CREATE NONCLUSTERED INDEX IDX_MESSAGE_TENANT_UNREAD
ON base_message (f_tenant_id, f_is_read, f_creator_time DESC)
INCLUDE (f_id, f_user_id, f_type);
```

---

## 6. Evidence Basis

- **Sources read**: 
  - `evidence/SOURCE-EVIDENCE.md` §4.1
  - `D:\JNPF-v52\backend\modularity\message\JNPF.Message.Entitys\Entity\MessageEntity.cs`
  - `D:\JNPF-v52\backend\modularity\common\JNPF.Common\Contracts\TenantCLDSEntityBase.cs`
  - P8-0 registry §4 (no FKs for 业务表)
- **Evidence tags used**: [KNOWN] 3, [INFERRED] 4
- **Stop condition met**: YES — KNOWN evidence sufficient for schema/inheritance; INFERRED is appropriate for query patterns based on JNPF module conventions
- **Total assessment**: Standard R2 table, 2 indexes proposed

---

## 7. State Machine Status

```
DISCOVERED  → (input valid)                          ✅
ASSESSED    → (7 dimensions filled)                  ✅
DESIGNED    → (2 indexes aligned with findings)      ✅
READY       → (Approval Gate = AUTO at R2)           ✅
REFACTORED  → (proposed; not yet executed)           ⏸ deferred
VERIFIED    → (would run after execution)            ⏸ pending
CLOSED      → (would run after verification)         ⏸ pending
```

**Current State**: DESIGNED → READY (indexes proposed, ready for review)

---

**Skill Result A complete for Table 01 — base_message**
