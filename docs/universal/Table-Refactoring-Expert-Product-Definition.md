# Table Refactoring Expert — Product Definition v1.0

**Phase**: 0 — Product Definition
**Status**: Draft → 用户审批后冻结
**Date**: 2026-08-29
**Nature**: 产品定义文档。定义 Universal Table Refactoring Expert（以下简称 TRE）的产品定位、能力边界、质量模型、KPI、风险模型、阶段路线与退出条件。
**Not in scope of this document**: 任何 JNPF / Foundry / BBB 的实现细节、字段命名、ORM quirks。这些进入 Phase 5 (JNPF Extension) 与 Phase 5 配套的 Target Profile。
**后续阶段产物**（本 Phase 0 仅定义边界，不预先承诺内容）: Phase 1 Master Spec · Phase 2 Execution Manual · Phase 3 Universal SKILL · Phase 4 Generic Validation · Phase 5 Extension/Target Profile · Phase 6 Pilot · Phase 7 v1.0 Freeze · Phase 8 Mass Refactoring。

---

## 0. 本文档的约束力

冻结后：

1. 一切 TRE 能力边界、风险等级、KPI 阈值、阶段退出条件以本文档为准；偏离必须先改本文档并经用户审批。
2. 严禁把 JNPF / Foundry / 任何特定 Target System 的字段命名、ORM 行为、数据库方言知识写进 Universal Core。
3. 严禁在 Pilot 之前给 Productivity KPI 设定具体时间/数量数字；先建 baseline 再设 v1.0 target。
4. 严禁"为了更确定"无限取证；达到当前决策所需最低证据阈值后立即停止。
5. Phase Exit Criteria 未达成 → 不得进入下一阶段。

---

## 1. Universal Skill 产品定位

### 1.1 一句话定义

> TRE 是面向**任意关系型数据库**（不限方言）任意 ORM/数据访问层的**单表级专家作业产品**。它把"识别→评估→设计→重构→验证→关闭"的过程系统化、证据化、可度量、可产品化，使一张数据库表从"可运行"演进到"语义清晰、数据可信、CRUD 可控、生命周期可治理、对接目标数据基础设施就绪"。

### 1.2 服务对象（按优先级）

```
首个 Consumer（任何低代码/企业级 .NET / Java / Go / Node 后端）
 → 跨行业通用：ERP / CRM / SaaS / MES / OA / Workflow
 → 任何含持久层的应用：Web API / Console / Worker / Microservice
```

### 1.3 角色边界（不是开发框架，不是数据库迁移工具）

TRE 是**专家级作业法与 AI 操作大脑**，不是：

- 不是 ORM/Repository 框架（Foundry/EFCore/SqlSugar/Dapper 是它的潜在 Target System）
- 不是数据库迁移/DDL Diff 工具（Flyway/Liquibase 是它的下游消费者）
- 不是 Schema 设计工具（如同 EF Core Migration CodeFirst）
- 不是性能调优工具（如 SQL Sentry / pg_stat_statements）
- 不是业务领域建模工具（如 EventStorming）

### 1.4 明确不追求

- 不追求"覆盖所有数据库方言"——方言知识属于 Extension/Target Profile。
- 不追求"零证据决策"——无证据 = STOP。
- 不追求"全自动重构"——任何 L1-R3 以上风险必须人工批准。
- 不追求"用 changed lines / index 数量 / finding 数量衡量产出"。
- 不为数量而支持——不"为了完整列表"生造维度。

---

## 2. Capability Boundary

### 2.1 Universal Capability Model（七维 A–G）

每张表至少按以下七维判断。**G 不得写死为特定 Target System**——Target Readiness 由独立 Target Profile 定义。

