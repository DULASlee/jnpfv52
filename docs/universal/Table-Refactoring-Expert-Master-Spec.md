# Table Refactoring Expert — Universal Master Specification v1.0

**Phase**: 1 — Universal Master Specification
**Status**: Draft → 用户审批后冻结
**Date**: 2026-08-29
**Upstream**: `Table-Refactoring-Expert-Product-Definition.md` v1.0 (Phase 0, FROZEN)
**Downstream**: Phase 2 Execution Manual · Phase 3 Universal SKILL
**Not in scope of this document**: SOP 步骤、AI 执行细节、JNPF/Foundry/BBB/任何 Target System 的字段命名/ORM 行为/方言特性、历史遗留行为。这些分属 Phase 2、Phase 3、Phase 5。

---

## 0. 文档约束力

冻结后：

1. Universal Table Refactoring Expert（以下简称 TRE）的全部技术判定准则以本文档为**唯一权威源**。
2. 任何规则入本文档前必须通过 **Universal Core Purity Gate**（见 §15）。判定为 C（特定项目特例）的规则**禁止入本文档**，必须进入 Extension 或 Target Profile。
3. 本文档**不写** SOP 步骤、不写 AI 执行指令、不写 ORM/数据库方言/具体命名约定。
4. 修改必须升版本号并登记 §17 版本历史，旧版本保留存档。
5. 修改不得削弱 Hard Gate、降低 Risk 严格度、扩大 Capability 范围、引入特例依赖。

---

## 1. 核心原则

### 1.1 唯一优化目标

> **Optimize for correctness, evidence sufficiency, risk reduction, and throughput.**
> **Do NOT optimize for amount of change.**

具体含义：

- **不为产生 diff 而重构** — No-change 是合法 TABLE CLOSED 出口。
- **不为减少/增加索引数量而重构** — 索引的存在与否由真实查询路径决定。
- **不为减少代码行数而重构** — 行数与质量无相关关系。
- **不为"看起来更先进"而重构** — Universal 规则不奖励复杂方案。

### 1.2 五大闸门（统一模型）

| 闸门 | 防止的失败模式 | 落点章节 |
|---|---|---|
| **Capability Scope** | "不能少" — 必备能力缺失导致评估盲区 | §2 + §3–§9 |
| **Evidence Sufficiency** | "不能无限查" — 为更确定而无限搜索 | §11 |
| **Risk Gate** | "不能乱改" — 高风险变更无审查推进 | §10 + §12 |
| **KPI Boundary** | "不能无限慢" — Productivity 拍脑袋导致少分析 | §14 |
| **Exit Criteria** | "不能无限做" — 阶段无终止条件导致范围蔓延 | §16 + Phase 0 §7 |

五条闸门同时存在。任何一条失守都会让其他闸门失效。

### 1.3 Universal Core Purity

TRE 的 Universal Core 必须满足：

- 脱离 JNPF/Foundry/BBB/任何特定项目仍成立。
- 不引用具体 ORM API（SqlSugar / EF Core / Dapper / Hibernate 等）。
- 不引用具体数据库方言（SQL Server / PostgreSQL / MySQL 等方言分支）。
- 不引用具体字段命名约定（`F_xxx` 前缀 / `xxx_id` / `tenantId` 等）。
- 不引用具体历史遗留行为。

当一条规则来自具体项目经验但表达为通用原则时，必须先抽象到"relational persistence concept"层级，再入本文档。

---

## 2. Capability Boundary（Universal 七维 A–G）

每张表**至少**按以下七维判断。G 维必须保持抽象，Target 词汇由独立 Target Profile 注入。

| 维 | 名称 | 核心问题 | 风险域关联 |
|---|---|---|---|
| **A** | Schema | 字段/类型/Nullability/默认值/PK/生成值/约束是否与语义一致？ | R0/R1/R2 |
| **B** | Integrity | 唯一性/外键/CHECK/级联/孤儿/删除行为是否完备？ | R2/R3/R4 |
| **C** | Index | 真实查询路径是否有合理索引支撑？ | R1/R2 |
| **D** | Lifecycle | 增长/归档/冷热分离/保留策略是否清晰？Tenant/Soft-Delete/Audit 概念是否清晰？ | R2/R3/R4 |
| **E** | CRUD / Query | Service/Repository 的真实用法是否健康？ | R1/R2 |
| **F** | DDD / Aggregate Boundary | 表是 Aggregate Root / Child / Reference Data / Global Data？持久化边界是否正确？ | R3/R4 |
| **G** | Consumer / Target Readiness | 表的持久化语义能否被"目标数据基础设施"干净承接？ | R2/R3/R4 |

**Capability 不在范围（明确排除，防蔓延）**：完整 Service/Class 重构、业务领域设计、授权策略、工作流编排、微服务拆分、UI 改造。发现依赖时登记到"跨域依赖"，不替代实施。

---

## 3. Capability A — Schema

### 3.1 Universal 标准

A 维判定一张表的**字段、类型、Nullability、默认值、主键、生成值、约束**是否符合持久化语义。

#### A.1 列清单完整性

| 项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 列覆盖 | 每一列有明确语义用途；无"待定" / "预留" / 含义不明的列 | DDL + Entity + 一条真实读写路径 |
| 列命名 | 命名表达业务含义；不一致的命名变体需识别为同义异形 | DDL + Entity + 至少一处业务使用 |
| 注释/文档 | 关键列含义应在 DDL 注释、Entity 文档或数据字典中可追溯 | Entity 注释 OR 项目文档 |

#### A.2 数据类型选择

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 类型与语义一致 | 类型选择表达语义（如金额 ≠ 浮点；时间 ≠ 字符串；真假 ≠ 字符串'Y/N'） | DDL + Entity + 至少一处业务约束 |
| 精度与范围 | 字符串长度/数值精度满足业务极值；超长字段应有显式上限或文档化"无界"决策 | 一条写路径 + 一条读路径 |
| Unicode | 多语言字段应使用 Unicode 类型；纯 ASCII 字段可考虑非 Unicode 节省存储（视数据库方言） | 内容采样 |
| 大字段策略 | 超过 KB 量级的大字段应有专门处置（独立表/对象存储/压缩），不应与高频查询字段同表 | 一次实际查询的 I/O 数据 |

