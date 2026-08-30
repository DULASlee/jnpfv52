# Table Refactoring Expert — Universal Execution Manual v1.0

**Phase**: 2 — Universal Execution Manual
**Status**: Draft → 用户审批后冻结
**Date**: 2026-08-29
**Upstream**: `Table-Refactoring-Expert-Product-Definition.md` v1.0 (FROZEN) · `Table-Refactoring-Expert-Master-Spec.md` v1.0 (FROZEN)
**Downstream**: Phase 3 Universal SKILL · Phase 5 Extension · Phase 6 Pilot
**Not in scope of this document**: Universal 技术标准（属 Master Spec）、AI 操作指令（属 Phase 3 SKILL）、任何 Consumer/Target System 的具体知识（属 Phase 5 Extension/Profile）。

---

## 0. 文档约束力

冻结后：

1. 本文档是 TRE 的**唯一操作手册**。与 Master Spec 冲突时，**以 Master Spec 为准**；Master Spec 与本手册冲突时，先改 Master Spec 再适配本手册。
2. 本文档**不复制** Master Spec 的技术规则（What / Why / Rule）；仅将规则**操作化**为流程（When / How / Gate / Flow）。
3. 每条流程规则如需引用 Master Spec 的判定标准，使用**引用**而非**复述**。
4. 本文档**不写** AI 如何执行的提示词细节——属 Phase 3 SKILL 范围。
5. 本文档**不引入**第二套技术规则；不得通过本手册修订 Master Spec 的判定准则。
6. 修改必须升版本号并登记 §15 版本历史，旧版本保留存档。

---

## 1. 文档定位（三层职责清晰分离）

| 文档 | 回答的问题 | 性质 |
|---|---|---|
| **Master Spec**（Phase 1） | What / Why / Rule — 什么是好的表级设计、什么是判定准则 | 权威规范源 |
| **Execution Manual**（本文件，Phase 2） | When / How / Gate / Flow — 何时做什么、按什么顺序、何时停、何时升 | 操作手册 |
| **Universal SKILL**（Phase 3） | AI 如何执行这套流程 | AI 操作大脑 |

**任何规则只允许出现在 Master Spec**。本手册中如出现"应该这样做"的描述，必须以"依据 Master Spec §X.Y"开头，否则视为 scope creep。

---

## 2. 正式生命周期（Table State Machine）

### 2.1 状态定义

| 状态 | 含义 | 进入条件 | 离开条件 |
|---|---|---|---|
| **DISCOVERED** | 表被列入待评估池；存在基础元数据 | 满足 P0 Discovery 输入 | 完成 Assess 输入校验 |
| **ASSESSED** | 七维 Capability 评估完成；Risk 等级判定 | Assess 完成；Finding 全部有 `[KNOWN]/[COMPUTED]` 证据 | Design 输入就绪 |
| **DESIGNED** | `[DESIGN]` 标签下的目标态已定义 | Design 完成；目标态与 Finding 一一对应 | 风险等级决策已做出 |
| **READY** | 设计已批准 / 自主批准；等待执行窗口 | Risk Gate 通过；Batch 已分配 | 实际开始执行 Refactor |
| **REFACTORED** | Refactor 步骤已完成（含 no-change 标注） | Refactor 步骤 Input/Action/Evidence/Output 全达成 | Verify 步骤开始 |
| **VERIFIED** | Verification 通过；所有 13 条 DoD 满足 | Verify 完成；KPI 字段全部填毕 | 触发 Closed Gate |
| **CLOSED** | 表级重构生命周期结束 | Closed Gate 全部条件满足 | 仅可由 Re-trigger 重新打开 |

### 2.2 状态转移矩阵

| From | To | 触发动作 | 守门条件 |
|---|---|---|---|
| — | DISCOVERED | Discovery 步骤产出物登记 | 输入元数据合法（表存在、未 CLOSED） |
| DISCOVERED | ASSESSED | Assess 步骤完成 | 七维 Capability 每维至少一条 Finding 或显式 N/A |
| ASSESSED | DESIGNED | Design 步骤完成 | `[DESIGN]` 标签存在且与 Finding 一一对应 |
| DESIGNED | READY | Approval Gate 通过 | 风险等级决策完成（R0/R1 自动；R2 证据自检；R3+ 批准） |
| DESIGNED | ASSESSED | Design 缺证据回退 | Finding 与 DESIGN 不一致需重评 |
| READY | REFACTORED | Refactor 步骤启动 | Batch 调度执行 |
| REFACTORED | VERIFIED | Verify 步骤完成 | 13 条 DoD 全达成（见 Master Spec §13.2） |
| VERIFIED | CLOSED | Closed Gate 通过 | 5 条件全满足（见 §11） |
| ASSESSED / DESIGNED / READY | DISCOVERED | 严重证据反转回退 | 新证据推翻前提（如 PK 实际为多列） |
| 任意非 CLOSED | DISCOVERED | Hard Gate 触发（见 Master Spec §10.3） | Decision Brief 未拍板前不进入下一态 |

### 2.3 关键约束