| 维 | 名称 | 核心问题 | 触发决策 |
|---|---|---|---|
| **A** | Schema Quality | 字段类型/Nullability/默认值/PK 是否与语义一致？ | 类型修正、约束补齐 |
| **B** | Data Integrity | 唯一性/外键/CHECK/级联/孤儿/删除行为是否完备？ | UNIQUE/FK/CHECK/Cascade |
| **C** | Index Engineering | 真实查询（含 Where/OrderBy/Join/Tenant）有无合理索引？ | 索引增删改 |
| **D** | Data Lifecycle | 表的增长/归档/冷热分离/保留策略是否清晰？ | 归档/分区/清理 |
| **E** | CRUD / Query Usage | Service/Repository 的 Create/Read/Update/Delete/Query/Projection/Pagination/Include 真实用法是否健康？ | N+1、投影、批量、分页、异步 |
| **F** | DDD / Aggregate Boundary | 表是 Aggregate Root / Child / Reference Data / Global Data？持久化边界是否正确？ | 聚合归属与边界 |
| **G** | Consumer / Target Readiness | 表的持久化语义能否被目标数据基础设施（由 Target Profile 定义）干净承接？ | 承接度评级与 gap 清单 |

### 2.2 Capability Boundary 三大硬性原则

1. **Universal Core 不得含任何 Target System 的字段命名/API/ORM 特性**——抽象到 "marker concept" 层（如 `Auditable Concept` 而非 `CreatedBy`）。
2. **No Backward Contamination**——JNPF/Foundry 侧发现的具体问题不自动反哺 Universal Core，只能沉淀为 Target Profile 的已知 gap。
3. **No Capability Without Evidence**——每条 Capability 触发必须可追溯到一张事实卡的某一项观测信号。

### 2.3 Capability 不在范围

下列能力**不属于 TRE**，明确排除以防范围蔓延：

- 完整 Service/Class 重构（属类级专项）
- 业务领域设计（属领域专家）
- 授权/权限策略
- 工作流/审批编排
- 微服务拆分
- UI 改造

**发现上述范畴依赖时**：登记依赖到 Evidence Pack 的"cross-scope dependency"，不替代实施。

---

## 3. Quality Model

### 3.1 质量四象限

| 维度 | 定义 | 测量手段 |
|---|---|---|
| **Correctness** | 表的行为、约束、查询结果与既有契约一致；无未解释的异常 | Characterization 测试 + 行为快照 |
| **Evidence** | 结论可追溯到至少一项 [KNOWN]/[COMPUTED] 级别证据 | Evidence Pack 完备率 |
| **Stability** | 真实查询路径不被破坏；P95/P99 退化在阈值内 | 性能对比协议（同 DB / 同数据量 / 同查询） |
| **Readability** | 表结构、字段命名、注释、聚合边界对后续开发者可理解 | Code Review + 文档评审 |

### 3.2 质量纪律（硬规则）

- Quality ≠ Amount of Change。禁止把 changed lines / 索引数量 / 修复条数作主 KPI。
- No-change-is-valid。结论为"本表不需要任何变更"是合法的、审计过的、明确记录的 TABLE CLOSED 状态。
- 每个变更都必须有证据 → 决策 → 最小方案 → 风险评估 → 验证 → 收益存档 → 回归的完整链路；缺一即判定未达质量。

### 3.3 质量审计对象

- Universal SKILL（Phase 3）
- Execution Manual（Phase 2）
- Generic Validation 案例（Phase 4）
- Pilot 表（Phase 6）
- Extension 文档（Phase 5）

---

## 4. Performance KPI

KPI 分为"Universal 自始就有"与"Pilot 后建 baseline 再设 target"两类。**任何 Productivity KPI 的具体数字 v1.0 不预设，先建 baseline**。

### 4.1 Quality（v1.0 即设定）

| 指标 | v1.0 Target | 来源 |
|---|---|---|
| P0/P1 unresolved | **0**（绝对值） | L1-R5 风险零容忍 |
| Data integrity regression | **0** | 改造前后 Characterization 全绿 |
| Unexpected behavior regression | **0** | 快照比对 + 考卷 |
| Evidence completeness | **≥ 95%**（每表 Evidence Pack 字段填毕率） | 自检 |
| Core dimension evidence missing | **0**（A–G 七维每维至少一条证据或显式 N/A 标注） | 自检 |

### 4.2 Precision（Pilot 期统计）

