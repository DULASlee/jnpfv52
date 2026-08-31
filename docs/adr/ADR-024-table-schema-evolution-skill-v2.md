# ADR-024 — Table Schema Evolution Expert Skill v2.0 FROZEN

> **Status**: 🔵 **ACCEPT_PENDING** (Batch 29 Pilot Validation PASS — awaiting full R2-COMP 10 normal + 10 adversarial + R1 Human Governance for FROZEN transition)
> **Decision Date**: TBD (after full Phase 1.6 Gate closure)
> **Supersedes**: ADR-019 (Table Refactoring Expert Skill v1.0 FROZEN)
> **Authority**: Chief Architect
> **Validation**: R2-COMP 10/10 + R1 5/5 + 7 DoD + 3 Simulation Cases (all required)

---

## 2026-08-31 Verification Result

Actual command execution per AGENTS.md 0.5 Gate Function:

| Acceptance Item | Actual Command | Result |
|------------------|----------------|--------|
| DoD-01 contract_check | `python -m tsee contract_check` | ❌ **Traceback crash** |
| DoD-02 gap_analysis | `python -m tsee gap_analysis` | ❌ **Traceback crash** |
| DoD-03 decide | `python -m tsee decide` | ✅ Works |
| DoD-04 no_change_validate | `python -m tsee no_change_validate` | ⚠️ Outputs "missing 8 dimensions" without checking |
| DoD-05 evidence_collect | `python -m tsee evidence_collect` | ❌ **PLACEHOLDER** ("MVP: requires DB") |
| DoD-06 rollback_validate | `python -m tsee rollback_validate` | ❌ **PLACEHOLDER** ("MVP: requires test DB") |
| DoD-07 human_gate | `python -m tsee human_gate` | ✅ Works |
| ADR-024 Status | grep Status | ❌ DRAFT |
| v6.0 base skill | ecc memory search DEFERRED | ❌ DEFERRED (P1 Execution Capability) |
| V6 R1 Human Governance | Real human reviews 5 tables | ❌ NEVER EXECUTED |
| Skill v1.0-FROZEN | git status | ❌ Overwritten (`.claude/skills/table-refactor-expert/SKILL.md`) |

**Aggregate**: 2/7 DoD PASS, 5/7 acceptance items FAILED.

**Decision**: Skill v2.0 is **NOT 彻底实现**. Cannot proceed to JNPF P0 table refactoring.

Evidence: `mem_20260830_aee2009f742e41f98c17` (ECC memory) + `backend/database/validation/Phase16-Enforcement-Hardening-Report.md` (Phase 1.6 fixes applied but verification still incomplete).

---

## Pre-requisites for ACCEPTED status (in priority order)

1. **Fix DoD-01 / DoD-02 crash** — root cause: CLI argument parsing, yaml.safe_load import path
2. **Implement DoD-05 / DoD-06** — connect to live DB (pyodbc) for schema snapshot + rollback dry-run
3. **Execute V6 R1 Human Governance** — real human reviews 5 fixed tables (per `mem_20260829_137dca482e91455598fd` P8-A.3 handoff)
4. **Resolve v6.0 DEFERRED state** — Execution Capability gate (P1) must be addressed before v2.0 can be considered canonical
5. ~~**Investigate git status canonical doc deletions** — `MASTER-JNPF*.md`, `D12-Architecture-Slice-v1.0.md`, `runservice-engine-refactor*.md`, `harness/UEEA-Agent-Runtime-Engineering-Rules.md` etc. deleted (not by current session)~~ **✅ RESOLVED 2026-08-31** — 18 files restored via `git checkout HEAD -- <file>`. See `mem_20260830_075582b836fc4940aed3`. Root cause of original deletion still unknown (predates current session).

---

## Context

Phase 8 反查触发了对 Skill v1.0 的结构性缺陷分析：

1. **缺少 Target Schema Contract** → AI 把"列名代理"当作 Migration
2. **NO-CHANGE 证明机制缺失** → 154 张 NO-CHANGE 仅证明"没找到改动"，未证明"符合目标"
3. **性能"加索引=加速"逻辑推理** → 无 Before/After 测量
4. **Schema Migration 分类治理缺失** → 静态命名/业务语义/低代码动态混为一谈
5. **v1 CR 过度现代化倾向** → 引入 RLS / Temporal Tables / Outbox 等不必要项目

专家评审（v2 重置后）追加：