- **`READY ≠ REFACTORED`** — READY 是评估与设计完成、待执行窗口；REFACTORED 是实际动手做了。两态之间可被审批、排期、Batching 任意打断。
- **不允许跳跃状态** — 如不允许 DISCOVERED → DESIGNED（跳过 Assess）；违反则触发重做。
- **不允许 CLOSED → 其他态**（除非 Re-trigger 条件触发）。

---

## 3. 五步 SOP（Discover → Assess → Design → Refactor → Verify）

每步固定六字段：**Input · Action · Evidence · Output · Stop Condition · Escalation**。

### 3.1 步骤 1：Discover

| 字段 | 内容 |
|---|---|
| **Input** | 待评估表清单（来自优先级队列 / 业务依赖图 / 用户指定）；表的基础元数据（DDL 概要、所属模块） |
| **Action** | 登记表为 DISCOVERED；建立 Evidence Ledger 初始结构；抓取 DDL + Entity + 索引 + 物理 FK 概览 |
| **Evidence** | `[KNOWN]` — DDL 全文、Entity 全文、索引清单、FK 清单（来自项目数据库 schema 视图） |
| **Output** | 状态 = DISCOVERED；Ledger 含七维 Capability 占位；表元数据快照文件 |
| **Stop Condition** | 元数据齐全；占位字段全部初始化 |
| **Escalation** | 表不存在 / 元数据缺失 → Hard Gate #1（PK 语义不明，详见 Master Spec §10.3 #1） |

### 3.2 步骤 2：Assess

| 字段 | 内容 |
|---|---|
| **Input** | DISCOVERED 表的 Ledger + 表元数据 |
| **Action** | 按 Master Spec §3–§9 七维逐项评估；每维产出至少一条 Finding 或显式 N/A；判定 Risk 等级；命中 Hard Gate 立即 STOP |
| **Evidence** | `[KNOWN]` 优先；不足时用 `[COMPUTED]`；多源推断用 `[INFERRED]`；达 Evidence Threshold 即停（Master Spec §11.3） |
| **Output** | 状态 = ASSESSED；每维 Finding + Risk 等级 + Hard Gate 检测结果 |
| **Stop Condition** | 七维全部填毕（Finding 或 N/A）；Risk 等级已判定 |
| **Escalation** | 命中任一 Hard Gate（Master Spec §10.3）→ STOP → Decision Brief → 人类决策 |

### 3.3 步骤 3：Design

| 字段 | 内容 |
|---|---|
| **Input** | ASSESSED 表的 Finding 列表 |
| **Action** | 为每条 Finding 写 `[DESIGN]` 目标态；定义 Target Profile 接入点（抽象）；定义 Refactor Type（Schema / Data / Index / Constraint / Code）；定义 Verification Criteria |
| **Evidence** | `[DESIGN]` 标签下的目标态必须可追溯到某条 Finding |
| **Output** | 状态 = DESIGNED；Design Ledger 含目标态清单 + Refactor 类型 + 验证标准 |
| **Stop Condition** | 每条 Finding 有对应 DESIGN；Refactor 类型与 Verification Criteria 已定义 |
| **Escalation** | Finding 与 DESIGN 无法对齐 → 回退至 ASSESSED；引入新 Finding → 回退至 Assess |

### 3.4 步骤 4：Refactor

| 字段 | 内容 |
|---|---|
| **Input** | DESIGNED 表 + 批准决策 |
| **Action** | 按 Refactor 类型执行（Schema / Data / Index / Constraint / Code）；每步产生中间证据；no-change 路径直接跳过 |
| **Evidence** | 变更前快照、变更命令、变更后快照、回滚脚本（如适用） |
| **Output** | 状态 = REFACTORED；Refactor Ledger 含变更流水 + 中间验证记录 |
| **Stop Condition** | 全部 Refactor Action 完成（含 no-change 标注）；中间验证全部通过 |
| **Escalation** | 变更失败 / 中间验证不通过 → 局部回滚（见 §8 Rollback）→ 决策"继续 / 暂停 / 升级" |

### 3.5 步骤 5：Verify

| 字段 | 内容 |
|---|---|
| **Input** | REFACTORED 表 + Refactor Ledger |
| **Action** | 执行 Master Spec §13.2 13 条 DoD 逐条核对；执行性能动作的 §5 存档号（如有）；记录 Before / After 状态 |
| **Evidence** | DoD 13 条逐项证据 + KPI 字段值 + Verification 输出 |
| **Output** | 状态 = VERIFIED；Verification Ledger 含 13 条 DoD 状态 + KPI |
| **Stop Condition** | 13 条 DoD 全达成；P0/P1 = 0 |
| **Escalation** | DoD 任一条不达成 → 回退至 REFACTORED；Hard Gate 触发 → 回退至 ASSESSED |

### 3.6 Step 与 State 的关系

```
DISCOVERED ──(Discover)──> 临时态 ──> ASSESSED ──(Assess)──> 临时态 ──> DESIGNED
DESIGNED ──(Design)──> 临时态 ──> READY ──(Approval Gate)──> REFACTORED ──(Refactor)──> VERIFIED ──(Verify)──> CLOSED
```

