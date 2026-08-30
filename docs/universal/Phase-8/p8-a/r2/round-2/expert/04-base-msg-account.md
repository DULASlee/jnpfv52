# R2 Round 2 — Table 04 — base_msg_account — Expert Result B

> **Date**: 2026-08-30
> **Reviewer type**: Independent AI Expert (Result B)
> **Reviewer session ID**: IAE-R2-2026-08-30
> **Source evidence accessed**: `p8-a/r2/round-2/evidence/SOURCE-EVIDENCE.md`
> **Isolation guarantee**: No reference to `p8-a/r2/round-2/skill/*` (Result A)

---

## 1. Table Overview

- **Name**: base_msg_account
- **Module**: system-core (messaging)
- **Entity**: MessageAccountEntity : TenantCLDSEntityBase
- **Row count**: 4 rows (config)
- **Tenant**: YES (inherited)
- **SoftDelete**: YES (inherited)
- **FKs**: 0
- **Special**: **SENSITIVE CREDENTIALS** — 4 sensitive fields (F_SMTP_PASSWORD, F_APP_SECRET, F_BEARER, F_PASSWORD). 39 columns total.

---

## 2. Seven-Dimension Assessment (A–G)

| Dim | Finding | Evidence | Tag |
|-----|---------|----------|-----|
| **A Schema** | 39 columns (28 declared + 11 inherited). **Sensitive fields in plaintext (string type)**: F_SMTP_PASSWORD, F_APP_SECRET, F_BEARER, F_PASSWORD. F_EnCode is business key. F_Channel/F_WebhookType/F_ApproveType are type discriminators. | MessageAccountEntity.cs L17-176 | [KNOWN] |
| **B Integrity** | No DB FKs. F_EnCode should be UNIQUE per tenant (business key), no constraint. **Plaintext credential storage** = security finding (but not HG#2 strict trigger). | MessageAccountEntity.cs L31-32, L65-170 | [KNOWN] |
| **C Index** | Hot paths: 1) Tenant CRUD (f_tenant_id); 2) Lookup by EnCode (business key); 3) Filter by Channel (which messaging platform); 4) Filter by WebhookType. | Messaging pattern | [INFERRED] |
| **D Lifecycle** | Standard CRUD. F_DeleteMark handles soft delete. **Credential rotation lifecycle** is application-level (not in schema). | Standard pattern | [KNOWN] |
| **E CRUD/Query** | Low frequency (4 rows). User creates account config, sends test, retrieves credentials. Hot path: load credentials when sending message. | Volume + usage | [INFERRED] |
| **F DDD** | Aggregate: MessageAccount. TenantId is aggregate identifier. **Sensitive fields should be Value Objects with encryption**. F_Channel is enum. | DDD analysis | [INFERRED] |
| **G Consumer/Target** | **Multiple modules consume**: messaging module, notification module, integrate module, workflow. 4+ modules. Foundry Target: channel-based lookup, sensitive credential masking. | JNPF messaging architecture | [INFERRED] |

### Expert Reasoning Notes

This is a **sensitive data table**. The 4 sensitive fields are:
1. **F_SMTP_PASSWORD** — SMTP credential
2. **F_APP_SECRET** — Cloud API secret (Aliyun, etc.)
3. **F_BEARER** — Bearer token for API auth
4. **F_PASSWORD** — Basic auth password

**Storage as plaintext string** is a security concern. While not strictly a Hard Gate, this should be flagged.

---

## 3. Risk Classification