| 指标 | v1.0 Target（上限） | 说明 |
|---|---|---|
| False Positive Rate | **≤ 10%** | 标识出的问题中真问题占比 |
| False Negative / Maturity | **≤ 5%** | 真问题中漏标占比 |
| Human Gate Rate | **≤ 20%** | 触发人工决策卡的比例 |

### 4.3 Efficiency（Pilot 期统计）

| 指标 | v1.0 Target（上限） | 说明 |
|---|---|---|
| Rework Rate | **≤ 10%** | 关闭后再次打开率 |
| Autonomous Resolution Rate | **≥ 80%** | L1-R0/R1/R2 闭环率 |

### 4.4 Productivity（Pilot 后建 baseline，**v1.0 不预设数字**）

下列指标**仅在 Pilot 完成后采集真实数据**，v1.0 Freeze 时填入第一版 target；不得在 Pilot 前拍数字：

- **Tables Closed / AI Engineer Hour**（工程产出率）
- **Median Table Completion Time**（中位数）
- **P90 Table Completion Time**（尾部耗时）
- **Engineering Yield**：每投入 1 小时 AI 工作关闭的高质量 Table Unit 数（控制无限取证的反向指标，越高越好但有上限，过低说明在无意义劳动，过高说明在敷衍）

### 4.5 指标控制约束

- v1.0 Freeze 前必须能展示所有 Quality 指标 + Pilot 期 Precision/Efficiency 实测数据。
- v1.0 Freeze 前 Productivity 指标可以只填"baseline 待采集"，禁止填拍脑袋的数字。

---

## 5. Risk Model

### 5.1 风险等级（L1-R0 到 L1-R5，共六档）

| 等级 | 名称 | 定义 | 典型动作 |
|---|---|---|---|
| **L1-R0** | No change | 评估结论是不变更 | AI 自主；记录决策理由即关闭 |
| **L1-R1** | Low risk | 单点、低影响、可立即回滚（如低选择性索引清理、注释补全） | AI 自主；Evidence Pack + 收益存档 |
| **L1-R2** | Structural change | 表内结构变更（列类型/Null/默认值/索引增删改）但不改语义 | Evidence-driven 执行；Characterization 全绿即可 |
| **L1-R3** | Data / semantic migration | 涉及数据搬运、值转换、新约束生效 | **人类批准**（PR 评审 + 数据体检报告 + 回滚脚本） |
| **L1-R4** | Cross-table / aggregate change | 跨表结构、聚合边界、级联策略变更 | **Product + Architecture Decision Gate** |
| **L1-R5** | Destructive / production-impact | 不可逆、影响生产数据、跨环境行为变更 | **Product + Architecture Decision Gate** + Pilot Dry-run + 灰度 |

### 5.2 风险判定纪律

- 风险等级必须写在 Evidence Pack 的每条 Finding 上。
- L1-R3 及以上：必须有 Decision Brief（Input/Options/Risks/Recommendation）。
- L1-R4 及以上：禁止 AI 自主推进，必须人工拍板。

### 5.3 风险升级触发（STOP → Decision Brief）

遇到以下任意一条，AI **必须**停下取证、产出 Decision Brief、等人类决策：

1. PK 语义不明（多列 PK? 自增 vs GUID vs 雪花? 业务键 vs 代理键?）
2. FK 含义不明（孤儿数据？CASCADE 缺失但代码手动删？逻辑外键 vs 物理外键？）
3. 破坏性迁移风险（数据丢失/截断/不可回滚）
4. 数据类型转换风险（语义损失、精度丢失、字符集变化）
5. Nullability 语义冲突（业务上"未知"应保留 NULL vs "漏填"应 NOT NULL + DEFAULT）
6. Tenant ownership 不明（多租户字段位置、过滤点、租户归属不清）
7. Aggregate boundary 不明（无法判定聚合根、边界含子实体/引用数据关系不清）
8. 跨表改造需求（变更必须穿透边界）
9. 未解释的 legacy behavior（历史遗留行为找不到设计意图，删除前需定性）
10. 目标 Contract 不兼容（Target Profile 的契约与表当前语义冲突）

---

## 6. Phase Roadmap

### 6.1 阶段总览（严格串行，禁止跳过）