每个 Step 完成即触发对应状态转移；Step 中遇到 Escalation 则不转移状态。

---

## 4. Risk-Adaptive Execution（流程重量由风险决定，不是表大小）

### 4.1 核心原则

> **流程重量由 Risk 等级决定，不由表大小、行数、列数决定。**

### 4.2 Risk-Adaptive Flow Routing

| Risk 等级 | 流程重量 | 关键差异 |
|---|---|---|
| **L1-R0** No change | **Lightweight** | Discover → Assess（一句话 Concludes）→ CLOSED；跳过 Design / Refactor / Verify 详细步骤 |
| **L1-R1** Low risk | **Lightweight** | Discover → Assess → Refactor → Verify → CLOSED；Design 可省略至一句话目标态 |
| **L1-R2** Structural | **Standard** | 五步全跑；Approval Gate 为证据驱动自主；Verify 全 DoD |
| **L1-R3** Data / semantic | **Heavy** | 五步全跑 + Approval Gate（人工批准）；Design 必须含数据体检 + 回滚脚本 |
| **L1-R4** Cross-table / aggregate | **Heavy +** | 五步全跑 + Product+Architecture Gate；Design 必须含跨表影响分析 |
| **L1-R5** Destructive | **Heaviest** | 五步全跑 + Product+Architecture Gate + Pilot Dry-run + 灰度；Design 必须含完整迁移方案 + 回滚演练 |

### 4.3 R0 / R1 的具体简化路径

**R0 No-change 路径**（1 步内可关闭）：

```
DISCOVERED
  → Assess（Conforms 五维 + G N/A）
  → CLOSED
```

**R1 Low-risk 路径**（2 步内可关闭）：

```
DISCOVERED
  → Assess
  → Refactor（执行低影响变更）
  → Verify（快速核对）
  → CLOSED
```

### 4.4 R3+ 的额外要求

- **R3** 必须出 Decision Brief（Input / Options / Risks / Recommendation）。
- **R4** 必须 Product + Architecture 拍板；含跨表影响分析。
- **R5** 必须 Pilot Dry-run + 灰度；含完整迁移方案 + 回滚演练。

### 4.5 Risk 升级路径

任一 Finding 在 Refactor 或 Verify 阶段发现需要升级 Risk 时：

| 升级 | 行动 |
|---|---|
| R0 → R1 | 继续 Refactor，但加轻量 Verify |
| R1 → R2 | 补做完整 Assess + Design |
| R2 → R3 | STOP → Decision Brief → 人工批准 |
| R3 → R4 | STOP → Product + Architecture Gate |
| R4 → R5 | STOP → 全量回滚评估 + 重新 Design |

---

## 5. Approval Gate Matrix（决策权矩阵）

### 5.1 决策权映射

| Risk 等级 | 决策权 | Gate 名称 | 产物 |
|---|---|---|---|
| **L1-R0** | AI 自主 | Auto-Close Gate | Ledger + Concludes 记录 |
| **L1-R1** | AI 自主 | Auto-Apply Gate | Refactor 流水 + Verify 结果 |
| **L1-R2** | AI 自主（证据驱动） | Evidence-Driven Auto Gate | Refactor 流水 + Verify 结果 + 证据清单 |
| **L1-R3** | **人工批准** | Human Approval Gate | Decision Brief + 数据体检 + 回滚脚本 |
| **L1-R4** | **Product + Architecture Gate** | Cross-Table Gate | Decision Brief + 跨表影响分析 |
| **L1-R5** | **Product + Architecture Gate + Pilot Dry-run + 灰度** | Destructive Gate | Decision Brief + Pilot 演练结果 + 灰度方案 |

### 5.2 Gate 触发条件

**AI 自主 Gate**（R0/R1/R2）的触发条件：

- 七维 Assessment 完成
- Evidence Threshold 已达（Master Spec §11.3）
- 无 Hard Gate 触发（Master Spec §10.3）
- Refactor 类型与 Risk 等级匹配

**Human Approval Gate**（R3）的触发条件：

- 以上 AI 自主条件 + Refactor 类型含数据迁移 / 语义变更
- Decision Brief 已写出（含 Input/Options/Risks/Recommendation）

**Product + Architecture Gate**（R4/R5）的触发条件：

- 以上 Human Approval 条件 + 跨表 / 跨模块 / 不可逆变更
- 影响分析 + 灰度方案完整

### 5.3 Approval ≠ New Audit（关键纪律）

- "需要人批准"**不等于**重新做一轮审计。
- Human Approval 读 Assess 产出 + Design Ledger + Refactor Ledger，**不重跑七维 Assessment**。
- Gate 决策的依据是 Ledger 中的证据，不是新证据采集。
- 决策耗时应在 1–3 工作日内（参考 Master Spec §16 Generic Validation 节奏）。

### 5.4 Gate 决策产物

| Gate | 必产 |
|---|---|
| Auto-Close | Ledger 完整性 + Concludes 一句话 |
| Auto-Apply | Refactor 流水 + Verify 结果 |
| Evidence-Driven Auto | 上述 + Evidence 清单 |
| Human Approval | Decision Brief + 批准人签字 + 批准时间 |
| Cross-Table | 上述 + 跨表影响分析 + Gate 决议 |
| Destructive | 上述 + Pilot 演练结果 + 灰度方案 |

