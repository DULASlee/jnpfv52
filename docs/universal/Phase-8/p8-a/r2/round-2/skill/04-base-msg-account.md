# R2 Round 2 — Table 04 — base_msg_account — Skill Result A

> **Date**: 2026-08-30
> **Reviewer type**: table-refactor-expert Skill (Result A)
> **Source evidence**: `p8-a/r2/round-2/evidence/SOURCE-EVIDENCE.md`

---

## 1. Table Overview

- **Name**: base_msg_account
- **Module**: system-core (messaging)
- **Entity**: MessageAccountEntity : TenantCLDSEntityBase (entity present)
- **Row count**: 4 rows (low — config table)
- **Tenant**: YES (inherited from TenantCLDSEntityBase)
- **SoftDelete**: YES (inherited)
- **FKs in/out**: 0
- **Special**: **SENSITIVE CREDENTIALS** — contains 4 sensitive fields (F_SMTP_PASSWORD, F_APP_SECRET, F_BEARER, F_PASSWORD). 28 declared + 11 inherited = 39 columns total.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | 39 columns (28 declared + 11 inherited). F_SMTP_PASSWORD, F_APP_SECRET, F_BEARER, F_PASSWORD are **sensitive credentials**. F_EnCode is business key. F_Channel + F_WebhookType + F_ApproveType are type discriminators. | MessageAccountEntity.cs L17-176 | [KNOWN] |
| **B Integrity** | No DB FKs. F_EnCode should be UNIQUE per tenant (business key), no constraint. **Sensitive credential fields are stored in plaintext (string)** — security concern. | MessageAccountEntity.cs L31-32, L65-170 | [KNOWN] |
| **C Index** | Hot paths: 1) tenant CRUD (f_tenant_id); 2) lookup by EnCode (business key); 3) filter by Channel (1=email, 2=SMS, 3=WeChat, etc.); 4) filter by WebhookType. | JNPF messaging pattern | [INFERRED] |
| **D Lifecycle** | Standard CRUD. F_DeleteMark handles soft delete. No state machine. **Credentials rotation lifecycle** (not in schema — would be application logic). | Standard pattern | [KNOWN] |
| **E CRUD/Query** | Low frequency (4 rows). User creates account config, sends test message, retrieves credentials. Hot path: load credentials when sending message. | Volume + usage | [INFERRED] |
| **F DDD** | Aggregate: MessageAccount. TenantId is aggregate identifier. **Sensitive fields are Value Objects that should be encrypted** (DDD + security principle). F_Channel is enum. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | **Cross-module consumer**: messaging module, notification module, integrate module (for outbound), workflow (for approval notifications). Foundry Target: needs efficient lookup by channel for "send via channel X" pattern. Sensitive credentials need masking in Foundry Profile. | JNPF messaging architecture | [INFERRED] |

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: HIGH
- **Rationale**:
  - Sensitive credentials (security concern)
  - Multi-module consumer (HG#4 likely)
  - F_EnCode should be UNIQUE (HG#2 borderline)
  - 39 columns is wide
  - 4 rows is low volume but high importance

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason |
|----|-----------|--------|
| HG#1 Tenant Isolation | NO | F_TenantId inherited. |
| HG#2 Data Integrity | **borderline** | F_EnCode UNIQUE missing. Plaintext credential storage (security concern but not HG#2 strict trigger). |
| HG#3 Migration | NO | Only ADD INDEX proposed. |
| HG#4 Cross-Module | **YES triggered** | Multiple modules consume message accounts: messaging, notification, integrate, workflow. 4+ modules via application logic. |
| HG#5 Business Ambiguity | NO | Channel types are clear enum. |

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — security + cross-module concern
- **Closure**: DEFERRED

### Why Defer

1. **Sensitive credentials** = security implications
2. **Multi-module consumer** = cross-module impact
3. **HG#2 borderline** on F_EnCode UNIQUE
4. **HG#4 triggered** = needs architectural review

### Required Evidence

1. SQL query for existing indexes
2. Architectural review: how are credentials used? Encrypted at rest? Masked in queries?
3. Foundry Profile: credential handling requirements

### Potential Indexes (deferred)

```sql
-- Index 1: Tenant CRUD
CREATE NONCLUSTERED INDEX IDX_MSGACCOUNT_TENANT ON base_msg_account (f_tenant_id, f_delete_mark);

-- Index 2: Channel lookup (send message via channel X)
CREATE NONCLUSTERED INDEX IDX_MSGACCOUNT_CHANNEL ON base_msg_account (f_tenant_id, f_channel, f_enabled_mark);

-- Index 3: EnCode business key (UNIQUE constraint + index)
-- Note: requires data audit for duplicates first
CREATE UNIQUE NONCLUSTERED INDEX IDX_MSGACCOUNT_ENCODE ON base_msg_account (f_tenant_id, f_en_code);
```

### Security Recommendations (deferred, out of scope for R3+ refactor)

- Encrypt F_SMTP_PASSWORD, F_APP_SECRET, F_BEARER, F_PASSWORD at rest
- Mask credentials in query results
- Consider vault integration for credential storage

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §3.4
  - `D:\JNPF-v52\backend\modularity\message\JNPF.Message.Entitys\Entity\MessageAccountEntity.cs`
  - `D:\JNPF-v52\backend\modularity\common\JNPF.Common\Contracts\TenantCLDSEntityBase.cs`
- **Evidence tags used**: [KNOWN] 4, [INFERRED] 3
- **Stop condition met**: PARTIAL (security concern requires escalation)

---

## 7. State Machine Status

```
Current State: DEFERRED (HUMAN APPROVAL required)
```

---

**Skill Result A complete for Table 04 — base_msg_account**
