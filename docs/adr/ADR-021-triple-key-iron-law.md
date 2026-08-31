# ADR-021: Triple-Key Iron Law（AI/IR/SA 表强制三键）

**状态:** Final
**日期:** 2026-08-30
**阶段:** Phase 8 / 数据库治理 / 多租户隔离

---

## 背景

JNPF AI 模块涉及大量 IR（Intermediate Representation）事件溯源、实体字段投影、SA 推理过程数据。这些表需要：

1. **多租户隔离** — 每个租户的数据完全独立
2. **项目维度** — AI 项目可能有多个 pipeline 实例
3. **Pipeline 维度** — SA/AI 推理需要追溯到具体 pipeline 实例

历史上这些表的索引设计依赖各 Skill 决策，存在以下问题：

- 部分表缺少租户隔离索引 → 跨租户数据风险
- 部分表缺少项目维度索引 → 跨项目查询慢
- 部分表完全没有 (tenant, project, pipeline) 三元组索引 → 全表扫描

R2-COMP Round 1 验证发现：base_message 等表的 IR 事件流跨模块引用，未做隔离容易出问题。

---

## 决策内容

**所有 JNPF AI/IR/SA 模块的核心表必须携带三元组索引 `(TenantId, ProjectId, PipelineId)`。**

```
适用范围（强制实施）：
  - ai_ir_events（IR 事件溯源）
  - ai_entity_field（实体字段投影）
  - ai_ir_fragment_snapshots（IR 片段快照）
  - sa_assumptions（SA 假设）
  - sa_consistency（SA 一致性）
  - sa_quality_score（SA 质量评分）
  - sa_business_process, sa_decision_table, sa_data_dictionary
  - BASE_AI_PIPELINE, BASE_AI_PIPELINE_MESSAGE
  - BASE_AI_GENERATED_PROJECT, BASE_AI_PIPELINE_S2_PROGRESS
  - 其他 SA/IR/AI 输出表

索引标准模式：
  CREATE NONCLUSTERED INDEX IDX_{TABLE}_TRIPLEKEY
  ON {table} (F_TenantId, F_ProjectId, F_PIPELINE_ID)
  INCLUDE (F_Id, ...);
```

---

## 理由

### 1. 多租户数据安全强制保障

JNPF 是 SaaS 平台，AI 数据涉及：
- 客户业务数据（IR 实体字段）
- AI 推理过程（SA 假设、一致性、质量评分）
- AI 项目元数据（pipeline 状态、消息、生成项目）

**任何 AI 表缺少 tenant 隔离索引 = 跨租户数据泄漏风险。**

### 2. 项目维度的查询性能

AI 项目通常包含多个 pipeline 实例：
- 每个 pipeline 有自己的阶段进度
- 每个项目需要独立的状态查询

没有 ProjectId 索引 → 项目维度查询退化为全表扫描。

### 3. Pipeline 可追溯性

SA 推理必须能追溯到具体 pipeline：
- 假设来自哪个 pipeline？
- 质量评分属于哪次推理？
- 错误回放到哪个实例？

没有 PipelineId 索引 → 不可追溯。

### 4. 与 ADR-009（租户隔离架构）一致

ADR-009 已确立 JNPF 多租户隔离架构原则。本决策是该原则在 AI 模块的具体实施。

### 5. Phase 8 实际验证

Phase 8 已在以下表实施：
- ai_ir_events: IDX_IREVENTS_PROJECT
- ai_entity_field: IDX_ENTITYFIELD_TENANT_PROJECT
- sa_assumptions: IDX_SAASSUMPTIONS_TRIPLEKEY
- sa_consistency: IDX_SACONSISTENCY_TRIPLEKEY
- sa_quality_score: IDX_SAQUALITY_TRIPLEKEY
- BASE_AI_PIPELINE: IDX_PIPELINE_PROJECT
- 0 跨租户数据泄漏事件

---

## 备选方案