---

## 6. Evidence Routing（按 Capability 路由证据来源）

### 6.1 Evidence Routing Matrix

| Finding 类型 | 证据来源（按优先级） | 严禁范围 |
|---|---|---|
| **Schema（A）** | DDL + Entity + 一条真实读写路径 | 全项目 Service 扫描（除非必要） |
| **Integrity（B）** | PK / FK / Constraint 元数据 + 业务规则 | 全项目 SQL 历史 |
| **Index（C）** | 一条真实查询（含 Where/OrderBy/Join/Lifecycle Filter）+ 列分布 + 索引统计 | 整个项目性能日志 |
| **Lifecycle（D）** | Tenant / Soft-Delete / Audit 的真实代码路径 + 业务规则 | 全部业务文档 |
| **CRUD / Query（E）** | 真实 Service / Repository 代码路径 + 慢查询 | 整个 Service 层 |
| **DDD（F）** | 业务规则 + 一致性边界证据 + Entity 关系 | 全项目领域模型 |
| **Target Readiness（G）** | Target Profile 契约摘要 + 本表 Marker Concept 列表 | Target Profile 全文（除本表相关章节） |

### 6.2 Evidence Sufficiency Stop Rule（强约束）

每条 Finding 达到 Master Spec §11.3 最低阈值后**立即停止取证**。判定细则：

| 决策类型 | 最低证据集（来自 Master Spec §11.3） |
|---|---|
| 字段语义 | DDL + Entity + 一条真实读写路径 |
| 索引设计 | 一条真实查询 + 列分布 |
| Risk 判定 | 影响面 + 回滚成本 + 数据风险 |
| Marker Concept | DDL + Entity + 一处读写路径 |
| 聚合分类 | 业务规则 + 一致性边界证据 |

**禁止行为**：

- "为了更确定"继续扫描全仓
- 反复验证已 `[KNOWN]` 事实
- 在未确认决策前并行开多条证据链

### 6.3 Routing 误用检测

如发现下列模式，触发 Evidence Routing 自检：

- 评估 Schema Finding 时读了 Service 层代码 → 路由错误
- 评估 Index Finding 时读了业务文档全文 → 路由错误
- 评估 DDD Finding 时仅读了 DDL → 证据不足
- 同一条 Finding 反复取证超过 3 轮 → 触发 Stop Rule 自检

---

## 7. Table Evidence Ledger（统一格式）

### 7.1 Ledger 字段

每张表维护一个 Ledger，**字段集固定**：

| 字段组 | 字段 | 说明 |
|---|---|---|
| **Current Fact** | `[KNOWN]` 事实条目 | DDL / Entity / 索引 / FK 中可直接读取 |
| **Current Fact** | `[COMPUTED]` 推导条目 | 由已知事实计算 |
| **Current Fact** | `[INFERRED]` 推断条目 | 多源推断 |
| **Current Fact** | `[GUESS]` 假设条目 | 未证实（仅临时标注） |
| **Target State** | `[DESIGN]` 目标态条目 | 不可与 `[KNOWN]/[COMPUTED]` 混用 |
| **Decision** | Risk 等级、Hard Gate 检测、Approval Gate 决议 | 决策记录 |
| **Change** | Refactor Action 流水 + 中间验证 | 变更记录 |
| **Verification** | 13 条 DoD 状态 + KPI 字段 + 性能存档 | 验证记录 |

### 7.2 Ledger 沿用的 Evidence Taxonomy

**沿用 Master Spec §11.1 的五标签**，**不创造第二套 evidence taxonomy**：

`[KNOWN]` `[COMPUTED]` `[INFERRED]` `[GUESS]` `[DESIGN]`

### 7.3 Ledger 的生命周期

```
DISCOVERED  → 初始化 Ledger（Current Fact 占位）
ASSESSED    → Current Fact 填毕
DESIGNED    → Target State 填毕
READY       → Decision 填毕
REFACTORED  → Change 填毕
VERIFIED    → Verification 填毕
CLOSED      → Ledger 封存（仅 Re-trigger 可修改）
```

### 7.4 Ledger 可机读要求

Ledger 必须支持机读：每条 Finding 含稳定 ID、字段类型固定、时间戳记录。**Phase 1 §16.4** 已要求 Evidence Pack 字段机读，本手册继承此约束。

---

## 8. Refactoring & Rollback（5 类 Refactor Type）

### 8.1 Refactor Type 矩阵

