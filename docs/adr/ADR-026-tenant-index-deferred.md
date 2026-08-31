# ADR-026: 为什么 Tenant Index 在本轮 Deferred

> **ADR**: ADR-026
> **Title**: Tenant Index Deferred — Insufficient Production Evidence
> **Status**: ACCEPTED
> **Date**: 2026-08-31
> **Context**: 15 个 Tenant Index 发现项全部 Deferred

---

## Context

Batch 29/30/31 分析阶段发现 15 个表可能需要 Tenant Index（多租户隔离索引）。

最终结论：**全部 15 项 Deferred，不在本轮迁移执行。**

---

## Decision

**Deferred 15 个 Tenant Index 发现项，等待触发条件满足后重新评估。**

---

## Reason

### 1. 生产数据不足，无法测量选择性

Tenant Index 的价值取决于：
- 租户数据分布（tenant_id 选择性是否 > 1%）
- 查询 workload（有多少查询以 `WHERE tenant_id = ?` 开头）

当前状态：
- 多个目标表为空表（0 行），无法测量选择性
- 有数据的表，tenant_id 全为 NULL（数据质量问题，非 schema 问题）

**正确做法**：等生产数据 > 100 行再测量。

### 2. 实体非 Tenant-Aware（False Positive）

通过 ORM Entity 分析发现：
- 多个表对应的 Entity 继承自 `CLDSEntityBase` / `CLDEntityBase`（非 TenantEntityBase）
- 这些表在 ORM 层**不是**多租户隔离的
- 即使加了 Tenant Index，SqlSugar 查询也不会使用（无 ITenantFilter 绑定）

**正确做法**：这些不是真正的 Tenant Index 需求，标记为 False Positive。

### 3. 数据质量问题优先于索引

`BASE_IM_CONTENT` / `BASE_IM_REPLY` 两个表实体标注为 tenant-aware，但数据全为 NULL。

加 Tenant Index 在 NULL 数据上是无用功（索引选择性与 NULL 比例相关）。

**正确做法**：先修复数据质量（填入真实 tenant_id），再评估索引。

---

## Deferred Items（7 项有触发条件）

| Gap ID | 表 | Deferred 原因 | 触发条件 |
|:---|:---|:---|:---|
| FR-004 | BASE_APP_DATA | 空表 | 生产数据 >100 行且选择性可测 |
| FR-009 | BASE_IM_CONTENT | 数据质量全 NULL | 先修复 NULL 数据 |
| FR-010 | BASE_IM_REPLY | 数据质量全 NULL | 先修复 NULL 数据 |
| FR-012 | BASE_INTEGRATE_NODE | 空表 + ORM 未确认 | 生产数据 + ORM 实体确认 |
| FR-013 | BASE_ORGANIZE_RELATION | 空表 + ORM 未知 | 生产数据 + ORM 确认 |
| FR-016 | BASE_SIGNATURE | 空表，非 tenant-aware | 实体重新分类为 tenant-aware |
| FR-017 | BASE_SIGNATURE_USER | 空表，非 tenant-aware | 实体重新分类为 tenant-aware |

---

## Alternatives Considered

| 方案 | 缺点 |
|:---|:---|
| 本轮执行 Tenant Index | 生产数据不足，无法证明收益；可能加错索引（选择性问题）|
| 不分析直接忽略 | 忽视真实的架构问题 |
| **Deferred（采用）** | 保持观察，有明确触发条件，不浪费迁移成本 |

---

## Consequences

- 本轮不执行任何 Tenant Index DDL
- 所有 15 项在 Final Matrix 中标记为 DEFERRED
- 后续评估时必须提供：生产数据行数、tenant_id 选择性（`SELECT tenant_id, COUNT(*) GROUP BY tenant_id`）、查询 workload 证据
- 数据质量问题（FR-009/FR-010）需要优先修复，这属于数据工程而非 Schema 工程

---

## Status

**ACCEPTED** — 全部 15 项 Deferred，Chief Architect 授权（2026-08-31）。

**Not Forgotten. Not Rejected. Not False Positive.**
当触发条件满足时，应重新评估这些表的 Tenant Index 需求。