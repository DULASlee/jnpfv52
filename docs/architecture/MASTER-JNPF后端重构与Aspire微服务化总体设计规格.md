# JNPF 后端重构与 Aspire 微服务化总体设计规格

**版本**：v1.0 ｜ **状态**：MASTER / LONG-LIVED ｜ **日期**：2026-08-26
**性质**：项目最高层技术路线与边界约束 ｜ **裁决依据**：2026-08-26 人工终审（专家建议全文采纳）
**姊妹文档**：《JNPF 后端重构与 Aspire 微服务化总体实施计划》（同目录 MASTER-\*）——回答"按什么顺序做、做到什么程度、什么时候停下来让人裁决"

---

## ⚡ 路线记忆卡（ROUTE MEMORY CARD —— 任何 Agent 每次开工前必读）

> ## JNPF 重构项目唯一主线
>
> **我们不是重新设计 JNPF。**
>
> **我们是在现有 JNPF 上进行拆分和优化。**
>
> `Baseline → Platform Boundary → Modularization → Physical Decomposition → Contract → Aspire → Microservices → Data Isolation`
>
> * 不重新设计数据库
> * 不预设 Domain
> * 不预设微服务数量
> * 不因 Aspire 改变架构边界
> * 不删除未知/遗留数据
> * 不让 Demo/Template/Customer Data 污染平台判断
> * 先代码模块化，再服务化
> * 数据库隔离最后根据实际 Ownership 和迁移证据决定
> * AI 执行，人类裁决
> * 每阶段 PASS / REFINE / BLOCK 后停止

---

## 0. 文档地位与体系

1. **两主文档制**：本规格（做什么 / 为什么 / 什么不能做）+ 总体实施计划（顺序 / 程度 / 停点）。以后所有 Task、Slice、Incident、Refine 都只是这两份文档下面的**执行项**，一律从实施计划派生，**不再新增主线编号**（NG-x 系列永久退役为历史证据链，登记见 §9）。
2. **冲突规则**：任何下位文档（PHASE 规格 / Task 卡 / 旧基线 / 旧 NG 文档）与本规格 §2 六原则、§8 八禁、§5 PHASE 定义冲突时，**以本规格为准**。
3. **修订控制**：§2/§5/§8 为宪法条款，仅人类裁决可修订；其余条款经 Human Gate 修订并记录版本史。

---

## 1. 项目总目标

本项目不是重新开发一个新的 JNPF，也不是重新设计一套数据库。

### 唯一总目标

> **以现有 JNPF 后端为唯一事实基础，在保持现有业务行为、数据兼容性和平台能力连续性的前提下，通过持续重构、模块化、物理程序集拆分、契约解耦和渐进式部署隔离，最终形成以 Aspire 为运行编排基础的微服务化 JNPF 后端。**

最终演进路线：

```text
现有 JNPF
   ↓
现状基线与资产清理
   ↓
平台代码 / Demo / Template / Customer / Legacy 分离
   ↓
现有模块边界识别
   ↓
模块内部重构与依赖治理
   ↓
Project / DLL / NuGet 物理拆分
   ↓
Contract / API 解耦
   ↓
模块化单体
   ↓
Aspire 编排
   ↓
选择具备独立运行条件的模块
   ↓
渐进式微服务化
   ↓
必要时进行数据库物理隔离
```

---

## 2. 六条最高级原则（P1–P6，任何 AI Agent 必须遵守）

### P1 不重新设计 JNPF

- **禁止**："重新定义一个 JNPF Next Domain Model"。
- 目标是**拆分和优化现有 JNPF**。"JNPF Next"作为独立重建目标的提法退役，改称「重构后的 JNPF 后端」。

### P2 不预先重新设计数据库

数据库不是架构起点。必须遵循：

```text
代码 Ownership → 生命周期 → 事务边界 → Contract → 运行依赖 → 迁移可行性 → 数据库物理隔离
```

> **数据库拆分是代码和运行边界成熟后的结果，而不是项目第一阶段的设计输入。**
> （据此取消原拟《JNPF Next 数据架构与数据库设计规范》——尚未动工，零废置成本。）

### P3 不根据表名判断业务领域

```text
ext_order ≠ Order Domain        wform_* ≠ Workflow Domain
```

必须区分七+一类：`Platform / Template / Demo / Customer Application / Legacy / Orphan / Framework (+ Unknown)`。此前 NG-1A / NG-1B 的资产考古结果作为**历史证据**保留（登记见 §9），是 PHASE 1 的既成输入。