```
Phase 0  Product Definition (本文档)
   ↓
Phase 1  Universal Master Spec
   ↓
Phase 2  Universal Execution Manual
   ↓
Phase 3  Universal SKILL
   ↓
Phase 4  Generic Validation (3-5 non-JNPF 案例)
   ↓
Phase 5  JNPF Extension + Foundry Target Profile (第一个 Consumer / Profile 示例)
   ↓
Phase 6  JNPF Pilot (3-5 真实表)
   ↓
Phase 7  v1.0 Freeze
   ↓
Phase 8  Mass Table Refactoring
```

### 6.2 阶段原则

- **Core First**：Universal Core（Phases 1–4）必须完全不依赖任何 Consumer/Target System 词汇。
- **Extension Second**：Consumer/Target 知识（Phase 5）晚于 Universal Core 完成。
- **Validation Before Extension**：通用能力必须在脱离 JNPF 词汇的 3–5 案例上跑通，才能进入 Extension 阶段。
- **Pilot Before Freeze**：3–5 张真实表跑完 + KPI 实测，方可 Freeze。
- **Freeze Before Mass**：未 Freeze 不得批量重构。

---

## 7. Phase Exit Criteria

每阶段必须定义：Input / Output / KPI / Exit Criteria / Maximum Scope / Next-stage Trigger。

### Phase 0 — Product Definition（本文档）

| 项 | 内容 |
|---|---|
| **Input** | 用户产品化指令 |
| **Output** | `Table-Refactoring-Expert-Product-Definition.md`（10 节） |
| **KPI** | 文档完整性 = 10 节全部实质内容；无 placeholder |
| **Exit Criteria** | 用户书面批准 + 状态改为"Frozen v1.0" |
| **Maximum Scope** | 仅产品定义，不写任何 Master Spec / SKILL / 代码 |
| **Next-stage Trigger** | Phase 1 启动 |

### Phase 1 — Universal Master Spec

| 项 | 内容 |
|---|---|
| **Input** | Phase 0 冻结版本 |
| **Output** | `Table-Refactoring-Expert-Master-Spec.md` |
| **KPI** | 涵盖 5.3 全部 Hard Gate、A–G 七维技术深度、Evidence 格式定义、Risk 等级判定规则 |
| **Exit Criteria** | Self-review 无 placeholder/contradiction/ambiguity + 用户批准 |
| **Maximum Scope** | 不引用任何 Target System；不写 SOP 步骤（步骤在 Phase 2） |
| **Next-stage Trigger** | Phase 2 启动 |

### Phase 2 — Universal Execution Manual

| 项 | 内容 |
|---|---|
| **Input** | Phase 1 冻结版本 |
| **Output** | `Table-Refactoring-Expert-Execution-Manual.md` |
| **KPI** | SOP 六步（Discover→Assess→Design→Refactor→Verify→TABLE CLOSED）完整、批准门禁、批量规则、回滚规则、证据停止规则全部落地 |
| **Exit Criteria** | Self-review + 用户批准 |
| **Maximum Scope** | 不重复 Master Spec 的技术深度，仅写"什么时候做什么、按什么顺序、找谁批" |
| **Next-stage Trigger** | Phase 3 启动 |

### Phase 3 — Universal SKILL

| 项 | 内容 |
|---|---|
| **Input** | Phase 1 + Phase 2 冻结版本 |
| **Output** | `.claude/skills/table-refactor-expert/SKILL.md` + references/ |
| **KPI** | AI 操作大脑：work sequence / document routing / FACT-INFERENCE-DESIGN 纪律 / hard gate 检测 / decision escalation / Golden Example 查找 / closure；**不得重复技术事实** |
| **Exit Criteria** | Self-review + 用户批准 + 不含 JNPF/Foundry 字眼（如需举例，必须抽象到"示例 Target System X"层级） |
| **Maximum Scope** | 仅操作层；不替代 Master Spec 的权威定义 |
| **Next-stage Trigger** | Phase 4 启动 |

### Phase 4 — Generic Validation