| Refactor Type | 典型动作 | 前置条件 | 执行证据 | 验证手段 | 回滚策略 |
|---|---|---|---|---|---|
| **Schema** | ALTER COLUMN / ADD/DROP COLUMN / 类型变更 | 业务规则 + 当前列使用情况 | ALTER 脚本 + 前后快照 | 列使用无回归 | ALTER 反向 |
| **Data** | 数据搬运、值转换、回填、UPDATE | 数据体检 + 备份 | 迁移脚本 + 备份哈希 | 抽样验证 + 全量 diff | **数据迁移通常不可逆**——必须备份+演练 |
| **Index** | CREATE / DROP / REBUILD INDEX | 真实查询路径 | 索引 DDL + 执行计划 | 执行计划对比 + 性能存档 | DROP 反向（无数据损失） |
| **Constraint** | ADD UNIQUE / FK / CHECK | 查重零重复 + 孤儿扫描 + 违规扫描 | 约束 DDL + 扫描结果 | 验证插入/更新路径 | DROP 约束（数据保留） |
| **Code / Entity** | 改 Entity / Repository / Service | 设计方案 + Review | Diff + 单元测试 + 集成测试 | 行为快照 + 考卷 | **代码 git revert** |

### 8.2 关键纪律：Data Rollback ≠ Code Rollback

| 维度 | Code Rollback | Data Rollback |
|---|---|---|
| 可逆性 | 通常完全可逆（git revert） | **经常不可逆**（数据已被迁移） |
| 时间窗口 | 任意 | 必须迁移前备份；备份保留期 = 业务保留期 |
| 验证手段 | 单测 / 集成测试 / 行为快照 | **抽样 + 全量 diff** |
| 回滚决策人 | AI 可自主 | 必须人工批准（R3+） |
| 灰度要求 | 通常不需要 | R5 必须灰度 |

**Data Rollback 的最低要求**：

1. 迁移前全量备份（带哈希）
2. 备份保留至 Verify 完成后 + 业务保留期
3. 抽样验证迁移前后一致率 ≥ 99.9%
4. 全量 diff 报告归档
5. 回滚脚本必须 dry-run 验证

### 8.3 Refactor Execution 流程

每个 Refactor Action 必走四步：

```
1. Precondition Check   → 前置条件满足？
2. Backup / Snapshot    → 备份或快照存在？
3. Execute              → 执行变更
4. Verify               → 立即验证
```

任一步失败 → 局部回滚 + 决策升级。

### 8.4 多步 Refactor 的顺序约束

当表涉及多类 Refactor 时，**推荐顺序**：

```
Schema (列定义) → Constraint (依赖 Schema) → Index (依赖 Schema) → Data (依赖 Schema/Constraint) → Code (最后)
```

任意顺序错误可能导致后续 Refactor 失败或回滚困难。

---

## 9. Batch Execution（批量策略）

### 9.1 核心原则

> **单表闭环 → Batch 聚合 → Batch Verification → 下一 Batch。**

### 9.2 禁止的反模式

- ❌ **先分析全部 N 张表，再统一实施** —— 范围蔓延 + 累积风险。
- ❌ 一次 Batch 包含超过 8 张表 —— 故障爆炸半径过大。
- ❌ Batch 内混合 R5 与 R0 风险等级 —— Gate 流程冲突。

### 9.3 Batch 构成规则

| 规则 | 说明 |
|---|---|
| 大小 | 建议 3–8 张表/批 |
| 风险同质 | 同一 Batch 内 Risk 等级应相近（≤ 2 档差异） |
| 模块聚集 | 同一 Batch 内表应同模块（或强业务依赖） |
| 依赖顺序 | 父表 → 子表顺序；外键依赖顺序 |
| 中止条件 | Batch 内任一表触发 Hard Gate（Master Spec §10.3）→ Batch 暂停 |

### 9.4 Batch Lifecycle

```
Batch Open
  → Table #1: Discover → Assess → Design → Approval → Refactor → Verify → CLOSED
  → Table #2: 同上
  → ...
  → Table #N: 同上
  → Batch Verification（整体验证：跨表约束、模块行为）
  → Batch Close
  → 下一 Batch Open
```

### 9.5 Batch 暂停与恢复

| 触发 | 行动 |
|---|---|
| Batch 内任一表 Hard Gate 触发 | Batch 暂停；后续表进入 HOLD 态 |
| Batch 内任一表 R5 失败 | Batch 回滚评估；Product+Architecture Gate 决策 |
| Batch Verification 发现跨表问题 | Batch 回退至最近安全点；新 Batch 重新设计 |

---

## 10. Failure / Rework（故障恢复）

### 10.1 故障类型与恢复

| 故障类型 | 检测时机 | 恢复策略 | 决策权 |
|---|---|---|---|
| **Test failure** | Verify 步骤 | 局部回退 Refactor；分析失败原因 | R1/R2 AI；R3+ 人工 |
| **Schema validation failure** | Verify 步骤 | ALTER 反向；重做 Refactor | R1/R2 AI；R3+ 人工 |
| **Migration failure** | Refactor 中 | 中止迁移；从备份恢复 | R3+ 人工 |
| **Contradictory evidence** | 任意步骤 | STOP → Decision Brief | R3+ 人工 |
| **Unexpected behavior** | Verify 或事后 | 回退 Refactor + 重新 Assess | R3+ 人工 |

### 10.2 关键纪律

> **优先局部回退，不得因为一张表失败就重新启动整个项目。**

局部回退范围：