#### A.3 Nullability 语义

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 业务上"未知" | 应保留 NULL（如未填的可选属性） | DDL + Entity + 业务规则 |
| 业务上"漏填" | 应 NOT NULL + DEFAULT | DDL + Entity + 业务规则 |
| Nullability 漂移 | DDL 与 Entity 的 Nullability 必须一致 | DDL vs Entity diff |
| 三值逻辑 | 表达式与过滤条件应显式处理 NULL 与非 NULL（避免语义模糊） | 至少一处过滤条件代码 |

#### A.4 默认值

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 默认值一致性 | 业务默认与 DDL 默认一致；无歧义 | DDL + Entity + 一条插入路径 |
| 默认值来源 | 常量默认值应下沉至 DB DEFAULT，避免应用层重复 | 至少两次插入路径 |
| 默认值审计 | 默认值变更应记录影响面 | 一次变更历史 |

#### A.5 主键（PK）

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| PK 存在性 | 每张业务表必须有显式 PK；无 PK 表必须登记理由 | DDL |
| PK 语义 | PK 必须满足不可变、唯一、非空 | DDL + Entity |
| PK 类型 | 业务键 vs 代理键的选择应有显式决策依据 | 业务文档 OR DDL 注释 |
| 复合 PK | 复合 PK 应有显式业务含义（关联实体 / 多租户场景 / 自然键）；不允许"无意中复合" | DDL + 业务规则 |
| 生成值 | 自增/序列/UUID/雪花等生成策略应有显式文档化决策 | 生成器配置 + 业务规则 |

#### A.6 约束

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| CHECK 约束 | 业务值域应下沉至 DB CHECK | DDL + 业务值域 |
| 枚举 | 枚举值应在 DDL/CHECK/Entity 三方一致 | DDL + Entity |
| 计算列 | 持久化计算列须满足确定性函数依赖 | 计算列定义 + 业务规则 |

### 3.2 判定准则

A 维评估结论必须落到以下 5 类之一：

1. **Conforms** — 列、类型、Null、默认值、PK、约束与语义一致，无需变更。
2. **Low-risk Adjustment** — 注释、命名一致性、默认值下沉等低影响项。
3. **Structural Change** — 类型修正、长度调整、约束补齐。
4. **Semantic Migration** — 数据类型语义变化（需数据搬运）。
5. **Aggregate / Cross-table** — PK/约束影响聚合边界或其他表。

### 3.3 Evidence Thresholds

A 维证据**最低**取齐即停止：

- 字段语义判断：DDL + Entity + 一条真实读写路径 = 足够
- 类型选择判断：DDL + Entity + 业务极值证据 = 足够
- Nullability 判断：DDL + Entity + 业务"未知 vs 漏填"语义证据 = 足够
- PK 判断：DDL + Entity + 业务规则 = 足够

禁止"为了更确定"无限搜索业务代码。

---

## 4. Capability B — Data Integrity

### 4.1 Universal 标准

B 维判定表的**唯一性、外键、CHECK、级联、孤儿、删除行为**是否完备。

#### B.1 唯一性（UNIQUE）

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 业务唯一性 | 业务上"不应重复"的字段组合应有 DB 层 UNIQUE 约束，而非仅靠应用层"先查后插" | 全表查重零重复 + 一处写路径 |
| 唯一索引必要性 | 已存在的 UNIQUE 索引需有可追溯的业务解释 | 索引清单 + 业务规则 |
| NULL 与 UNIQUE | UNIQUE 约束对 NULL 的处理应与业务规则一致（多 NULL 是否允许共存） | DDL + 业务规则 |

#### B.2 外键（FK）

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 逻辑外键 | 代码中存在的强父子关系应在 DDL 有显式 FK 或显式登记为"逻辑外键"（并给出理由） | 代码引用 + 业务规则 |
| 物理 FK 完整性 | 现有 FK 不得有孤儿引用 | 全表孤儿扫描 |
| 级联策略 | ON DELETE / ON UPDATE 策略必须显式（RESTRICT / CASCADE / SET NULL / NO ACTION 任一），不存在"未思考的默认" | FK DDL + 删除路径 |
| 删除路径 | 代码中的删除路径应与级联策略一致；不一致时需显式裁决 | 代码 + DDL |
| 跨模块 FK | 跨模块 FK 应有架构裁决（避免模块间强耦合） | 架构图/裁决记录 |

#### B.3 CHECK 约束

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 值域下沉 | 业务值域应下沉至 DB CHECK，避免分散在应用层 | DDL + 应用层值域判断代码 |
| 一致性 | DB CHECK 与应用层值域判断应一致 | DDL vs 应用层 diff |
| 违规扫描 | 加 CHECK 前全表扫描违规，违规率超过 §10 阈值需熔断转专项 | 全表扫描 |

#### B.4 引用动作（Referential Actions）

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 显式声明 | 每个 FK 必须显式声明引用动作，不允许隐式默认 | FK DDL |
| 与业务一致 | 删除/更新父记录时的行为必须与业务规则一致 | 业务规则 + 删除路径 |
| 孤儿处理 | 现有孤儿行在加 FK 前必须清零 | 全表孤儿扫描 |

#### B.5 孤儿数据

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 孤儿检测 | 表加 FK/UNIQUE/CHECK 前必须做孤儿扫描 | 全表扫描 |
| 孤儿处置 | 孤儿必须明确处置（清理/标记/归档/排除） | 处置方案 + 业务规则 |
| 持续监控 | 高变更率表应有孤儿监控机制（避免引入孤儿） | 监控/审计机制 |

### 4.2 判定准则

B 维评估结论必须落到以下 5 类之一：

1. **Conforms** — 完整性约束完备且与代码一致。
2. **Constraint Add** — 新增 UNIQUE/FK/CHECK，需先做孤儿扫描与违规扫描。
3. **Constraint Adjust** — 调整引用动作或值域。
4. **Orphan Cleanup** — 清理孤儿后才能加约束。
5. **Aggregate Restructure** — 跨表聚合边界变更（移交 F 维）。

### 4.3 Evidence Thresholds

B 维证据**最低**取齐即停止：

- UNIQUE 加列前：全表查重 + 业务规则 = 足够
- FK 加列前：全表孤儿扫描 + 业务关系确认 = 足够
- CHECK 加列前：全表违规扫描 + 值域规则 = 足够
- 级联策略变更：删除/更新路径代码 + 业务规则 = 足够

禁止"为了更确定"扫描整个项目。

---

## 5. Capability C — Index Engineering

### 5.1 Universal 标准