| 项 | 内容 |
|---|---|
| **Input** | Phase 3 Universal SKILL |
| **Output** | 3–5 个**非 JNPF 词汇**的案例 + Validation Report |
| **KPI** | 5 类必跑案例至少各 1：tenant SaaS table / FK-heavy / soft-delete-audit / aggregate root + child / query-index-heavy；Universal SKILL 不依赖 JNPF 假设 |
| **Exit Criteria** | 全部案例 PASS + Validation Report 通过用户审阅 |
| **Maximum Scope** | 案例必须使用通用 SaaS/CRM/ERP 风格实体，**严禁 JNPF 词汇** |
| **Next-stage Trigger** | Phase 5 启动 |

### Phase 5 — JNPF Extension + Foundry Target Profile

| 项 | 内容 |
|---|---|
| **Input** | Phase 4 通过的 Universal SKILL + 已冻结 Master/Manual |
| **Output** | `JNPF-Table-Refactoring-Extension-Spec.md` + `JNPF-Foundry-Readiness-Profile.md` |
| **KPI** | JNPF 命名约定 / Entity 基类 / ORM 行为 / 历史约束全部进 Extension；Foundry 契约字段映射进 Target Profile；**不反向污染 Universal Core** |
| **Exit Criteria** | Self-review + 用户批准 + Universal Core diff = 0 |
| **Maximum Scope** | Extension 与 Profile 是 Universal 的"插头"，不是 Universal 的修改 |
| **Next-stage Trigger** | Phase 6 启动 |

### Phase 6 — JNPF Pilot

| 项 | 内容 |
|---|---|
| **Input** | Phase 5 Extension + Profile |
| **Output** | 3–5 张真实 JNPF 表的完整 Evidence Pack + KPI 实测数据 |
| **KPI** | 覆盖 5 类代表表（simple tenant CRUD / FK-heavy / soft-delete/audit / aggregate root / query-index-heavy）；Quality 指标 100% 达成；Precision/Efficiency 实测可填 |
| **Exit Criteria** | Pilot KPI 报告通过用户审阅；发现的 Skill/Manual/Extension 问题列入 P7 修正项 |
| **Maximum Scope** | 仅 3–5 张表，**严禁扩大为"audit 全部 289 表"** |
| **Next-stage Trigger** | Phase 7 启动 |

### Phase 7 — v1.0 Freeze

| 项 | 内容 |
|---|---|
| **Input** | Pilot 实测 KPI + 待修正项列表 |
| **Output** | Universal SKILL v1.0 + Master Spec v1.0 + Manual v1.0 + Extension v1.0 + Target Profile v1.0 + Productivity KPI 第一版 target（基于 Pilot baseline） |
| **KPI** | 全部 Quality 指标达成；Productivity baseline → target 转换有据可循；Productivity target 不拍脑袋 |
| **Exit Criteria** | v1.0 Freeze 包通过用户书面批准 |
| **Maximum Scope** | 仅修正与冻结；不开新能力 |
| **Next-stage Trigger** | Phase 8 启动 |

### Phase 8 — Mass Table Refactoring

| 项 | 内容 |
|---|---|
| **Input** | v1.0 Frozen 包 |
| **Output** | 批量重构产出（每表 TABLE CLOSED） |
| **KPI** | 按 v1.0 Quality/Precision/Efficiency 指标监控；Productivity 持续追踪 |
| **Exit Criteria** | 全部目标表完成 + 全部 Quality 指标达成 + Productivity 未显著退化 |
| **Maximum Scope** | 由 Master Spec 的批量规则决定（不在本 Phase 0 文档内预设数字） |
| **Next-stage Trigger** | 无（产品常态化运营） |

---

## 8. Universal Core vs Extension Boundary

### 8.1 边界铁律

**Universal Core 必须包含**：表级抽象（七维 A–G）、Evidence 格式（FACT/INFERENCE/DESIGN + [KNOWN]/[COMPUTED]/[INFERRED]/[GUESS] + 新增 [DESIGN]）、Risk 模型（L1-R0..R5）、Hard Gate 检测规则、Quality/KPI 框架、Phase Exit Criteria、TABLE CLOSED 定义、Golden Example 模式（不含具体表）。