- 同一表：Refactor 反向动作
- 同一 Batch：暂停后续表，已 CLOSED 表保留
- 跨 Batch：前序 Batch 已 CLOSED 表不回退；后续 Batch 重新设计

### 10.3 Re-trigger 流程

当表进入 CLOSED 状态后再次打开时（Re-trigger 触发条件见 Master Spec §13.5）：

```
CLOSED → DISCOVERED（重新走流程）
```

Re-trigger 必须记录 Re-trigger 原因（属"Deferred items"或"Re-trigger conditions"触发）。

---

## 11. TABLE CLOSED Gate

### 11.1 关闭的 5 必要条件

仅当以下 5 项全部满足时，表进入 CLOSED 状态：

| # | 条件 | 来源 |
|---|---|---|
| 1 | **Evidence sufficient** | Master Spec §11.3 Evidence Sufficiency Stop Rule 已达 |
| 2 | **Target design settled** | `[DESIGN]` 标签完整 |
| 3 | **Refactor completed OR no-change justified** | Refactor Ledger 完整 + no-change 理由显式 |
| 4 | **Verification passed** | Master Spec §13.2 13 条 DoD 全达成 |
| 5 | **No blocking finding** | P0/P1 = 0 |

### 11.2 关闭时必录的 6 项记录

| 记录项 | 说明 |
|---|---|
| **Before state** | Refactor 前的 DDL / Entity / 索引 / FK 快照 |
| **After state** | Refactor 后的 DDL / Entity / 索引 / FK 快照 |
| **Key evidence** | 七维 Finding + Evidence 清单（机读） |
| **Accepted constraints** | 已知但不处理的约束（如"未加 FK 因为跨模块耦合"——需 Architecture 裁决） |
| **Deferred items** | 明确推迟到未来 Re-trigger 的项 |
| **Re-trigger conditions** | 哪些条件触发时重新打开（如"行数 > 100 万"） |

### 11.3 No-change 是合法出口

当 5 条件全达成且 Refactor 步骤标注 no-change 时，TABLE CLOSED 合法（详见 Master Spec §13.4）。

**No-change 路径**：

```
DISCOVERED
  → Assess（七维全部 Conforms）
  → Refactor（标注 no-change）
  → Verify（核对 Conforms 现状）
  → CLOSED
```

### 11.4 不允许的关闭原因

- ❌ "未来还可以优化索引"——属 Re-trigger 条件，不是关闭阻碍
- ❌ "数据量太小暂不考虑"——属当前决策，需 `[DESIGN]` 标注为 N/A
- ❌ "暂时无业务影响"——属 `[GUESS]`，不构成关闭证据
- ❌ "等下一批统一处理"——属范围蔓延，违反单表闭环原则

---

## 12. 效率纪律（最高级约束）

### 12.1 五条效率铁律

| # | 铁律 | 落点 |
|---|---|---|
| 1 | **Evidence Sufficiency Stop Rule** — 证据达阈值立即停止取证 | §6.2 / Master Spec §11.3 |
| 2 | **Risk-adaptive flow** — 流程重量由 Risk 决定，不由表大小决定 | §4 |
| 3 | **No scope creep** — 当前表关闭时不允许"顺手优化其他表" | §11.4 |
| 4 | **No future optimization as block** — 未来优化不阻塞当前 TABLE CLOSED | Master Spec §13.3 |
| 5 | **Local rollback priority** — 故障优先局部回退 | §10.2 |

### 12.2 普通 P2/P3 不阻塞关闭

- **P2/P3 Finding**（非阻塞）应记录在"Deferred items"区，不阻止 TABLE CLOSED。
- **P0/P1**（阻塞）必须修复或转 Hard Gate。
- 详见 Master Spec §10.1 Risk 等级定义。

### 12.3 风险决定流程重量

不允许的流程膨胀：

- ❌ 一张 R0 No-change 表跑完整 5 步 SOP
- ❌ 一张 R1 Low-risk 表跑 Evidence-Driven Auto Gate
- ❌ 一张 R5 Destructive 表跳过 Product+Architecture Gate

不允许的流程压缩：

- ❌ 一张 R5 表跳过 Pilot Dry-run
- ❌ 一张 R4 表跳过跨表影响分析

### 12.4 流程终止条件

流程在以下条件**必须**终止当前 Step，进入下一态：

| Step | 终止条件 |
|---|---|
| Discover | 元数据齐全 |
| Assess | 七维全填毕 + Risk 判定 |
| Design | Finding → DESIGN 一一对应 |
| Refactor | 变更流水完整 + 中间验证通过 |
| Verify | 13 条 DoD 全达成 |
| Closed | 5 条件全满足 |

---

## 13. Phase 2 Exit Criteria

Phase 2 完成必须满足以下全部条件，方可进入 Phase 3。

### 13.1 操作手册完整性