C 维判定表的**索引设计**是否由真实查询路径驱动、是否与查询模式匹配、是否过度。

#### C.1 索引存在性

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 主查询索引 | 每条高频 Where 条件应有对应索引（含 Tenant/Lifecycle 过滤） | 至少一条真实查询 |
| 连接索引 | JOIN 字段应有索引 | JOIN 路径 |
| 排序索引 | ORDER BY 字段组合应有索引或可利用已有索引 | 排序路径 |
| 覆盖索引 | 高频投影列应在 INCLUDE 中（消除 Key Lookup） | 投影列集合 |

#### C.2 索引选择性

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 选择性 | 唯一值/总行数 < 0.1 的单列索引若无覆盖用途，应清理 | 索引统计 + 列分布 |
| 选择性 ≠ 唯一性 | 高选择性 ≠ 必须有索引；判断依据是查询模式 | 查询路径 |

#### C.3 复合索引列序

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 等值优先 | 等值列在前，范围列在后 | 查询条件 + 索引定义 |
| 列序匹配 | 索引列序应匹配查询模式（最左前缀原则） | 查询条件 + 索引定义 |

#### C.4 过滤索引 / 部分索引

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 适用场景 | 90% 行从不被查询时考虑过滤索引 | 行数 + 查询频次 |
| 谓词稳定性 | 过滤索引的谓词应稳定（参数化后仍命中） | 查询路径 |

#### C.5 隐式转换

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 无 CONVERT_IMPLICIT | 查询端参数类型应与列类型对齐，避免隐式转换 | 执行计划 |
| 类型对齐 | 修查询端优先（不动列类型） | 实体类型 + 参数类型 |

#### C.6 碎片与维护

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 碎片率 | 碎片率过高应 REORGANIZE 或 REBUILD | 索引统计 |
| 统计信息 | 统计信息应更新，过期的统计信息会导致计划劣化 | STATS_DATE |
| 维护窗口 | 大表 REBUILD 应评估锁窗口 | 表大小 + 写入压力 |

#### C.7 索引清理

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 重复索引 | 同一列集合的多余索引应清理 | 索引清单 |
| 未使用索引 | 长期未使用索引应清理（注意冷查询） | 查询路径 + 索引统计 |

### 5.2 判定准则

C 维评估结论必须落到以下 5 类之一：

1. **No Index Change** — 索引设计合理，无需变更。
2. **Add Index** — 新增索引，必须先有真实查询路径证据。
3. **Drop Index** — 清理低选择性/重复/未使用索引。
4. **Adjust Composite** — 调整复合索引列序或 INCLUDE。
5. **Performance Verification Required** — 需要性能对比验证（不得仅凭理论）。

### 5.3 Evidence Thresholds

C 维证据**最低**取齐即停止：

- 索引设计：一条真实查询（含 Where/OrderBy/Join/Tenant/Lifecycle Filter）+ 列分布 = 足够
- 索引清理：索引统计 + 查询路径 = 足够
- 性能优化：执行计划 + 一次实测 = 足够（禁止仅靠理论推断）

---

## 6. Capability D — Data Lifecycle

### 6.1 Universal 标准

D 维判定表的**生命周期语义**是否清晰：Tenant 概念、Soft-Delete 概念、Audit 概念、保留策略、可见性。

#### D.1 Tenant 概念（多租户）

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| Tenant 字段存在性 | 多租户表应有显式 Tenant 字段 | DDL + 业务规则 |
| 过滤点 | Tenant 过滤应集中在可拦截点（Interceptor / Global Filter），避免散落各 Service | 全表查询路径 |
| Tenant 归属 | 跨租户引用应有显式决策（同租户 / 全局 / 拒绝） | 跨租户引用代码 |
| 超级租户 | 超级租户/平台租户的语义应显式 | 业务规则 |
| Tenant 缺失处置 | 缺 Tenant 字段的行（如种子数据）的处置应显式 | 数据体检 |

#### D.2 Soft-Delete 概念

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 删除标记存在性 | 业务表应有显式 Soft-Delete 标记（除非有强理由硬删） | DDL + 删除路径 |
| 过滤一致性 | 所有查询路径必须过滤已删除行；漏过滤应登记 | 全表查询路径 |
| 唯一性兼容 | UNIQUE 约束应兼容 Soft-Delete 状态（已删行不阻塞新插入） | UNIQUE 索引定义 |
| 恢复 | Soft-Delete 应有恢复路径 | 恢复代码 |
| 硬删策略 | 真正硬删的策略应有显式决策（归档、异步清理） | 清理机制 |

#### D.3 Audit 概念

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 创建元数据 | 创建时间/创建人应有显式填充 | 一条创建路径 |
| 修改元数据 | 修改时间/修改人应有显式填充 | 一条更新路径 |
| 删除元数据 | 删除时间/删除人应有显式填充（与 Soft-Delete 配合） | 一条删除路径 |
| 填充一致性 | 应用代码与数据库默认值应一致 | DDL + 应用代码 |
| 审计不可绕过 | 元数据应自动填充，不依赖业务代码手工设置 | 写入路径 |

#### D.4 保留策略（Retention）

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 保留窗口 | 业务表应有显式保留窗口（多久清理/归档） | 业务规则 |
| 归档机制 | 高增长表应有归档表 + 迁移作业 | 写入增长率 |
| 冷热分离 | 冷数据应迁出主表 | 数据量 + 查询模式 |
| 合规约束 | 受合规约束的数据应显式标注（不可删除/不可修改） | 合规文档 |

#### D.5 可见性（Visibility）

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 公开/私有 | 字段的可见性（公开 vs 内部）应显式 | 业务规则 |
| 敏感数据 | 敏感字段应有处置策略（脱敏、加密、访问审计） | 业务规则 + 合规约束 |

### 6.2 判定准则

D 维评估结论必须落到以下 5 类之一：

1. **Conforms** — Tenant/Soft-Delete/Audit/Retention 概念清晰且一致。
2. **Filter Gap** — 过滤缺失（必须 R2 整改）。
3. **Metadata Gap** — 元数据缺失或填充不一致。
4. **Retention Missing** — 无保留策略（高增长表应触发归档设计）。
5. **Concept Conflict** — 多源对同一概念（如 Soft-Delete）有不同实现。

### 6.3 Evidence Thresholds

D 维证据**最低**取齐即停止：