- **Risk**: R3+
- **Confidence**: HIGH
- **Rationale**:
  - Sensitive credentials = security implications
  - Multi-module consumer (HG#4 likely)
  - F_EnCode UNIQUE missing (HG#2 borderline)
  - 39 columns is wide
  - 4 rows is low volume but high importance

---

## 4. Hard Gate Assessment

| HG | Triggered? | Reason | Evidence |
|----|-----------|--------|----------|
| HG#1 Tenant Isolation | NO | F_TenantId inherited. | TenantCLDSEntityBase inheritance |
| HG#2 Data Integrity | **borderline** | F_EnCode UNIQUE missing. Plaintext credential storage is security concern but not HG#2 strict trigger. | MessageAccountEntity.cs |
| HG#3 Migration | NO | Only ADD INDEX proposed. | Recommended action scope |
| HG#4 Cross-Module | **YES triggered** | 4+ modules consume this: messaging, notification, integrate, workflow. | JNPF messaging architecture |
| HG#5 Business Ambiguity | NO | Channel types are clear enum. | MessageAccountEntity.cs |

### Expert Note on HG#2 and HG#4

**HG#2 borderline**: Master Spec §10.3 specifies HG#2 trigger as:
- No FK where app expects FK (orphan risk)
- Missing UNIQUE that causes data corruption risk

F_EnCode UNIQUE is a business rule. Without it, two accounts could have the same EnCode → confusion but not corruption. **Verdict: borderline, NOT triggered.**

**HG#4 triggered**: 4+ modules consume this via service layer. This is the textbook cross-module trigger.

---

## 5. Recommended Action

- **Action**: HUMAN APPROVAL (R3+) — security + cross-module
- **Closure**: DEFERRED

### Why Defer

1. Sensitive credentials = security implications
2. HG#4 triggered = cross-module impact
3. Index recommendations need verification

### Required Before Action

1. SQL query for existing indexes
2. Architectural review: how are credentials used? Encrypted at rest?
3. Foundry Profile: credential masking requirements

### Conditional Recommendation (pending evidence)

If (f_tenant_id, f_delete_mark) not indexed:
```sql
CREATE NONCLUSTERED INDEX IDX_MSGACCOUNT_TENANT 
ON base_msg_account (f_tenant_id, f_delete_mark)
INCLUDE (f_id, f_en_code, f_full_name, f_channel);
```

If (f_tenant_id, f_channel) not indexed:
```sql
CREATE NONCLUSTERED INDEX IDX_MSGACCOUNT_CHANNEL
ON base_msg_account (f_tenant_id, f_channel, f_enabled_mark)
INCLUDE (f_id, f_en_code, f_full_name);
```

### Security Recommendations (DEFERRED, out of scope)

- Encrypt F_SMTP_PASSWORD, F_APP_SECRET, F_BEARER, F_PASSWORD at rest
- Mask credentials in query results
- Consider vault integration

---

## 6. Evidence Basis

- **Sources read**:
  - `evidence/SOURCE-EVIDENCE.md` §3.4
  - `D:\JNPF-v52\backend\modularity\message\JNPF.Message.Entitys\Entity\MessageAccountEntity.cs`
- **Evidence tags used**: [KNOWN] 4, [INFERRED] 3
- **Stop condition met**: PARTIAL (security concern requires escalation)

---

## 7. Additional Reasoning (Expert Commentary)

### Sensitive Field Categories

The 4 sensitive fields fall into 3 categories:

1. **Email credentials**: F_SMTP_PASSWORD
2. **Cloud API secrets**: F_APP_SECRET
3. **HTTP auth**: F_BEARER (Bearer token), F_PASSWORD (Basic auth)

Each has different rotation patterns:
- SMTP: rotate on personnel change
- Cloud API: rotate per security policy
- Bearer: rotate per token expiry
- Basic auth: rotate per security policy

**Recommendation**: Application logic should track rotation. Schema doesn't capture this.

### Cross-Module Consumer Analysis

The 4 modules that consume base_msg_account:
1. **messaging** (sends messages)
2. **notification** (system notifications)
3. **integrate** (third-party integrations)
4. **workflow** (approval notifications)

Each module has different access patterns:
- messaging: read for send
- notification: read for trigger
- integrate: read for outbound
- workflow: read for approval

All need efficient lookup by Channel (which provider).

### Round 2 Test: Sensitive Data Table

This is a critical Round 2 test:
- Does Skill correctly identify sensitive fields?
- Does Skill trigger HG#4 for multi-module consumer?
- Does Skill recommend escalation for security concern?

---

**Expert Result B complete for Table 04 — base_msg_account**