- [ ] **正式生命周期（状态机）定义完整**（§2）
- [ ] **五步 SOP 每步六字段填毕**（§3）：Input / Action / Evidence / Output / Stop Condition / Escalation
- [ ] **Risk-Adaptive Flow Routing 明确定义**（§4）：R0/R1/R2/R3/R4/R5 各对应流程重量
- [ ] **Approval Gate Matrix 明确定义**（§5）：R0/R1 自主、R2 证据驱动自主、R3 人工批准、R4/R5 Product+Architecture Gate
- [ ] **Evidence Routing Matrix 明确定义**（§6）：A/B/C/D/E/F/G 七维各对应证据来源与禁止范围
- [ ] **Table Evidence Ledger 字段定义完整**（§7）：沿用 Master Spec 五标签
- [ ] **Refactoring & Rollback 五类定义完整**（§8）：Schema / Data / Index / Constraint / Code；Data Rollback ≠ Code Rollback
- [ ] **Batch Execution 规则定义完整**（§9）：单表闭环 + Batch 聚合 + Batch Verification
- [ ] **Failure / Rework 故障恢复策略定义完整**（§10）：5 类故障 + 局部回退优先
- [ ] **TABLE CLOSED Gate 5 必要条件 + 6 必录项定义完整**（§11）
- [ ] **效率纪律五条铁律定义完整**（§12）

### 13.2 与 Master Spec 无冲突

- [ ] 引用 Master Spec 处全部使用 `§X.Y` 引用，未复述 Master Spec 技术规则
- [ ] 状态机不与 Master Spec §13 DoD 冲突
- [ ] Approval Gate 不与 Master Spec §10 Risk 等级冲突
- [ ] Evidence Taxonomy 沿用 Master Spec §11，未创建第二套

### 13.3 Purity Gate 通过

- [ ] **No JNPF dependency** in the manual（grep 验证 §1–§14 无 JNPF 相关字眼）
- [ ] **No Foundry/BBB dependency** in the manual
- [ ] **No specific ORM dependency** in the manual
- [ ] **No specific database dialect dependency** in the manual
- [ ] **No specific field naming convention dependency** in the manual
- [ ] **No second technical rule** —— 未通过本手册引入 Master Spec 之外的技术规则

### 13.4 Phase 3 接口就绪

Phase 2 必须为 Phase 3 SKILL 提供以下接口（不写 SKILL，仅定义接口）：

- [ ] State Machine 接口（状态名 + 转移条件）
- [ ] Step 接口（每步六字段的结构化描述）
- [ ] Risk-Adaptive Flow Router 接口（Risk → Flow Weight）
- [ ] Approval Gate Interface（Gate 决策协议）
- [ ] Evidence Router 接口（Finding Type → Evidence Source）
- [ ] Ledger Schema（机读字段定义）
- [ ] Refactor Type Matrix 接口
- [ ] Batch Lifecycle 接口
- [ ] TABLE CLOSED Gate 判定协议

---

## 14. 文档保护声明

1. **严禁删除**本文档及后续冻结版本（含"合并/升级/冗余清理"等理由）。
2. **允许修改**；每次修改必须升版本号并在 §15 登记版本历史，旧版本保留存档。
3. 修改不得削弱 Risk-adaptive flow、扩大流程、引入特例依赖、降低 Gate 严格度。
4. **Backward Compatibility**：Phase 3/5/6 必须能跟随本文档的修改而适配；不允许 Phase 后续阶段在本手册外另设流程以"避免改 Phase 2"。

---

## 15. 版本历史

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 (draft) | 2026-08-29 | 首版 Execution Manual。Phase 1 冻结后的 Universal 操作手册。含 7 态生命周期 / 五步 SOP 六字段 / R0–R5 Risk-adaptive Flow / Approval Gate Matrix / Evidence Routing Matrix / Ledger 字段 / 5 类 Refactor & Rollback / Batch 规则 / Failure Rework / TABLE CLOSED Gate 5 条件 + 6 必录 / 效率五条铁律 / Phase 2 Exit Criteria。严格无 JNPF/Foundry/BBB/SqlSugar/EF Core/SQL Server 等字眼。 |

---

## 附录 A — State Machine 转移矩阵

| From | To | 触发动作 | 守门条件 | 失败回退 |
|---|---|---|---|---|
| — | DISCOVERED | Discovery 步骤 | 元数据合法 | — |
| DISCOVERED | ASSESSED | Assess 完成 | 七维全填毕 + Risk 判定 | 回退 DISCOVERED |
| ASSESSED | DESIGNED | Design 完成 | Finding → DESIGN 对齐 | 回退 ASSESSED |
| DESIGNED | READY | Approval Gate 通过 | 风险等级决策完成 | 回退 DESIGNED/ASSESSED |
| READY | REFACTORED | Refactor 启动 | Batch 调度执行 | — |
| REFACTORED | VERIFIED | Verify 完成 | 13 条 DoD 全达成 | 回退 REFACTORED |
| VERIFIED | CLOSED | Closed Gate 通过 | 5 条件全满足 | 回退 VERIFIED |
| 任意非 CLOSED | DISCOVERED | Hard Gate 触发 | Decision Brief 未拍板 | — |
| CLOSED | DISCOVERED | Re-trigger 触发 | Master Spec §13.5 条件 | — |

---

## 附录 B — Risk-Adaptive Flow Routing Table