- Tenant 过滤：一条未过滤查询的代码 = 足够定位问题；不需要扫描全部 Service
- Soft-Delete 过滤：同上
- Audit 填充：一条插入/更新路径 = 足够
- 保留策略：增长率 + 一次业务访谈 = 足够

---

## 7. Capability E — CRUD / Query Usage

### 7.1 Universal 标准

E 维判定 Service/Repository 的**真实 CRUD / Query 用法**是否健康。

#### E.1 CRUD 完整性

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| Create | 应有显式插入路径（无 Create-Only API 仅允许事件总线写入时需文档化） | 一次插入路径 |
| Read | 应有显式查询路径 | 一次查询路径 |
| Update | 应有显式更新路径 | 一次更新路径 |
| Delete | 应有显式删除路径（Soft-Delete / Hard-Delete / 仅归档） | 一次删除路径 |
| 缺失方法 | 缺 Create / Delete 时应登记理由 | 业务规则 |

#### E.2 N+1 查询

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 循环单查 | 循环内 `Find/First` 模式应改为批量查询 | 一段循环代码 |
| 隐式 N+1 | 投影中触发的关联查询应识别 | 一次慢查询 |

#### E.3 投影查询

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 全列加载 | 列表页不应全列加载实体 | 一次列表查询 |
| 必要列 | 投影应只取必要列 | 一次列表查询 |

#### E.4 批量操作

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 循环单行 | 循环单行 Insert/Update 应改为批量 | 一段导入代码 |
| 批量能力 | 利用数据库批量能力（BulkCopy / INSERT ... VALUES 多行） | 一次批量调用 |

#### E.5 分页

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 深分页 | OFFSET 深页应改为 Keyset 游标 | 一次深页调用 |
| 分页上限 | 无界 `ToList()` 应设上限 | 一次全表扫描 |

#### E.6 异步

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 数据访问异步 | 高频表应使用异步数据访问 | 一次同步阻塞调用 |

#### E.7 事务边界

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 事务粒度 | 多表写应在显式事务内 | 一段多表写代码 |
| 外部调用 | 事务内不应包含 HTTP / 文件 I/O / 远程调用 | 一段事务代码 |
| 隔离级 | 隔离级调整应单列评估 | 一次并发问题 |

### 7.2 判定准则

E 维评估结论必须落到以下 5 类之一：

1. **Conforms** — CRUD 完整且查询健康。
2. **N+1 / Projection / Batch / Pagination Fix** — 局部 R1 整改。
3. **Async / Transaction Refine** — 异步/事务边界 R2 整改。
4. **Query Restructure** — 查询语义重写（移交 E 维专评）。
5. **Cross-table Transaction** — 跨表事务 R3 整改。

### 7.3 Evidence Thresholds

E 维证据**最低**取齐即停止：

- N+1 定位：一段循环代码 = 足够
- 投影缺失：一次列表查询 = 足够
- 批量缺失：一段导入代码 = 足够
- 深分页：一次分页调用 + 表规模 = 足够

---

## 8. Capability F — DDD / Aggregate Boundary

### 8.1 Universal 标准

F 维判定表的**聚合归属与持久化边界**是否正确。注：本节只涉及**持久化视角**的 DDD 概念，不涉及应用层 DDD 框架。

#### F.1 分类

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| Aggregate Root | 一致性边界内的根实体；其生命周期管理子实体 | 业务规则 + 写路径 |
| Child Entity | 仅通过根实体访问；不独立持久化 | 写路径 |
| Value Object | 无独立标识；通过值比较 | DDL + Entity |
| Reference Data | 跨聚合引用的只读/低变更数据 | 业务规则 |
| Global Data | 全局共享、跨租户/跨业务域 | 业务规则 |

#### F.2 持久化边界

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 一致性边界 | 聚合内的强一致；跨聚合的最终一致 | 业务规则 |
| 跨聚合事务 | 跨聚合修改应避免单事务；使用 Saga / 事件 / 异步 | 跨聚合写路径 |
| 持久化映射 | 聚合根 → 主表；子实体 → 同表 / 1:N 子表 / JSON 列（任一显式选择） | DDL + Entity |
| 引用外键 | 跨聚合引用应使用 ID 而非 ORM 导航属性 | Entity 设计 |

#### F.3 决策记录

| 子项 | Universal 标准 | 最低证据阈值 |
|---|---|---|
| 分类理由 | 每张表的 DDD 分类应有可追溯的理由 | 业务规则 |
| 边界变更 | 跨聚合重划分应触发 R4 决策 | 业务规则 |

### 8.2 判定准则

F 维评估结论必须落到以下 5 类之一：

1. **Classified** — DDD 分类清晰，持久化边界与业务一致。
2. **Boundary Refine** — 边界需要细化（聚合根调整、子实体调整）。
3. **Aggregate Split** — 当前聚合过大需拆分（R4）。
4. **Aggregate Merge** — 多个聚合需合并（R4）。
5. **Reference Resolution** — 跨聚合引用方式需调整（ID vs 导航）。

### 8.3 Evidence Thresholds

F 维证据**最低**取齐即停止：

- 聚合分类：业务规则 + 一致性边界证据 = 足够
- 持久化映射：DDL + Entity + 一次写路径 = 足够
- 跨聚合事务：跨聚合写路径 = 足够

---

## 9. Capability G — Consumer / Target Readiness

### 9.1 Universal 标准

G 维判定表的**持久化语义能否被"目标数据基础设施"干净承接**。

**G 维本身是抽象的**。Target System 的具体契约（如某个 Generic Repository 框架的 `IAuditableEntity` / `ISoftDeleteEntity` / `ITenantEntity`）由独立 Target Profile 注入。Universal Master Spec 不识别任何 Target System。

#### G.1 Marker Concept 抽象集合

每张表需要评估是否具备以下抽象 Marker Concept 之一或多个。Profile 负责把 Marker Concept 映射到具体 Target 契约。

| Marker Concept | Universal 含义 | Profile 映射责任 |
|---|---|---|
| **Tenant Concept** | 表存在多租户隔离语义；通过 Tenant 字段标识行归属；过滤应集中 | Profile 决定 Target 的 `ITenantEntity` 等接口如何映射到 Tenant 字段 |
| **Soft-Delete Concept** | 表使用软删除而非硬删除；删除行通过状态字段识别 | Profile 决定 Target 的 `ISoftDeleteEntity` 等接口如何映射 |
| **Audit Concept** | 表有创建/修改/删除的元数据 | Profile 决定 Target 的 `IAuditableEntity` 等接口如何映射 |
| **Aggregate Root Concept** | 表是聚合根，承载一致性边界 | Profile 决定 Target 的 `IRepository<T>` 等接口如何映射 |
| **Read-Only Concept** | 表是只读 Reference Data / Global Data | Profile 决定是否需要只读 Repository 形态 |
| **Time Series Concept** | 表以时间为主要索引维度 | Profile 决定 Target 的 Keyset / 时间分区形态 |