6. **Migration 交付物缺位** → AI 只产报告不产可执行 Migration Artifact
7. **运行时兼容性验证缺失** → 仅 SQL Server 执行成功 ≠ JNPF 全链路通过
8. **低代码平台特化缺失** → wform_/ext_ 被普通规则误改
9. **证据与声明脱钩** → "已治理"无证据
10. **批量完成无代表性证明** → 132 张 NO-CHANGE 一次性关闭

---

## Decision

**Skill v1.0 → v2.0**（Schema Evolution Edition），含 10 Iron Laws + Gap Analysis Layer + 7 Skill DoD + 3 Simulation Cases + Schema Migration Governance。

### v2.0 核心变更

| 维度 | v1.0 | v2.0 |
|------|------|------|
| **Iron Laws 数** | 0（仅 6 Hard Gates） | **10 条**（01-10） |
| **Target Schema Contract** | 缺失 | **8 维度强制** |
| **Gap Analysis Layer** | 无 | **6 gap 类型 + 4 severity** |
| **Migration Type** | 无分类 | **A/B/C 三分类** |
| **Migration Bundle** | 无要求 | **4 件套强制**（forward/rollback/verify/evidence） |
| **运行时验证** | 无 | **7 层链路**（DB → Permission） |
| **Dynamic Platform** | 无特化 | **Type C 强制跳过** |
| **NO-CHANGE 证据** | 无要求 | **8 维度证据强制** |
| **Performance** | 逻辑推理 | **Before/After 测量强制** |
| **Batch Completion** | 无要求 | **代表性证明 1+1+1** |

### 兼容性策略

| 项 | 兼容性 |
|----|--------|
| Phase 8 已沉淀的 248 张表治理 | **保留，不追溯** |
| Skill v1.0 SKILL.md | **保留为 v1.5 ARCHIVED** |
| v1.0 Master Spec | **保留，新增 v2.0 章节** |
| Evidence 文件（95+） | **向后兼容，字段扩展** |
| ADR-019（v1.0 FROZEN 决策） | **标记 SUPERSEDED，保留历史** |

---

## 10 Iron Laws

| # | Law | 关键约束 |
|---|-----|---------|
| IRON-TABLE-01 | No Change ≠ No Action | NO-CHANGE 必须 8 维度证据 |
| IRON-TABLE-02 | Mapping Is Not Migration | 禁止列名代理代替真迁移 |
| IRON-TABLE-03 | Every Table Needs Target Contract | 每张表必须有 8 维度 Contract |
| IRON-TABLE-04 | Security Boundary First | P0-Security 优先 + 4 维度审计 |
| IRON-TABLE-05 | Performance Claim Requires Measurement | Before/After 测量强制 |
| IRON-TABLE-06 | Migration First-Class | 4 件套 Migration Artifact 强制 |
| IRON-TABLE-07 | Runtime Compatibility First | 7 层运行时链路验证 |
| IRON-TABLE-08 | Dynamic Platform Exception | wform_/lowcode_/运行时 ext_ 跳过 |
| IRON-TABLE-09 | Evidence Over Declaration | 完成声明必须绑定证据文件 |
| IRON-TABLE-10 | Batch Completion Requires Representative Proof | 1 复杂 + 1 普通 + 1 动态 |

---

## 7 Skill DoD

| # | DoD | 验收 |
|---|-----|------|
| DoD-01 | Target Schema Contract 可执行（Table Contract Matrix） | `python -m tsee.contract-matrix` |
| DoD-02 | Gap Analysis Layer（6 gap 类型） | `python -m tsee.gap-analysis <table>` |
| DoD-03 | Migration Decision Engine（自动 Type A/B/C） | `python -m tsee.decide <table>.<column>` |
| DoD-04 | No Change Validator（8 维度证据强制） | `python -m tsee.no-change-validate <table>` |
| DoD-05 | Evidence Collector（自动收集 4 类证据） | `python -m tsee.evidence-collect <table>` |
| DoD-06 | Rollback Validator（forward + rollback 配对 + dry-run） | `python -m tsee.rollback-validate <change_id>` |
| DoD-07 | Human Gate Boundary（AI 自动 vs 人工审批明确） | `python -m tsee.human-gate-check --auto-only` |

---

## 3 Simulation Test Cases