**Universal Core 不得包含**：任何 Target System 的字段名/类型/命名约定/ORM 行为/数据库方言/历史遗留行为。

**Extension 必须包含**：Consumer 特定的表命名、字段映射、Entity 基类、ORM quirks、方言差异、Legacy 行为解释、跨 Consumer 不通用的业务约定。

**Target Profile 必须包含**：目标数据基础设施的契约字段映射（如"该 Consumer 的 `F_CREATOR_USER_ID` 列映射到 Target System 的 Auditable Concept.CreatedBy"）、承接度评级方法、已知 gap。

### 8.2 反向污染防御

- Pilot（Phase 6）发现的 JNPF 特定问题，**优先**沉淀到 JNPF Extension；只有当该问题证明属于"Universal 通用抽象遗漏"时，才允许修改 Universal Core，且必须附"至少两个不同 Consumer 出现同一抽象缺口"的证据。
- 严禁"Foundry 这个契约字段映射有错"导致 Universal 的 Auditable Concept 重定义——Auditable Concept 是抽象的，错误是 Target Profile 层的。

### 8.3 No Backward Contamination 检查

每份 Extension/Profile 发布前必须回答：

1. 本文档含 Universal Core 未定义的术语吗？如含，候选迁移到 Core 或显式标注为 Consumer-specific。
2. 本文档与已冻结的 Universal SKILL 描述的相同概念存在命名冲突吗？如有，必须以 Universal 为准，Extension 改写。
3. 本文档的 KPI 是否在试图覆盖 Universal KPI 之外的维度？如是，必须提升到 Universal KPI 框架内讨论。

---

## 9. TABLE CLOSED Definition

### 9.1 状态语义

**TABLE CLOSED = 一张表的本次重构生命周期结束，进入"持续维护 + 后续 Re-trigger 条件"状态**。

### 9.2 关闭判定（13 条 DoD，全为必要条件）

| # | 条件 | 说明 |
|---|---|---|
| 1 | Schema understood | A 维度：字段、类型、Nullability、默认值已记录 |
| 2 | Integrity validated | B 维度：唯一/外键/CHECK/级联/孤儿已记录 |
| 3 | Index justified by real query | C 维度：每个索引能追溯到一条真实查询路径或显式记录"暂未发现消费方" |
| 4 | Lifecycle semantics defined | D 维度：增长/归档/冷热分离/保留策略已记录 |
| 5 | CRUD / query usage mapped | E 维度：真实 Service/Repository 用法已记录 |
| 6 | DDD boundary classified | F 维度：聚合根/子实体/引用数据/全局数据已分类 |
| 7 | Tenant / SoftDelete / Audit classified | G-1：三个概念的承接度已评级 |
| 8 | Target readiness classified | G-2：Target Profile 的承接度已评级 |
| 9 | Target design defined | DESIGN 标签下的目标态已记录 |
| 10 | Change implemented OR No-change justified | 实际变更 OR 显式 No-change 决策理由 |
| 11 | Verification passed | 表的 CRUD 接口快照比对一致 + 考卷全绿 + 性能动作附 §KPI 存档号 |
| 12 | No unresolved blocking finding | P0/P1 = 0；未决议项必须登记到 evidence 的"跨阶段遗留"区 |
| 13 | No unexplained behavior | 关闭前所有"未知 legacy behavior"必须有定性（保留 / 移除 / 重定义） |

### 9.3 合法 No-change 出口

当 13 条 DoD 全达成但结论为"无需任何变更"时，TABLE CLOSED 状态合法。理由必须显式记录（"已知晓的 legacy behavior 已定性 + 现有结构与 Target Profile 契约无冲突 + 当前无任何健康指标异常"）。

### 9.4 关闭后 Re-trigger 条件

TABLE CLOSED 不等于永远不再打开。Re-trigger 触发：

- 数据量、查询模式、写入压力达到原设计假设的偏离阈值
- Target Profile 契约升级
- 跨表聚合边界重新设计
- Schema/Entity 实测漂移被检出

---

## 10. V1.0 Productization Exit Criteria