#### G.2 承接度评级（Universal）

G 维评估结论必须落到以下 5 类之一（具体 Target 适配由 Profile 给出）：

1. **Native-Ready** — Marker Concept 与 Profile 的 Target 契约**直接可承接**（无需适配）。
2. **Adapter-Ready** — Marker Concept 与 Target 契约有差异，但**可在 Project Extension / Adapter 内映射**（不改 Universal Core）。
3. **Partial-Ready** — 部分 Marker Concept 可承接，部分需要 Project Extension。
4. **Not-Ready** — 当前持久化语义与 Target 契约有本质冲突，需要先做表级重构（Capability A–F）才能进入 G 维承接。
5. **N/A** — 表不涉及 Target 接入（如纯内部表）。

#### G.3 评估准则

G 维评估必须回答：

1. **本表涉及哪些 Marker Concept？**
2. **每个 Marker Concept 的当前持久化语义是什么？**（[KNOWN] 证据：DDL + Entity）
3. **每个 Marker Concept 与 Target Profile 的契约是否兼容？**（[INFERRED]）
4. **若不兼容，差距在哪？能否在 Project Extension 内适配？**（[DESIGN] 目标态）

G 维不允许"假设 Target 是 X"——任何 Target 引用都必须经 Target Profile 注入。

### 9.4 Evidence Thresholds

G 维证据**最低**取齐即停止：

- Marker Concept 识别：DDL + Entity + 至少一处读写路径 = 足够
- 承接度评级：Marker Concept 列表 + Target Profile 契约摘要 = 足够

---

## 10. Risk Model

### 10.1 风险等级（L1-R0 到 L1-R5）

| 等级 | 名称 | 定义（按**潜在业务/数据影响**，**非**实现复杂度） | 决策权 |
|---|---|---|---|
| **L1-R0** | No change | 评估结论为不需要任何变更 | AI 自主 |
| **L1-R1** | Low risk | 单点、低影响、可立即回滚（注释、命名一致性、低选择性索引清理、默认值下沉） | AI 自主 |
| **L1-R2** | Structural change | 表内结构变更（列类型/Null/默认值/索引增删改）但不改语义；不影响其他表；可完全回滚 | Evidence-driven 执行；Characterization 全绿即可 |
| **L1-R3** | Data / semantic migration | 涉及数据搬运、值转换、新约束生效、Soft-Delete 语义变更 | **人工批准**（PR 评审 + 数据体检 + 回滚脚本） |
| **L1-R4** | Cross-table / aggregate change | 跨表结构变更、聚合边界变更、级联策略变更、跨模块行为 | **Product + Architecture Decision Gate** |
| **L1-R5** | Destructive / production-impact | 不可逆、影响生产数据、跨环境行为变更、生产事故风险 | **Product + Architecture Gate + Pilot Dry-run + 灰度** |

### 10.2 Risk ≠ Implementation Complexity

风险等级判定**必须依据**：

- 数据完整性影响（是否可能丢数据/产生孤儿）
- 行为影响面（影响多少查询路径/调用方）
- 迁移影响（是否需要数据搬运/停机/双写）
- 生产影响（变更是否会被生产感知）

**不依据**：

- 改了多少行 SQL / 代码
- SQL 复杂度
- 涉及多少文件
- 改动"看起来大不大"

**反例**：
- 重建一个缺失 Index — 可能只是 R1/R2（影响局部查询）
- `tenant_field nullable → NOT NULL` — 可能只是一行 SQL，却可能是 **R3**（因为种子数据该字段为 NULL 时，NOT NULL 会导致现有行无法写入/读取；需先 UPDATE 种子行）

### 10.3 风险升级触发（STOP → Decision Brief）

以下任意一条触发，AI **必须停下取证、产出 Decision Brief、等人类决策**：

1. **PK 语义不明** — 多列 PK? 自增 vs GUID vs 业务键? 代理键 vs 业务键?
2. **FK 含义不明** — 孤儿数据？CASCADE 缺失但代码手动删？逻辑外键 vs 物理外键？
3. **破坏性迁移风险** — 数据丢失/截断/不可回滚
4. **数据类型转换风险** — 语义损失、精度丢失、字符集变化
5. **Nullability 语义冲突** — 业务上"未知"应保留 NULL vs "漏填"应 NOT NULL + DEFAULT
6. **Tenant ownership 不明** — 多租户字段位置、过滤点、租户归属不清
7. **Aggregate boundary 不明** — 无法判定聚合根、边界含子实体/引用数据关系不清
8. **跨表改造需求** — 变更必须穿透边界
9. **未解释的 legacy behavior** — 历史遗留行为找不到设计意图，删除前需定性
10. **目标 Contract 不兼容** — Target Profile 的契约与表当前语义冲突（移交 Phase 5 Target Profile 处理）

### 10.4 Risk 判定纪律

- 每条 Finding 必须有 Risk 等级标注。
- L1-R3 及以上：必须出 Decision Brief（Input / Options / Risks / Recommendation）。
- L1-R4 及以上：必须 Product + Architecture 拍板；AI 不得自主推进。
- L1-R5：必须 Pilot Dry-run + 灰度。

---

## 11. Evidence Model

### 11.1 Evidence 标签

复用既有标签 + 新增 `[DESIGN]`：

| 标签 | 含义 | 示例 |
|---|---|---|
| `[KNOWN]` | 可被 DDL/Entity/代码直接读取的事实 | 列名、类型、PK |
| `[COMPUTED]` | 由已知事实计算或推导 | 索引选择性 = distinct/total |
| `[INFERRED]` | 由多源证据推断的合理结论，但需在 [GUESS] 升格前显式标记 | "该表无读消费方" 来自"全仓 grep 零结果" |
| `[GUESS]` | 假设性结论，未证实 | "运行时可能产生该问题" |
| `[DESIGN]` | **目标态**，不是现有事实 | "计划将软删标记由 `int? DeleteMark` 改造为 `bool IsDeleted`（通过 Adapter 映射）" |

