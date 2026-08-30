# Phase 8 — Shadow Mode Table Selection

**Phase**: 8 — P8-A
**Status**: PREPARED
**Version**: v1.0
**Date**: 2026-08-30

---

## 1. 目的

建立 Shadow Mode 首批 5 张 Table Units 的选择方法论。

**不是**：按功能清单或风险等级人为凑数。
**是**：从真实 JNPF 候选池出发，通过选择矩阵产出最具代表性的 5 张表。

---

## 2. 选择原则

### 自然形成风险分布

5 张表**不刻意堆高风险**，但也不刻意回避。目标自然分布：

```
至少 1 张 R0/R1（低风险独立表）
+
至少 1 张 R2（中风险）
+
至少 1 张 R3+（自然包含，不人为制造）
```

**禁止**：全部选 R3+ 来"证明 Skill 能处理复杂表"。

**禁止**：全部选 R0/R1 来"确保 Shadow 通过"。

### 代表性优先于覆盖度

目标是 5 张表组合起来形成**足够有代表性的生产样本**，而非一张表覆盖所有维度。

---

## 3. 候选池建立

### 数据来源

从 JNPF 真实数据库或 Entity 定义中提取所有表，建立候选池：

```
docs/universal/Phase-8/candidate-pool/
  candidate-pool.md
```

候选池包含：

| 字段 | 说明 |
|---|---|
| Table Name | 表名（如 BASE_USER） |
| Module | 所属模块（system/app/visualdev/workflow 等）|
| Column Count | 列数量 |
| FK Count | 外键数量 |
| Index Count | 索引数量（不含 PK）|
| Has Tenant | 是否有租户列（F_TENANT_ID）|
| Has SoftDelete | 是否有软删除列 |
| Has Status/Lifecycle | 是否有状态/生命周期字段 |
| Target Profile Ready | 与 Foundry Target Profile 对应程度 |
| Legacy Indicators | 历史包袱迹象（可为空的非必要列、命名不一致等）|

### 候选池建立步骤

1. 从 JNPF.Repository 层或 DB schema 提取所有表清单
2. 按 Module 分组
3. 排除 Pilot 1-3 已覆盖的表（BASE_AI_PIPELINE / BASE_KNOWLEDGE_NODE / BASE_KNOWLEDGE_EDGE / FLOW_TASK）
4. 对每张表填入候选池字段
5. 形成初步候选池列表

---

## 4. 选择矩阵

对候选池中每张表，从以下维度评估：

| 维度 | 描述 | 评分 |
|---|---|---|
| Schema Complexity | 列数量、列类型多样性 | 1（简单）→ 5（复杂）|
| Integrity Complexity | FK 数量、自引用、多重外键 | 1（独立）→ 5（高度关联）|
| Index/Query Complexity | 查询模式复杂度、索引需求 | 1（简单）→ 5（查询密集）|
| Lifecycle Complexity | 状态机、自定义生命周期 | 1（标准 CRUD）→ 5（复杂状态）|
| CRUD/Query Ratio | 是写密集还是读密集 | 1（写密集）→ 5（读密集查询复杂）|
| DDD Boundary Clarity | 聚合边界是否清晰 | 1（清晰）→ 5（边界模糊）|
| Target Profile Readiness | 与 Foundry Target Profile 的匹配程度 | 1（复杂映射）→ 5（直接对应）|
| Legacy Burden | 历史包袱（可为空的列、废弃字段等）| 1（干净）→ 5（高包袱）|
| Dependency Level | 被其他表依赖的程度 | 1（低依赖）→ 5（高依赖/被依赖）|

### 风险等级初步评估

基于选择矩阵初步评估每张表的 R 等级（R0-R5），来自 Universal Skill 的风险判定逻辑：

| 风险等级 | 触发条件 |
|---|---|
| R0 | 无任何风险；NO-CHANGE |
| R1 | 轻微风险；低优先级优化 |
| R2 | 中等风险；需要评估后决策 |
| R3 | 高风险；Human Gate |
| R4 | 极高风险；Multiple HG |
| R5 | 最高风险；跨模块/跨系统边界 |

---

## 5. 选择流程

```
Step 1: 建立候选池（所有真实表）
    ↓
Step 2: 对每张表应用选择矩阵
    ↓
Step 3: 按模块分组，确保跨模块覆盖
    ↓
Step 4: 优先选择矩阵得分分布均匀的表
    ↓
Step 5: 确保自然形成 R0/R1 + R2 + R3+ 组合
    ↓
Step 6: 最终 5 张表确认
```

### 批次组织原则（适用于 P8-C）

当进入 P8-C 时：

- **强关联表尽量同批次**：如果两张表在同一个业务流中（如 Order + OrderDetail），优先放同一 Batch
- **3-8 是默认窗口**：不是机械固定值，根据表间关联强度调整
- **低耦合表可以跨 Batch**：相互独立的表可以分散到不同 Batch

---

## 6. 首批 5 张表（待从候选池选取）

以下为模板，占位符待实际填充：

```
Shadow Table 01: ___________（待确认）
  Module: ___________
  预计风险: R___（待评估）
  选择理由: ___________

Shadow Table 02: ___________（待确认）
  Module: ___________
  预计风险: R___（待评估）
  选择理由: ___________

Shadow Table 03: ___________（待确认）
  Module: ___________
  预计风险: R___（待评估）
  选择理由: ___________

Shadow Table 04: ___________（待确认）
  Module: ___________
  预计风险: R___（待评估）
  选择理由: ___________

Shadow Table 05: ___________（待确认）
  Module: ___________
  预计风险: R___（待评估）
  选择理由: ___________
```

---

## 7. 批次关联图

首批 5 张表之间的依赖/关联关系（待填充）：

```
Table 01
  ├─ FK to: ___________
  ├─ FK from: ___________
  └─ Same module as: ___________

Table 02
  ├─ FK to: ___________
  ├─ FK from: ___________
  └─ Same module as: ___________

...

整体依赖图：
___________
```

---

## 8. 排除规则

以下表**不进入候选池**：

- Pilot 1-3 已覆盖的表（BASE_AI_PIPELINE / BASE_KNOWLEDGE_NODE / BASE_KNOWLEDGE_EDGE / FLOW_TASK）
- 明显不属于 JNPF 主数据模型的表（如临时表、日志表、备份表）
- 表结构信息完全未知的表

---

## 9. 候选池维护

Shadow Execution 完成后，将首批 5 张表标记为 `SHADOWED`。

候选池持续维护，供后续 P8-B / P8-C Batch 选择参考。

---

## 10. 当前状态

```
Phase 8                    🟢 OPEN
P8-A Shadow Preparation    ✅ PREPARED（候选池建立方法 + 选择矩阵就绪）
P8-A Shadow Execution      ⏸ NOT STARTED

首批 5 张表：待从真实 JNPF 数据库/Entity 中提取候选池后选取
```

**执行条件**：候选池建立 → 选择矩阵评估 → 5 张表确认 → Shadow Execution 启动