**V1.0 = Phase 7 冻结包**。V1.0 冻结意味着 Universal Core 与第一个 Consumer Extension + Profile 全部就绪、可独立运行、可度量、可被后续 Consumer 复用。

### 10.1 V1.0 必须达成的清单（全必要）

1. ✅ Phase 0–7 全部 Exit Criteria 达成
2. ✅ Phase 4 Generic Validation PASS（3–5 个非 JNPF 词汇案例全过）
3. ✅ Phase 6 Pilot KPI 全达成（Quality 100%；Precision/Efficiency 实测填入）
4. ✅ Pilot Productivity baseline 采集完成，V1.0 第一版 target 有数据依据（**非拍脑袋**）
5. ✅ Universal Core 自我一致性自检通过（无 placeholder/contradiction/ambiguity/scope creep）
6. ✅ No Backward Contamination 检查通过
7. ✅ SKILL.md 不含 JNPF/Foundry/任何 Target System 字眼（举例时使用抽象"示例 Target System X"）
8. ✅ 至少一个 Golden Example 录入 references/
9. ✅ 风险等级判定可机读（如 Risk 等级字段有判定标准，AI 可机械判定）
10. ✅ Evidence Pack 模板可机读（CSV/JSON Schema 任一即可）

### 10.2 V1.0 不承诺（避免范围蔓延）

- 不承诺所有 ORM/数据库方言支持——首版仅 JNPF Extension 一个 Consumer + Foundry 一个 Target Profile。
- 不承诺 Performance 调优通用化——属其他产品线。
- 不承诺自动生成 migration DDL——属 Foundry/Diff 引擎等下游产品。
- 不承诺跨库迁移——方言层能力由 Target Profile 持有。

### 10.3 V1.0 失败/重做条件

- Generic Validation 任一案例 FAIL
- Pilot KPI 任一 Quality 指标未达成
- Universal Core 被发现反含 JNPF/Foundry 知识
- Productivity baseline 无法采集（即 Pilot 没产出可度量数据）
- 任何 Hard Gate 在 Pilot 中未被触发过（说明覆盖度不足）

---

## 附录 A — Evidence 标签规范（Phase 1 详细化，此处仅定原则）

### A.1 来源标签

| 标签 | 含义 | 示例 |
|---|---|---|
| `[KNOWN]` | 可被 DDL/Entity/代码直接读取的事实 | 列名、类型、PK |
| `[COMPUTED]` | 由已知事实计算或推导 | 索引选择性 = distinct_values/total |
| `[INFERRED]` | 由多源证据推断的合理结论，但需在 [GUESS] 升格前显式标记 | "该表无读消费方" 来自"全仓 grep 零结果" |
| `[GUESS]` | 假设性结论，未证实 | "运行时可能产生该问题" |
| `[DESIGN]` | **目标态**，不是现有事实 | "计划将 `bool IsDeleted` 通过适配器映射为 `int? DeleteMark`" |

### A.2 纪律

- `[DESIGN]` 不得与 `[KNOWN]/[COMPUTED]` 混用——证据是当前事实，DESIGN 是目标状态。
- 每条 Finding 必须有至少一条 `[KNOWN]` 或 `[COMPUTED]` 证据支撑。
- `[GUESS]` 不允许出现在决策结论中；只允许出现在"待进一步取证"的临时标注。

### A.3 证据停止规则

达到当前决策所需最低证据阈值后立即停止取证。判定标准：

- 字段语义判断：DDL + Entity + 一条真实读写路径 = 足够
- 索引设计：一条真实查询（含 Where/OrderBy/Join/Tenant） + 列分布 = 足够
- 风险等级判定：影响面 + 回滚成本 + 数据风险 = 足够
- 严禁"为了更确定"无限搜索

---

## 附录 B — 文档保护声明

1. 严禁删除本文档及后续冻结版本（含"合并/升级/冗余清理"等理由）。
2. 允许修改；每次修改必须升版本号并在 §0 登记版本历史，旧版本保留存档。
3. 修改不得削弱 Hard Gate、降低 Risk 模型严格度、扩大 Universal Core 范围。

---

## 版本历史

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 (draft) | 2026-08-29 | 首版产品定义 |