| Risk | 流程重量 | Step 路径 | Approval Gate | 必产 |
|---|---|---|---|---|
| R0 | Lightweight | Discover → Assess → CLOSED | Auto-Close | Concludes 一句 |
| R1 | Lightweight | Discover → Assess → Refactor → Verify → CLOSED | Auto-Apply | Refactor 流水 + Verify |
| R2 | Standard | 五步全跑 | Evidence-Driven Auto | 流水 + Verify + 证据 |
| R3 | Heavy | 五步 + Approval Gate | Human Approval | Decision Brief + 体检 + 回滚 |
| R4 | Heavy+ | 五步 + Product+Architecture Gate | Cross-Table | 上述 + 跨表分析 |
| R5 | Heaviest | 五步 + Gate + Pilot + 灰度 | Destructive | 上述 + Pilot + 灰度方案 |

---

## 附录 C — Evidence Routing Matrix（完整版）

| Finding Type | 证据来源（按优先级） | 严禁范围 |
|---|---|---|
| **A Schema** | DDL + Entity + 一条真实读写路径 | 全项目 Service 扫描 |
| **B Integrity** | PK/FK/Constraint 元数据 + 业务规则 | 全项目 SQL 历史 |
| **C Index** | 一条真实查询 + 列分布 + 索引统计 | 整个项目性能日志 |
| **D Lifecycle** | Tenant/Soft-Delete/Audit 代码路径 + 业务规则 | 全部业务文档 |
| **E CRUD/Query** | Service/Repository 代码 + 慢查询 | 整个 Service 层 |
| **F DDD** | 业务规则 + 一致性边界 + Entity 关系 | 全项目领域模型 |
| **G Readiness** | Target Profile 契约摘要 + Marker Concept 列表 | Target Profile 全文 |

---

## 附录 D — Refactor Type Matrix

| Type | 典型动作 | 前置 | 证据 | 验证 | 回滚 |
|---|---|---|---|---|---|
| **Schema** | ALTER COLUMN | 业务规则 + 列使用情况 | 脚本 + 前后快照 | 列使用无回归 | ALTER 反向 |
| **Data** | 搬运/转换/回填/UPDATE | 体检 + 备份 | 脚本 + 备份哈希 | 抽样 + diff | **不可逆——备份+演练** |
| **Index** | CREATE/DROP/REBUILD | 真实查询路径 | DDL + 执行计划 | 计划对比 + 存档 | DROP 反向 |
| **Constraint** | ADD UNIQUE/FK/CHECK | 查重 + 孤儿 + 违规扫描 | DDL + 扫描结果 | 验证写路径 | DROP 约束 |
| **Code/Entity** | 改 Entity/Repository/Service | 设计 + Review | Diff + 测试 | 行为快照 + 考卷 | git revert |

---

## 附录 E — Batch Rules

| 规则 | 说明 |
|---|---|
| Batch 大小 | 3–8 张表 |
| 风险同质 | 同一 Batch 内 Risk 等级 ≤ 2 档差异 |
| 模块聚集 | 同一 Batch 内表应同模块 |
| 依赖顺序 | 父表 → 子表；外键依赖顺序 |
| 中止条件 | 任一 Hard Gate → Batch 暂停 |
| Verification | Batch 完成后整体验证跨表约束与模块行为 |

---

## 附录 F — Failure Recovery Matrix

| 故障 | 检测时机 | 恢复策略 | 决策权 |
|---|---|---|---|
| Test failure | Verify | 局部回退 + 原因分析 | R1/R2 AI；R3+ 人工 |
| Schema validation | Verify | ALTER 反向 + 重做 | R1/R2 AI；R3+ 人工 |
| Migration failure | Refactor 中 | 中止 + 备份恢复 | R3+ 人工 |
| Contradictory evidence | 任意 | STOP → Decision Brief | R3+ 人工 |
| Unexpected behavior | Verify / 事后 | 回退 Refactor + 重 Assess | R3+ 人工 |

---

## 附录 G — TABLE CLOSED 必录项

| 记录 | 说明 |
|---|---|
| Before state | DDL/Entity/索引/FK 快照 |
| After state | 同上，Refactor 后 |
| Key evidence | 七维 Finding + Evidence（机读） |
| Accepted constraints | 已知不处理的约束（含裁决） |
| Deferred items | 推迟到未来 Re-trigger 的项 |
| Re-trigger conditions | 重新打开的触发条件 |

---

## 附录 H — Phase 3 SKILL 接口契约

Phase 3 必须按以下接口实现 SKILL.md，本手册 §13.4 已锁定：

| 接口 | 字段 |
|---|---|
| State Machine | 7 态名 + 转移条件 |
| Step | Input/Action/Evidence/Output/Stop/Escalation |
| Risk-Adaptive Router | Risk → Flow Weight 映射 |
| Approval Gate | Gate 决策协议（含产物） |
| Evidence Router | Finding Type → Evidence Source |
| Ledger Schema | 机读字段 |
| Refactor Type Matrix | 5 类 × 5 字段 |
| Batch Lifecycle | Open/Verify/Close 协议 |
| TABLE CLOSED Gate | 5 条件判定协议 |