### P4 不因为 Aspire 预设微服务数量

Aspire 是**应用编排和运行基础设施**，不是 Domain Designer。

```text
❌ Aspire → 12 个 Service → 反过来设计代码
✅ 代码模块成熟 → Contract 稳定 → 运行边界明确 → 独立部署有价值 → 才服务化 → Aspire 编排
```

### P5 先模块化，再微服务化

```text
Monolith → Modular Monolith → Physically Modular Backend → Service-ready Modules → Microservices
```

**不允许一步跳到 Monolith → Microservices。**

### P6 人类控制架构，AI 执行工程

AI 可以：搜索 / 分析 / 编写代码 / 重构 / 测试 / 生成证据 / 执行迁移 / 验证。
AI 不得自行决定：Domain 边界 / 数据所有权 / 数据删除 / 数据库拆分 / 微服务数量 / 关键兼容性策略。

```text
Evidence → Agent Recommendation → Human Decision → Freeze → Agent Execution
```

---

## 3. 项目最终架构形态

最终不是简单追求"服务越多越好"：

```text
                    ┌──────────────────┐
                    │      Aspire      │
                    │ App Host / Infra │
                    └────────┬─────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
        Platform Core    Runtime       Independent
          Modules        Modules         Services
              │              │              │
              └──────────────┼──────────────┘
                             │
                     Stable Contracts
                             │
                  ┌──────────┴──────────┐
                  │ Existing Data Layer │
                  └──────────┬──────────┘
                             │
                 Progressive DB Isolation
```

**特别强调：最终也不要求所有模块都变成微服务。** 某些模块可能长期保持 Shared Platform Module——这也是合法架构结论。

---

## 4. 数据库战略："Preserve First, Isolate Later"

```text
第一阶段：代码拆分 + 数据库保持兼容
第二阶段：模块边界稳定 + 数据 Ownership 清晰
第三阶段：Logical Boundary → Migration Proof → Independent DB
```

任何数据库拆分必须回答十问：

1. 谁拥有数据？ 2. 谁负责写？ 3. 谁负责生命周期？ 4. 谁负责事务？ 5. 谁读取？
6. 跨域访问怎么办？ 7. 如何迁移？ 8. 如何回滚？ 9. 旧数据怎么办？ 10. 失败后如何恢复？

**没有这些证据，不允许创建新的独立数据库。**

---

## 5. 固定主线：PHASE 0–7（不再递增主线编号）

| Phase | 名称 | 一句话目标 | 核心产物 | Gate |
|---|---|---|---|---|
| 0 | Baseline | 把现有 JNPF 固定下来 | 六类基线 + Legacy Compatibility Registry | 三态 |
| 1 | Platform Boundary | 哪些东西属于真正要重构的平台 | Platform Asset Inventory | 三态 |
| 2 | Modularization | 现有代码应该怎样模块化 | Module Map + Dependency Map | 三态 |
| 3 | Physical Decomposition | 逻辑模块变成物理边界 | DLL / NuGet / Project 拆分 | 三态 |
| 4 | Contract Decoupling | 消费者依赖能力，不依赖实现 | Public Contract 层 | 三态 |
| 5 | Aspire Enablement | Aspire 成为统一运行/配置/观测入口 | AppHost + 资源编排 | 三态 |
| 6 | Progressive Service Extraction | 逐模块过 Service Candidate Gate | 独立服务（仅达标者） | 三态 |
| 7 | Data Isolation | 最后才做数据物理隔离 | Shadow Verify + Cutover | 三态 |

逐阶段详细约束见实施计划 §5 执行卡。全程维持各阶段"五零/六零"级别的只读或最小侵入纪律（由该阶段规格 redefine）。

---

## 6. 关键阶段红线摘要

- **PHASE 1 不是 Domain Design**——只回答"哪些属于我们要重构的平台"。已证明的事实必须继承：`ext_*` 含 Demo、`WFORM_*` 含产品模板、`WM_*/WH_*` 为真实历史数据、部分历史表与当前平台代码无关。
- **PHASE 2 不是重新设计业务**——Module Map 中的名称（JNPF.Framework / Identity / Authorization / Tenant / Organization / Workflow / VisualDev / Integration / File / Message / AI…）只是**现有代码重构目标候选**，不是预设微服务。
- **PHASE 3 主要治理九项**：循环依赖 / Framework 污染 / Service 互相调用 / Repository 穿透 / Entity 泄漏 / Shared DbContext / Shared Utility / 静态依赖 / 隐式 Contract。
- **PHASE 5 第一目标不是拆服务**——先让 Aspire 成为统一运行、配置、依赖和观测入口。
- **PHASE 6 一个模块不满足 Service Candidate Gate 条件，就继续保持 Modular Monolith**（合法结论，不是失败）。

