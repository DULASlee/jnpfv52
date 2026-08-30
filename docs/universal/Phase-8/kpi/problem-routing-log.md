# Problem Routing Log

> **Phase**: 8 — Cross-Phase Mechanism
> **Status**: ACTIVE
> **Version**: v1.0
> **Date**: 2026-08-30

---

## 路由分类（6 类 — 来自 Master Plan §10）

```
JNPF-specific
    → JNPF Extension (v1.0 FROZEN)

Skill execution issue
    → Skill Evolution (Level A/B/C)

Universal rule issue
    → Master Spec Evolution

BBB capability gap
    → BBB Product Backlog

Business ambiguity
    → Human Decision

Provider/database constraint
    → Target/Provider Profile
```

---

## 路由条目（P8-0 起所有 divergence / rework 记录）

| Date | Table / Batch | Issue | Classified As | Routed To | Status | Resolved Date | Resolution |
|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — |

(P8-0 不产生 routing 条目 — 仅机制建立)

---

## 路由规则（硬性）

1. **单个 Table finding 不得自动暂停整个 Phase**
2. **JNPF-specific 不得直接修改 Universal Core**
3. **Business ambiguity 不得 AI 自行决定** — 必须升级 Human Decision
4. **Provider constraint 必须路由到 Target/Provider Profile** — 不得作为 universal rule 改 Master Spec
5. **所有 rework 项必须先入 routing log 后执行** — 不得跳过记录

---

## 路由决策矩阵（quick reference）

| 问题症状 | 路由 |
|---|---|
| 表命名 / 列命名 JNPF 特殊（如 `F_*`）| JNPF-specific → JNPF Extension |
| Skill 输出 missing finding 但实际有 | Skill execution → Skill Evolution |
| Skill 输出 false finding | Skill execution → Skill Evolution |
| Hard Gate 阈值太严导致 false positive | Universal rule → Master Spec |
| Hard Gate 阈值太松导致 false negative | Universal rule → Master Spec |
| Skill 不支持新 SQL Server 特性 | Provider constraint → Target Profile |
| 字段类型 BBB 不支持（如 sql_variant）| BBB gap → BBB Product Backlog |
| 业务表无 F_TENANT_ID | Business ambiguity → Human Decision |
| 不知道如何迁移业务数据 | Business ambiguity → Human Decision |

---

## 路由升级规则

- **Critical**：立即暂停当前 Batch，路由到对应通道，修正后重跑
- **High**：当前 Table Unit 完成后路由到对应通道，不影响 Batch
- **Medium / Low**：记录入 log，继续下一张表，Batch Summary 时统一处理