### 11.2 Evidence 纪律

1. **`[DESIGN]` 不得与 `[KNOWN]/[COMPUTED]` 混用** — Evidence 是当前事实，DESIGN 是目标状态。
2. **每条 Finding 必须有至少一条 `[KNOWN]` 或 `[COMPUTED]` 证据支撑**。
3. **`[GUESS]` 不允许出现在决策结论中** — 只允许出现在"待进一步取证"的临时标注。
4. **`[INFERRED]` 升格为 `[KNOWN]/[COMPUTED]` 的条件** — 有直接可读的代码/DDL/SQL Plan 证据。

### 11.3 Evidence Sufficiency Stop Rule（核心）

达到当前决策所需**最低证据阈值**后**立即停止取证**。

各 Capability 的最低证据阈值详见 §3.3 / §4.3 / §5.3 / §6.3 / §7.3 / §8.3 / §9.4。

**判定标准（总则）**：

| 决策类型 | 最低证据集 |
|---|---|
| 字段语义判断 | DDL + Entity + 一条真实读写路径 |
| 索引设计 | 一条真实查询（含 Where/OrderBy/Join/Tenant/Lifecycle Filter）+ 列分布 |
| 风险等级判定 | 影响面 + 回滚成本 + 数据风险 |
| Marker Concept 识别 | DDL + Entity + 一处读写路径 |
| 聚合分类 | 业务规则 + 一致性边界证据 |

**禁止行为**：

- "为了更确定"继续扫描全仓
- 反复验证已经 `[KNOWN]` 的事实
- 为找全所有用例而扫描所有 Service
- 在未确认决策前并行开多条证据链

**Evidence 应服务于 Decision，而不是阻塞开发。**

---

## 12. Hard Gates（STOP → Decision Brief）

10 条触发器，遇任一 → STOP → Decision Brief → 人类决策。

| # | Hard Gate | 触发后产物 |
|---|---|---|
| 1 | PK 语义不明 | Decision Brief：PK 设计决策 |
| 2 | FK 含义不明 | Decision Brief：FK 含义与级联策略 |
| 3 | 破坏性迁移风险 | Decision Brief：迁移方案 + 回滚 + 灰度 |
| 4 | 数据类型转换风险 | Decision Brief：转换方案 + 数据体检 |
| 5 | Nullability 语义冲突 | Decision Brief：业务语义裁决 |
| 6 | Tenant ownership 不明 | Decision Brief：Tenant 字段与过滤点 |
| 7 | Aggregate boundary 不明 | Decision Brief：聚合划分 |
| 8 | 跨表改造需求 | Decision Brief：跨表改造方案 + 协同批次 |
| 9 | 未解释的 legacy behavior | Decision Brief：保留 / 移除 / 重定义 |
| 10 | 目标 Contract 不兼容 | Decision Brief：Target Profile 差异与适配方案 |

Hard Gate 触发后，AI 必须停下取证，**不可继续评估其他维度**——决策影响范围可能跨越多维。

---

## 13. TABLE CLOSED Definition

### 13.1 状态语义

**TABLE CLOSED = 一张表的本次重构生命周期结束，进入"持续维护 + 后续 Re-trigger 条件"状态。**

### 13.2 关闭判定 DoD（13 条最小充分条件）

| # | 条件 | 说明 |
|---|---|---|
| 1 | Schema understood | A 维：字段、类型、Nullability、默认值已记录 |
| 2 | Integrity validated | B 维：唯一/外键/CHECK/级联/孤儿已记录 |
| 3 | Index justified by real query | C 维：每个索引可追溯到真实查询路径或显式记录"暂未发现消费方" |
| 4 | Lifecycle semantics defined | D 维：Tenant/Soft-Delete/Audit/Retention 概念已记录 |
| 5 | CRUD / query usage mapped | E 维：真实 CRUD/Query 用法已记录 |
| 6 | DDD boundary classified | F 维：聚合根/子实体/引用数据/全局数据已分类 |
| 7 | Marker Concepts identified | G-1：涉及的抽象 Marker Concept 列表已记录 |
| 8 | Target readiness classified | G-2：承接度评级已记录（Native-Ready / Adapter-Ready / Partial / Not-Ready / N/A） |
| 9 | Target design defined | `[DESIGN]` 标签下的目标态已记录 |
| 10 | Change implemented OR No-change justified | 实际变更 OR 显式 No-change 决策理由 |
| 11 | Verification passed | CRUD 接口快照比对一致 + 考卷全绿 + 性能动作附存档 |
| 12 | No unresolved blocking finding | P0/P1 = 0；未决议项登记到"跨阶段遗留"区 |
| 13 | No unexplained behavior | "未知 legacy behavior" 全部定性（保留/移除/重定义） |

### 13.3 DoD 最小充分原则（核心闸门）

DoD 是**最小充分条件**，不是**最大努力清单**。

满足以下五项即可关闭：

- **风险已知** — Risk 等级已判定
- **目标明确** — DESIGN 已定义
- **必要整改完成** — L1-R3 及以上变更已批准 + 实施 + 验证
- **证据充分** — Evidence Sufficiency Stop Rule 已达
- **无 Blocking Finding** — P0/P1 = 0

不得因"未来还可以优化索引"拒绝关闭。**未来优化属于 Re-trigger 触发条件，不是当前关闭的阻碍。**

### 13.4 合法 No-change 出口

当 13 条 DoD 全达成且结论为"无需任何变更"时，TABLE CLOSED 合法。理由必须显式记录：

- 已知 legacy behavior 已定性
- 现有结构与 Target Profile 契约无冲突
- 当前无任何健康指标异常
- 索引设计已证明有真实查询路径支撑（或显式记录无消费方）

### 13.5 Re-trigger 条件

TABLE CLOSED 不等于永远不再打开。Re-trigger 触发：

- 数据量 / 查询模式 / 写入压力达到原设计假设的偏离阈值
- Target Profile 契约升级
- 跨表聚合边界重新设计
- Schema / Entity 实测漂移被检出

---

## 14. KPI Definitions

### 14.1 Quality（v1.0 即设定）

| 指标 | v1.0 Target | 来源 |
|---|---|---|
| P0 unresolved | **0** | L1-R5 风险零容忍 |
| Unresolved P1 | **0** | L1-R5 风险零容忍 |
| Data integrity regression | **0** | 改造前后 Characterization 全绿 |
| Unexpected behavior regression | **0** | 快照比对 + 考卷 |
| Evidence completeness | **≥ 95%** | Evidence Pack 字段填毕率 |