---

## 7. 执行纪律

每个工作单元使用固定九段格式：

```text
Task
├── Objective
├── Scope
├── Preconditions
├── Evidence
├── Implementation
├── Verification
├── Regression
├── Artifacts
└── Human Gate
```

每个阶段结束必须三选一收口：`PASS / REFINE / BLOCK`，然后 STOP 等待人工裁决。

---

## 8. 永久禁止事项（八禁——看到即停止并请求人工裁决）

| # | 禁止 |
|---|---|
| 1 | "重新设计数据库" |
| 2 | "创建新的 JNPF Next Domain Model" |
| 3 | "为了微服务而拆微服务" |
| 4 | "因为 Aspire，所以建立 N 个服务" |
| 5 | "根据表名判断 Domain" |
| 6 | "删除没有代码引用但存在数据的表" |
| 7 | "复制 Legacy 行为重新实现" |
| 8 | "一次性完成 Monolith → Microservices" |

---

## 9. 历史工作登记表（NG 系列归档定位——保留成果，改变定位）

| 工作 | 新定位 | 继续有效的资产 | 废止 / 降级部分 |
|---|---|---|---|
| NG-0 可行性侦察 | 历史基线 / 早期架构假设；PHASE 0/2 输入 | 五规格十证据；租户/权限/数据访问契约研究；Anti-Service 清单 | "JNPF Next 重建"框架表述 |
| NG-1A 平台资产考古 | **PHASE 1 主体已完成** | P0–PX 分类、289 表清单、二维标签 | — |
| NG-1B Provenance Matrix | **PHASE 1 主体已完成** | 157/132 结论、PROVEN 矩阵、Demo/Template/Legacy 铁证 | — |
| NG-1C 大矩阵 | SUSPENDED（未执行即被收敛，永不启动） | 六态/三权/PASS 完整性方法论存档备查 | 工程本体 |
| D1–D12 候选域 | SUPERSEDED，仅候选线索 | 十维分析方法 | 全部域结论 |
| 《JNPF 平台整体结构基线》v0.2 | 降级为 PHASE 1 事实输入 | §二能力地图、§三资产边界与证伪链 | "重建 Next"战略表述、其 PHASE 0–8 表、H1–H5 假设（并入本规格复审） |
| 原拟《JNPF Next 数据架构与数据库设计规范》 | **未启动即取消** | — | 违反 P2 |

---

## 10. 仓库既有资产 → PHASE 映射（全部"待按 P1–P6 复核"，不自动继承结论）

| PHASE | 既有资产 | 位置 |
|---|---|---|
| 0 | 复杂度基线 + JNPF009 Analyzer、Architecture tests、CI gate（lint→type-check→test:unit→build）、toolchain/hooks 校验 | `backend/tools/JNPF.Analyzers/`、`.github/workflows/ci.yml`、`scripts/verify-toolchain.mjs`、`scripts/test-hooks.mjs` |
| 1 | 资产分类 289×12 列、Provenance 289×26 列、Demo/Template/Legacy 登记册 | `.claude/evidence/jnpf-next-architecture/ng1a-product-boundary/`、`ng1b-provenance/` |
| 2 | 16 个 modularity 模块、模块依赖扫描闸门、领域与模块边界研究 | `backend/modularity/`、`scripts/arch-module-dependency-scan.ps1`、`docs/superpowers/specs/JNPF-Next-领域与模块边界规格.md` |
| 3 | 后端物理拆分 DLL 化规格 v2.3 + 实施计划（需按 P1–P6 复核吸收） | `docs/superpowers/specs/架构设计规格-后端物理拆分-DLL化-v2.3.md` 及对应 plan |
| 5 | Aspire 与运行架构研究（工具层定位与本规格 P4 一致） | `docs/superpowers/specs/JNPF-Next-Aspire与运行架构规格.md` |

---

## 11. 版本历史

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 | 2026-08-26 | 首版。确立 P1–P6 六原则、PHASE 0–7 固定主线、Preserve First 数据库战略、八禁、Task 九段纪律；NG 系列归档登记；取消《JNPF Next 数据架构与数据库设计规范》 |