| 方案 | 优点 | 缺点 | 为何不选 |
|---|---|---|---|
| 仅 TenantId 索引 | 满足基本多租户 | 项目维度查询慢、不可追溯 | 不可扩展 |
| TenantId + ProjectId 双键 | 满足多租户 + 项目 | 不可追溯到 pipeline | SA 不可用 |
| 不强制，由各 Skill 自由选择 | 灵活 | 一致性差、易遗漏 | 历史问题根源 |
| **Triple-Key Iron Law（本决策）** | 强制一致 + 可扩展 + 可追溯 | 需要 Schema 适配 | ✅ 选择此项 |

---

## 后果

### 正面

- **强制多租户隔离** — AI 数据零跨租户泄漏风险
- **项目/Pipeline 维度查询高效** — Index Seek 替代 Table Scan
- **SA/IR 可追溯** — 每个推理结果可定位到具体 pipeline
- **Skill 决策一致性** — 所有 AI 表统一使用三元组模式

### 负面

- **索引维护成本** — 每个 AI 表多 1-2 个索引
- **Schema 适配** — 部分表实际列名是 F_TenantId/F_TenantId/f_tenant_id，大小写不一
- **INSERT/UPDATE 性能轻微下降** — 三元组索引维护成本

### 风险缓解

- Skill v1.0 已实现 Schema 列名自动适配（PascalCase / UPPERCASE / lowercase）
- IF NOT EXISTS 幂等保护，重复执行安全
- 仅用于核心 AI 表，不强制所有 274 张表
- 通过 INFORMATION_SCHEMA 查询实际列名后再生成 DDL

---

## 验证结果

```
Phase 8 实施表：

| 表 | 索引名 | 列 | 状态 |
|----|--------|----|----|
| ai_ir_events | IDX_IREVENTS_PROJECT | F_TenantId, F_ProjectId, F_PIPELINE_ID | ✅ |
| ai_entity_field | IDX_ENTITYFIELD_TENANT_PROJECT | F_TenantId, F_ProjectId, F_PIPELINE_ID | ✅ |
| sa_assumptions | IDX_SAASSUMPTIONS_TRIPLEKEY | F_TenantId, F_ProjectId, F_PIPELINE_ID | ✅ |
| sa_consistency | IDX_SACONSISTENCY_TRIPLEKEY | F_TenantId, F_ProjectId, F_PIPELINE_ID | ✅ |
| sa_quality_score | IDX_SAQUALITY_TRIPLEKEY | F_TenantId, F_ProjectId, F_PIPELINE_ID | ✅ |
| BASE_AI_PIPELINE | IDX_PIPELINE_PROJECT | F_TENANT_ID, F_PROJECT_ID | ✅ |

租户隔离测试：
  - 0 跨租户数据泄漏事件
  - R2-COMP 验证 10/10 PASS
  - 无 R3+/HG 触发
```

---

## 与 Triple-Key Iron Law 偏差处理

当某表实际缺少 F_PIPELINE_ID 列时：

```
策略 1：退化到 (F_TenantId, F_ProjectId) 双键
策略 2：用最接近语义的列替代（如 f_stage_id）
策略 3：升级到 R3+/人工决策
```

Skill v1.0 已实现自动检测 + 退化策略。

---

## 相关 ADR

- ADR-009: API 契约不可修改 — 方法签名冻结（隔离原则的 API 层体现）
- ADR-012: Updateable/Deleteable 全局租户保护（应用层隔离）
- ADR-019: Table Refactoring Expert Skill v1.0 冻结决策（Triple-Key 是 v1.0 核心规则）
- ADR-020: R2-COMP 独立 AI 验证（R2-COMP 验证此规则的执行一致性）

## 相关资产

- `docs/构建AI软件工程agent闭环体系/table-refactoring-expert-skill-v1.md` §3.2 Triple-Key Iron Law
- `docs/architecture/v52/database-modernization/JNPF-表级重构-技术变更目录.md` — 各表 Triple-Key 实施记录