### 14.2 Finding Quality（统一术语）

**注意**：本节指标是**Finding classification error rate**，**不是**传统机器学习意义上的 Precision/Recall。

| 指标 | v1.0 Target | 含义 |
|---|---|---|
| **False Positive Rate** | **≤ 10%** | 标识为 Finding 的项中，实际不是真问题的占比 |
| **False Negative Rate** | **≤ 5%** | 实际真问题中，被漏标的占比 |

**统一表述**：

- 使用 `False Positive Rate ≤ 10%` / `False Negative Rate ≤ 5%`（**FP/FN 名称**），不混用 "Precision ≤ 10%" / "Recall ≤ 5%" 的 ML 术语。
- 这两个术语与 ML 的对应关系（仅供理解参考）：FP rate ↔ 1 - Precision；FN rate ↔ 1 - Recall。但 ML 含义的"precision"会让人误以为越高越好；FP/FN 命名更准确反映"错误率越低越好"。

### 14.3 Efficiency

| 指标 | v1.0 Target | 含义 |
|---|---|---|
| Rework Rate | **≤ 10%** | 关闭后再次打开率 |
| Autonomous Resolution Rate | **≥ 80%** | L1-R0/R1/R2 闭环率 |
| Human Gate Rate | **≤ 20%** | 触发人工决策卡的比例 |

### 14.4 Productivity（**v1.0 不预设数字**）

下列指标**仅在 Pilot 完成后采集真实数据**，v1.0 Freeze 时填入第一版 target；**不得在 Pilot 前拍数字**。

- **Tables Closed / AI Engineer Hour**
- **Median Table Completion Time**
- **P90 Table Completion Time**
- **Engineering Yield**（每投入 1 小时 AI 工作关闭的高质量 Table Unit 数；反向控制无限取证）

### 14.5 KPI 指标控制约束

- v1.0 Freeze 前必须能展示所有 Quality 指标 + Pilot 期 Precision/Efficiency 实测数据。
- v1.0 Freeze 前 Productivity 指标可以只填"baseline 待采集"，禁止填拍脑袋的数字。
- KPI 不得引入新的"amount of change"度量（如 changed lines、index 数量、finding 数量）作为主指标。

---

## 15. Universal Core Purity Gate

### 15.1 操作化方法

每条规则入本文档前必须经过三问 Purity Check：

| 问题 | 答案与处理 |
|---|---|
| **A. 是所有关系型项目都成立？** | Yes → 进入本文档 |
| **B. 至少是绝大多数企业业务系统成立？** | Yes → 进入本文档 |
| **C. 还是只是 JNPF / Foundry / BBB / SqlSugar / EF Core / 任何特定项目/ORM/方言特例？** | Yes → **禁止入本文档**，必须进入 Extension 或 Target Profile |

### 15.2 Purity Check 清单（每条规则填写）

每条规则在 §3–§9 出现时，必须在末尾显式标注 Purity Check 结果：

```
[PURITY: A]   → 全行业通用
[PURITY: B]   → 多数企业通用
[PURITY: EXTENSION] → 必须由 Extension/Profile 承载
```

如果作者无法判断为 A 或 B，必须默认为 EXTENSION，不得入本文档。

### 15.3 Purity 反向污染防御

发现"原以为是 A，实际是 C"时：

1. 立即从本文档移除该规则。
2. 沉淀到对应 Extension 或 Profile。
3. 不得修改本文档其余部分以"调和"该特例。

### 15.4 严禁本文档出现的词汇类别

| 类别 | 严禁词举例 |
|---|---|
| 特定项目/产品名 | 任何具体 ORM 框架名、任何特定应用产品名 |
| 特定 ORM API | `AsSugarClient()` / `DbContext.SaveChanges()` / `Session.Save()` / `ISqlSugarRepository<T>` 等 |
| 特定数据库方言关键字 | `MERGE` / `OUTPUT` / `RETURNING` / `ON DUPLICATE KEY` 等方言分支 |
| 特定字段命名约定 | `F_` 前缀 / `tenantId` 具体写法 / `Id` vs `ID` vs `_id` |
| 特定基类/接口名 | 任何 Consumer/Project 的 Entity 基类名 |
| 特定历史遗留行为 | 任何项目历史 case 的具体描述 |

**Universal 措辞范例**：

| 不允许 | Universal 表述 |
|---|---|
| "SqlSugar 通过 [SugarTable] 标注" | "ORM mapping metadata 应在 Entity 中可追溯" |
| "PostgreSQL 的 RETURNING 子句" | "插入/更新返回标识的能力应作为 Capability 探测" |
| "JNPF 的 F_TENANT_ID 字段" | "Tenant 字段应显式存在并集中过滤" |

---

## 16. Phase 1 Exit Criteria

Phase 1 完成必须满足以下全部条件，方可进入 Phase 2。

### 16.1 Universal 技术规范完整性

- [ ] Universal capability boundaries defined（§2）
- [ ] A Schema criteria defined（§3）
- [ ] B Integrity criteria defined（§4）
- [ ] C Index criteria defined（§5）
- [ ] D Lifecycle criteria defined（§6）
- [ ] E CRUD / Query criteria defined（§7）
- [ ] F DDD criteria defined（§8）
- [ ] G Target Readiness criteria defined（§9，抽象）
- [ ] Risk model defined（§10，L1-R0..R5）
- [ ] Evidence thresholds defined（§11 + 各 Capability 阈值）
- [ ] Hard Gates defined（§12，10 条）
- [ ] TABLE CLOSED defined（§13，13 条最小充分）
- [ ] KPI definitions stable（§14，含 FP/FN 统一术语）
- [ ] Universal Core Purity Gate operationalized（§15）

### 16.2 Purity Gate 通过

- [ ] Universal Core Purity Gate passes（§15 操作化已生效）
- [ ] **No JNPF dependency** in the specification（grep 验证 §0–§15 无 JNPF 相关字眼）
- [ ] **No Foundry/BBB dependency** in the specification
- [ ] **No specific ORM dependency** in the specification
- [ ] **No specific database dialect dependency** in the specification
- [ ] **No specific field naming convention dependency** in the specification

### 16.3 内部一致性