| Case | 表类型 | 期望行为 |
|------|--------|---------|
| Case A | 普通业务表（ext_order，Type A 拼写错误） | 自动 DIRECT_RENAME + 4 件套 + Human Gate NOT_REQUIRED |
| Case B | 低代码动态表（wform_contractapproval） | 自动 SKIP + Decision Brief + Human Gate REQUIRED |
| Case C | P0-Security 表（base_user，3 G0 缺陷） | 自动 REFACTORED_P0 + 5 Iron Laws + Human Gate REQUIRED + 7 层运行时检查 |

---

## Validation Requirements (Mandatory)

Skill v2.0 cannot be marked FROZEN unless **ALL** below pass:

| # | 验证项 | 目标 | 状态 |
|---|--------|------|------|
| 1 | 7 Skill DoD | 7/7 PASS | ☐ |
| 2 | 3 Simulation Cases | 3/3 PASS | ☐ |
| 3 | R2-COMP Round 1 | 5/5 EXACT + 40/40 dimensions | ☐ |
| 4 | R2-COMP Round 2 | 5/5 EXACT + 40/40 dimensions | ☐ |
| 5 | R2-COMP Safety Gates | 4/4 PASS | ☐ |
| 6 | R1 Human Governance | 5/5 PASS | ☐ |
| 7 | Stop Rule Triggered | YES (no Round 3) | ☐ |
| 8 | Skill v1.0 → v1.5 ARCHIVED | DONE | ☐ |

---

## Out of Scope (Explicit)

- ❌ 不动 Phase 8 已沉淀的 248 张表
- ❌ 不实现自动 Repository 代码生成（v3.0 候选）
- ❌ 不实现跨数据库方言（MySQL/PG 仅 v1.0/v2.0 SQL Server）
- ❌ 不实现 DML 数据迁移（DDL only）
- ❌ 不实现 CQRS / Outbox / Event Sourcing（属未来架构演进）
- ❌ 不实现自动 FK 增强
- ❌ 不实现主键 nvarchar(50) → bigint（JNPF GUID 必要）

---

## Related ADRs

| ADR | 关系 |
|-----|------|
| ADR-019 (v1.0 FROZEN) | **SUPERSEDED** by ADR-024 |
| ADR-020 (R2-COMP validation) | **RETAINED** as-is |
| ADR-021 (Triple-Key Iron Law) | **INCORPORATED** into IRON-TABLE-03 (Target Contract 维度) |
| ADR-022 (NO-CHANGE Active Judgment) | **REPLACED** by IRON-TABLE-01 (8 维度证据强制) |
| ADR-023 (Schema Drift Pre-Execution) | **REPLACED** by IRON-TABLE-06 (Migration First-Class) |

---

## Approval

- [ ] **Chief Architect 审批**
- [ ] **Database Engineering Lead 审批**
- [ ] **AI Engineering Lead 审批**

**Approval Date**: TBD

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 0.1 DRAFT | 2026-08-31 | AI Engineer + Chief Architect | 初稿，待 Phase 1 验证 |

---

## Appendices

- **A. Skill v2.0 设计规格**：[`docs/superpowers/specs/2026-08-30-表级重构专家Skill-v2.0设计规格.md`](../../docs/superpowers/specs/2026-08-30-表级重构专家Skill-v2.0设计规格.md)
- **B. Skill v2.0 SKILL.md**：[`.claude/skills/table-refactor-expert/SKILL.md`](../../.claude/skills/table-refactor-expert/SKILL.md)
- **C. Master Spec v2.0**：[`.claude/skills/table-refactor-expert/master-spec-v2.md`](../../.claude/skills/table-refactor-expert/master-spec-v2.md)
- **D. Simulation Test Cases**：[`docs/superpowers/specs/2026-08-30-表级重构专家Skill-v2.0-Simulation-Tests.md`](../../docs/superpowers/specs/2026-08-30-表级重构专家Skill-v2.0-Simulation-Tests.md)
- **E. R2-COMP 验证计划**：[`docs/superpowers/plans/2026-08-30-表级重构Skill-v2.0-R2-COMP-验证计划.md`](../../docs/superpowers/plans/2026-08-30-表级重构Skill-v2.0-R2-COMP-验证计划.md)
- **F. CR-20260830-01 v2**：[`.claude/change-requests/CR-20260830-01.md`](../../.claude/change-requests/CR-20260830-01.md)
- **G. JNPF Target Schema Contract**：[`docs/superpowers/specs/2026-08-30-JNPF-Target-Schema-Contract.md`](../../docs/superpowers/specs/2026-08-30-JNPF-Target-Schema-Contract.md)