- [ ] No unresolved internal contradiction（Risk 等级判定与 Evidence Sufficiency 一致）
- [ ] No unnecessary scope expansion（未提前引入 Phase 2/3/5 内容）
- [ ] No placeholder（全文 0 个 TBD/TODO/待补）

### 16.4 Phase 2 接口就绪

Phase 1 必须为 Phase 2 提供以下接口（不展开 SOP，仅定义接口）：

- [ ] Evidence Pack 字段定义（统一格式）
- [ ] Risk 等级字段定义（机读）
- [ ] Hard Gate 检测项清单（机读）
- [ ] TABLE CLOSED DoD 字段清单（机读）
- [ ] KPI 字段定义（机读）
- [ ] Purity Check 标注规范

---

## 17. 文档保护声明

1. **严禁删除**本文档及后续冻结版本（含"合并/升级/冗余清理"等理由）。
2. **允许修改**；每次修改必须升版本号并在 §18 登记版本历史，旧版本保留存档。
3. 修改不得削弱 Hard Gate、降低 Risk 模型严格度、扩大 Capability 范围、引入特例依赖、放弃 Purity Gate。
4. **Backward Compatibility**：Phase 2/3/5 必须能跟随本文档的修改而适配，不允许 Phase 后续阶段在 Universal Core 内添加特例以"避免改 Phase 1"。

---

## 18. 版本历史

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 (draft) | 2026-08-29 | 首版 Master Spec。Phase 0 冻结后的 Universal 技术规范。含 Capability A–G 七维 / Risk R0–R5 / Evidence 五标签 / Hard Gate 10 条 / TABLE CLOSED 13 条最小充分 DoD / KPI FP-FN 统一术语 / Universal Core Purity Gate 操作化 / Phase 1 Exit Criteria。严格无 JNPF/Foundry/BBB/SqlSugar/EF Core/SQL Server 等字眼。 |

---

## 附录 A — Evidence Threshold Reference（统一速查）

各 Capability 的最低证据阈值（满足即停止取证）：

| Capability | 最低证据集 |
|---|---|
| **A Schema** — 字段语义 | DDL + Entity + 一条真实读写路径 |
| **A Schema** — 类型选择 | DDL + Entity + 业务极值证据 |
| **A Schema** — Nullability | DDL + Entity + 业务"未知 vs 漏填"语义证据 |
| **A Schema** — PK | DDL + Entity + 业务规则 |
| **B Integrity** — UNIQUE 加列 | 全表查重 + 业务规则 |
| **B Integrity** — FK 加列 | 全表孤儿扫描 + 业务关系确认 |
| **B Integrity** — CHECK 加列 | 全表违规扫描 + 值域规则 |
| **B Integrity** — 级联策略 | 删除/更新路径代码 + 业务规则 |
| **C Index** — 索引设计 | 一条真实查询 + 列分布 |
| **C Index** — 索引清理 | 索引统计 + 查询路径 |
| **D Lifecycle** — Tenant 过滤 | 一条未过滤查询代码 = 足够定位 |
| **D Lifecycle** — Soft-Delete 过滤 | 同上 |
| **D Lifecycle** — Audit 填充 | 一条插入/更新路径 |
| **D Lifecycle** — 保留策略 | 增长率 + 一次业务访谈 |
| **E CRUD/Query** — N+1 | 一段循环代码 |
| **E CRUD/Query** — 投影 | 一次列表查询 |
| **E CRUD/Query** — 批量 | 一段导入代码 |
| **E CRUD/Query** — 深分页 | 一次分页调用 + 表规模 |
| **F DDD** — 聚合分类 | 业务规则 + 一致性边界证据 |
| **F DDD** — 持久化映射 | DDL + Entity + 一次写路径 |
| **F DDD** — 跨聚合事务 | 跨聚合写路径 |
| **G Readiness** — Marker Concept | DDL + Entity + 一处读写路径 |
| **G Readiness** — 承接度评级 | Marker Concept 列表 + Target Profile 契约摘要 |

---

## 附录 B — Capability ↔ Hard Gate 交叉索引

| Hard Gate | 主要触发 Capability |
|---|---|
| 1 PK 语义不明 | A.5 + F.1 |
| 2 FK 含义不明 | B.2 + B.4 + F.2 |
| 3 破坏性迁移风险 | A.2 + A.3 + B.1 + B.3 |
| 4 数据类型转换风险 | A.2 + B.3 |
| 5 Nullability 语义冲突 | A.3 + D.2 |
| 6 Tenant ownership 不明 | D.1 + G.1 |
| 7 Aggregate boundary 不明 | F.1 + F.2 |
| 8 跨表改造需求 | B.2 + B.4 + F.1 + F.2 + D.1 |
| 9 未解释的 legacy behavior | 全部 Capability（需人工定性） |
| 10 目标 Contract 不兼容 | G.2 + G.3 |

---

## 附录 C — TABLE CLOSED DoD ↔ Capability 交叉索引

| DoD # | 对应 Capability |
|---|---|
| 1 Schema understood | A |
| 2 Integrity validated | B |
| 3 Index justified | C |
| 4 Lifecycle semantics | D |
| 5 CRUD/query mapped | E |
| 6 DDD classified | F |
| 7 Marker Concepts | G-1 |
| 8 Target readiness | G-2 |
| 9 Target design | DESIGN（跨 A–G） |
| 10 Change/No-change | A–F 任一 |
| 11 Verification | 跨 A–F（对应 E 维） |
| 12 No blocking finding | 跨 A–G |
| 13 No unexplained behavior | 跨 A–G |

---

## 附录 D — Capability ↔ Risk 等级典型关联

| 风险等级 | 典型 Capability 改动 |
|---|---|
| **R0** | 无 |
| **R1** | A 注释/命名、B CHECK 值域、C 索引清理、D 注释 |
| **R2** | A 类型修正、B UNIQUE/FK/CHECK 加列、C 索引增删改、D 默认值下沉、E N+1/投影/批量 |
| **R3** | A Null 变更、A 类型语义变更、D Soft-Delete 语义变更、F 聚合边界细化 |
| **R4** | A PK 调整、B 级联策略变更、D 跨租户调整、F 聚合拆分/合并 |
| **R5** | A 不可逆类型变更、B 不可逆约束变更、F 聚合全表重构、D 不可逆归档/清理 |

注：上表为典型关联，**实际 Risk 等级必须按 §10.2 影响面判定，不机械套用本表**。
