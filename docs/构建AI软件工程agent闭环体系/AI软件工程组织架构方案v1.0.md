# AI 软件工程组织架构方案 v1.0

> **文档定位**：企业级 AI 软件工程体系总体设计规范
> **文档状态**：v1.0-draft（基于专家规划方案 + 现有架构基线锚定校准）
> **文档日期**：2026-08-30
> **适用项目**：JNPF v5.2 及后续演进、AI 原生开发体系
> **所属系列**：`docs/架构迭代/8、AI编程范式的再次进化/`（本系列第 8 篇）
> **上游依据**：[`CLAUDE.md`](../../../CLAUDE.md) · [`AGENTS.md`](../../../AGENTS.md) · 同系列 1-7 篇
> **下游接续**：本方案是**组织层级**抽象；具体落地仍以**阶段角色 Soul + 任务 Skill + Sub-agent 包装**三层范式为单元

---

## 0. 写前校准（写作前的诚实交代）

本文是**对专家规划方案的承接与锚定**，不是从零起草，也不是对原方案的推翻。为避免后续执行偏离，先把三处必须校准的事实写在最前面：

| # | 原方案表述 | 校准后的真实状态 | 校准证据 |
|---|---|---|---|
| 1 | 「Class Refactoring Expert — Agent Runtime Development IN PROGRESS」 | **Skill 已存在**（`.claude/skills/generic-class-refactor-expert/SKILL.md` + 16 个 references/），**无 Agent 运行时**。与 table-refactor-expert 同级 | `docs/architecture/ARCHITECTURE_DOC_RULES.md`、`.claude/skills/generic-class-refactor-expert/` |
| 2 | 「Chief Architect Agent」位于组织顶层 | 当前 `.claude/souls/architect/soul.md` 是 **Phase 阶段角色**（Align → Brainstorm → Explore），由 orchestrator 调度，**不是用户级常驻 Agent** | `.claude/souls/orchestrator/soul.md:5-25` |
| 3 | 「Refactoring Expert Family 含 Table / Class / API / Service / Aspire」 | 当前**仅 Table Refactoring Expert 已冻结 Skill v1.0**（Phase 7 CLOSED，2026-08-30），其余 4 个**全部未冻结** | `docs/universal/Phase-7-Final-Report.md:21` |

校准目的：把组织级规划从「愿景」对齐到「现状」。所有 Agent 描述后附 **状态标签**：`[FROZEN]` / `[PRODUCTION-VALIDATED]` / `[PILOT-VALIDATED]` / `[IN-PROGRESS]` / `[PLANNED]` / `[TBD]`。

---

## 1. 文档目的

本文用于指导：

- 首席架构师（架构方向决策）
- 系统架构师（领域边界、ADR）
- AI 工程师（Skill / Sub-agent / Soul 工程化）
- 软件工程师（与传统代码协作）
- 项目负责人（流水线编排、Human Gate）

**目标不是建设几个 AI 工具**，而是建立：

> **一个由多个专业 AI Agent 组成的虚拟软件研发组织，使 AI 大模型具备资深架构师、技术负责人和研发团队的系统工程能力。**

---

## 2. 第一章 战略目标

### 2.1 背景

企业级软件复杂度提升，传统工程痛点：

- 需求理解困难（业务规则跨域）
- 业务流程复杂（流程引擎 + 多租户）
- 系统设计依赖专家经验（不可规模化）
- 老系统维护成本高（v3.6 → v5.2 跨度大）
- 重构风险不可控（数据库治理经验已验证）
- 技术债持续累积（历史命名/索引/事务不一致）

单纯代码生成 AI 解决不了根因。**企业级软件开发核心不是写代码，而是「理解业务 → 设计系统 → 控制复杂度 → 持续演进」**。

### 2.2 AI 软件工程组织目标

```
AI Software Engineering Organization
                |
                ↓
具备完整软件生命周期能力
```

覆盖：

```
需求分析 → 业务分析 → 领域设计 → 界面设计
   → 架构设计 → 开发实现 → 重构优化 → 测试验证
   → 部署运行 → 持续演进
```

---

## 3. 第二章 总体架构模型

### 3.1 组织总览

```
                      Chief Architect Agent   [PLANNED — 见 §12.2 缺口]
                              |
              ---------------------------------
              |                               |
              ↓                               ↓

       产品业务体系                     软件工程体系

需求分析 Agent [PLANNED]             开发 Agent [PLANNED]
       |                                  |
AI业务分析 Agent [PLANNED]                |
       |                                  |
AI领域设计 Agent [PLANNED]                |
       |                                  |
AI界面设计 Agent [PLANNED]                |
       |                                  |
AI架构设计 Agent [PARTIAL]                |
       → `.claude/souls/architect/soul.md`    |
              + 阶段模式 Align/Brainstorm   ↓
                                  Refactoring Expert Family
                                  （见 §6.2 详情）
                          ------+-----+-----+-----
                          ↓     ↓     ↓     ↓     ↓
                       表级   类级   接口   服务   Aspire
                       详见 §12.1

                          测试 Agent [PARTIAL]
                          → `.claude/souls/tester/soul.md`
                          + `.claude/agents/jnpf-tester.md`

                          部署 Agent [PLANNED]
```

### 3.2 三层映射关系（必须分清）

本组织的「Agent」对应到本仓库工程实现时有**三种形态**，不能混用：

| 组织抽象 | 工程实现 | 已存在实例 |
|---|---|---|
| **业务型 Agent**（需求/分析/设计） | `.claude/souls/{role}/soul.md`（阶段角色） | `architect`、`planner`、`reviewer`、`reporter`、`tester`、`debugger`、`orchestrator`、`coder`（8 个 soul 已存在） |
| **专家型 Agent**（重构/治理） | `.claude/skills/{skill}/SKILL.md` + `references/` + `scripts/`（领域技能） | `table-refactor-expert`（FROZEN v1.0）、`generic-class-refactor-expert`（v6.0） |
| **任务型 Agent**（dispatch 单元） | `.claude/agents/{name}.md`（YAML frontmatter `tools:` + `skills:`） | `jnpf-debugger`、`jnpf-tester`、`session-summary-agent`（3 个已存在） |

**关键约束**：组织级 Agent 不能跨形态复用。`Class Refactoring Expert` 是 Skill 形态，其包装 Agent（如 `.claude/agents/table-refactoring-expert.md`）才属于任务型 Agent；专家本体不变。

---

## 4. 第三章 核心设计原则

### 原则 1：Agent 是岗位，不是工具

```
❌ AI Prompt → 执行任务

✅ 岗位职责
   → 专业能力
   → 知识体系
   → 工作流程
   → 质量标准
   → 交付物
```

每个 Agent 等价于企业研发团队中的一个**专业岗位**，有边界、有交接物、有 KPI。

### 原则 2：专业能力分离，工程治理统一

| 关注点 | 主管 Agent | 当前形态 |
|---|---|---|
| 数据库治理 | Table Refactoring Expert | Skill v1.0 `[FROZEN]` |
| 代码治理 | Class Refactoring Expert | Skill v6.0 `[PILOT-VALIDATED]` |
| API 治理 | API Refactoring Expert | `[PLANNED]` |
| 服务边界 | Service Refactoring Expert | `[PLANNED]` |
| 云原生演进 | Aspire Refactoring Expert | `[PLANNED]` |

**共享治理框架**（所有 Agent 必须接入）：

- Evidence（五标签体系 `[KNOWN]` `[COMPUTED]` `[INFERRED]` `[GUESS]` `[DESIGN]`）
- Review（5 维 × 3 级）
- Quality Gate（Hard Gate + Soft Gate）
- Change Record（Evidence Ledger + Closed Gate）
- Audit Trail（Change Catalog）

> 治理框架**本体已冻结**在 `docs/universal/Table-Refactoring-Expert-{Master-Spec, Execution-Manual}.md`，可直接复用。

### 原则 3：所有 AI 决策必须可验证

```
❌ AI 认为应该这样改

✅ Evidence
   → Analysis
   → Decision
   → Change
   → Validation
```

### 原则 4：AI 不替代架构决策，而增强架构能力

| 决策层级 | 角色 | 当前形态 |
|---|---|---|
| 架构最终决策 | 人类 Chief Architect / Technical Leader | 不可替代 |
| 架构方案生成 | AI Architect Agent | `.claude/souls/architect/soul.md` 阶段模式 |
| 架构执行 | AI Coder / Tester / Reviewer | `.claude/souls/{coder,tester,reviewer}/soul.md` |
| 架构验证 | AI Reviewer + 自动化扫描 | L0 Hooks + L1 Reviewer 双防线（见 `6、KIMI 涅槃重构.md`） |

---

## 5. 第四章 Agent 标准模型

### 5.1 Agent 六层模型

```
┌─────────────────────────┐
│ Identity                 │  身份定义 / 边界 / 不做什么
└─────────┬───────────────┘
          ↓
┌─────────────────────────┐
│ Capability               │  专业能力 / 输入输出契约
└─────────┬───────────────┘
          ↓
┌─────────────────────────┐
│ Knowledge                │  专业知识库（references/）
└─────────┬───────────────┘
          ↓
┌─────────────────────────┐
│ Workflow                 │  工作流程（5 步 / 状态机）
└─────────┬───────────────┘
          ↓
┌─────────────────────────┐
│ Evidence                 │  证据体系（五标签 + 阈值）
└─────────┬───────────────┘
          ↓
┌─────────────────────────┐
│ Quality Gate             │  验收标准（Hard Gate + DoD）
└─────────────────────────┘
```

### 5.2 三形态映射（同一六层模型的不同工程实现）

| 层 | 业务型 Agent（soul） | 专家型 Agent（skill） | 任务型 Agent（sub-agent） |
|---|---|---|---|
| Identity | `soul.md:1-50` 身份定义段 | `SKILL.md:1-50` description + Use When | YAML `name:` + `description:` |
| Capability | soul workflow 段 | `SKILL.md` Execution Protocol 段 | YAML `tools:` 列表 |
| Knowledge | `_shared/*.md` 自动注入 | `references/` 子目录 | `skills:` 字段引用 skill |
| Workflow | soul 中的阶段流转 | Execution Manual §3 | main Claude 决定何时 dispatch |
| Evidence | `_shared/assertion-discipline.md` 五标签 | Master Spec §11.1 五标签 | 由上游 skill 决定 |
| Quality Gate | soul 输出的 DoD | Master Spec §13.2 13 条 DoD | YAML description 中的不做什么 |

**示例**：表级重构专家的三形态对齐（现状）

- **专家型**：`.claude/skills/table-refactor-expert/SKILL.md`（345 行）
- **知识库**：`.claude/skills/table-refactor-expert/references/`（**待补齐**，见 §12.3）
- **任务型**（推荐包装）：`.claude/agents/table-refactoring-expert.md`（**未创建**，见 §12.3）

---

## 6. 第五章 Agent 分类体系

### 6.1 Business Intelligence Agents

#### 需求分析 Agent `[PLANNED]`

- **职责**：用户需求 → 业务目标 → 需求规格 → 业务范围
- **输出**：Requirement Specification、User Story、Acceptance Criteria
- **当前基础**：见 `openspec/specs/studio-requirement-spec-lifecycle/spec.md`（已有 spec，未做 Agent 化）

#### AI 业务分析 Agent `[PLANNED]`

- **职责**：行业规则、业务流程、角色权限建模
- **输出**：Business Model、Process Model
- **当前基础**：见 `openspec/specs/studio-clarification/spec.md`（Clarification Skill）

#### AI 领域设计 Agent `[PARTIAL]`

- **职责**：DDD 分析（Aggregate / Entity / Value Object / Domain Service）
- **输出**：Domain Model
- **当前基础**：`.claude/rules/triple-key-iron-law.mdc` 内的 R12 三元组已提供 Aggregate 边界规则；DDD 完整流程未冻结

### 6.2 Design Agents

#### AI 界面设计 Agent `[PLANNED]`

- **职责**：页面结构、用户流程、交互设计
- **输出**：UI Specification、Prototype Description

#### AI 架构设计 Agent `[PARTIAL]`

- **职责**：系统架构、技术选型、模块划分
- **输出**：Architecture Decision Record（ADR）
- **当前基础**：`.claude/souls/architect/soul.md`（148 行，阶段模式）；`docs/architecture/v52/` 13 篇技术内参是输入而非产出

---

## 7. 第六章 工程 Agent 家族（Engineering Agent Family）

### 7.1 开发 Agent `[PARTIAL]`

- **职责**：根据设计实现 Domain Code、Application Service、API、UI
- **当前基础**：`.claude/souls/coder/soul.md`（已存在阶段角色）；`.claude/rules/` 内 5 套编码铁律
- **必须遵守**：Coding Standard（`.claude/rules/`）、Architecture Rule（ARCHITECTURE_DOC_RULES）、Test Requirement（ARCH-01 layering）

### 7.2 表级重构专家 Agent `[PRODUCTION-VALIDATED → 等待 Agent 化]`

#### 定位

企业数据库架构治理专家。JNPF v5.2 274 张生产表的实际治理者。

#### 负责

```
Database Object → Schema / Index / Relationship / Tenant / Query / Lifecycle
```

#### 当前状态（已校准）

| 维度 | 状态 | 证据 |
|---|---|---|
| Universal Master Spec | `[FROZEN]` | `docs/universal/Table-Refactoring-Expert-Master-Spec.md`（917 行） |
| Execution Manual | `[FROZEN]` | `docs/universal/Table-Refactoring-Expert-Execution-Manual.md`（589 行） |
| JNPF Extension | `[FROZEN]` | `docs/universal/Table-Refactoring-Expert-JNPF-Extension.md` |
| Foundry Target Profile | `[FROZEN]` | `docs/universal/Table-Refactoring-Expert-Foundry-Target-Profile.md` |
| Universal Skill | `[FROZEN v1.0]` | `.claude/skills/table-refactor-expert/SKILL.md` |
| Production Validation | `[10/10 PASS]` | `docs/universal/Phase-8/Phase-8-最终关闭报告.md`（R2-COMP） |
| Agent 包装 | `[PENDING]` | `.claude/agents/table-refactoring-expert.md` 未创建 |

#### 工作流程（Skill 已实现）

```
Metadata Collection → 7 Dimension Analysis → Risk Classification
   → Hard Gate Check → Refactoring Proposal → Evidence Package → Validation
```

#### 输出（Skill 已实现）

`Table Refactoring Report`：

1. 表定位（Table Identity）
2. 业务含义（Business Meaning）
3. 架构分析（Architecture Context）
4. 风险等级（Risk R0-R5）
5. 重构建议（Refactoring Proposal）
6. 执行记录（Refactoring Flow）
7. 验证结果（13 DoDs）

### 7.3 类级重构专家 Agent `[PILOT-VALIDATED]`

- **当前基础**：`.claude/skills/generic-class-refactor-expert/`（SKILL.md + 16 references + empty scripts/）
- **已验证维度**：Lifetime / GC / Async / Exception / Performance / Type / Extensibility / Observability / Architecture（9 维）
- **生产验证**：`[TBD]` — 未在真实生产环境累计 KPI（与表级对比，类级缺 Phase 7/8 等价物）
- **包装 Agent**：`[PENDING]`

### 7.4 接口重构专家 Agent `[PLANNED]`

- **负责**：API 治理（Contract / DTO / Version / Compatibility）
- **当前基础**：`.claude/rules/` 内 `dynamic-api-controller` 规则、`iron-laws/architecture-redlines.md` R4-R8
- **缺口**：无独立 Master Spec / Skill

### 7.5 服务重构专家 Agent `[PLANNED]`

- **负责**：单体服务治理（Boundary / Dependency / Transaction / DDD）
- **当前基础**：`docs/architecture/MASTER-JNPF后端重构到Aspire微服务架构方案.md`、`docs/architecture/backend-modular-refactor-plan.md`
- **缺口**：无 Skill 化

### 7.6 Aspire 重构专家 Agent `[PLANNED]`

- **负责**：现代化演进（Monolith → Modular Monolith → Aspire → Cloud Native）
- **关注**：Service Boundary / Distributed Configuration / Observability / Deployment
- **当前基础**：`docs/architecture/MASTER-JNPF后端重构到Aspire微服务架构方案.md`（12KB）、`MASTER-JNPF后端重构到Aspire微服务架构实施方案.md`（9KB）

---

## 8. 第七章 测试 Agent `[PARTIAL]`

- **当前基础**：
  - `.claude/souls/tester/soul.md`（阶段角色）
  - `.claude/agents/jnpf-tester.md`（任务型 sub-agent）
  - `.claude/skills/jnpf-api-cli/`（自动 API 测试技能）
  - `scripts/test-hooks.mjs`（28 用例 hooks 验证）
- **覆盖**：Unit Test / Integration Test / Regression Test / API Test / Hook Test
- **缺口**：Performance Test / Security Test 未独立 Agent 化（依赖架构扫描 + L0 Hooks）

---

## 9. 第八章 部署 Agent `[PLANNED]`

- **负责**：CI/CD / Environment / Container / Monitoring
- **当前基础**：`docs/architecture/deployment/`、`backend/application/JNPF.API.Entry/Dockerfile`
- **缺口**：无 Agent 包装；当前部署依赖人工 + PowerShell 脚本

---

## 10. 第九章 Agent 协作流程

### 10.1 新系统开发流水线

```
需求分析 Agent [PLANNED]
       ↓
业务分析 Agent [PLANNED]
       ↓
领域设计 Agent [PARTIAL]
       ↓
架构设计 Agent [PARTIAL]
       ↓
开发 Agent [PARTIAL]
       ↓
测试 Agent [PARTIAL]
       ↓
部署 Agent [PLANNED]
```

### 10.2 老系统重构流水线（JNPF v5.2 当前路径）

```
Architecture Discovery          ← 已有 (docs/architecture/v52/)
       ↓
Table Refactoring Agent         ← Skill v1.0 已冻结，Agent 包装待做
       ↓
Class Refactoring Agent         ← Skill v6.0 已冻结，Agent 包装待做
       ↓
API Refactoring Agent           ← [PLANNED]
       ↓
Service Refactoring Agent       ← [PLANNED]
       ↓
Aspire Migration Agent          ← [PLANNED]
       ↓
Test Agent                      ← [PARTIAL]
       ↓
Deployment Agent                ← [PLANNED]
```

### 10.3 阶段对应（与本系列第 6 篇对齐）

老系统重构流水线对应 `6、KIMI 涅槃重构.md` 描述的 **Orchestrator 状态机**，但实现层落地方式不同：

| 维度 | 涅槃重构方案 | 本方案 |
|---|---|---|
| 调度方 | 外部 Python 状态机 | `.claude/souls/orchestrator/soul.md`（Claude Code 内置调度） |
| 交接物 | JSON Schema 文件级 | 优先 Markdown（人类可读），必要时 JSON |
| 角色切换 | 物理隔离会话 | 同会话内 soul 切换（依赖 Claude Code 的 sub-agent dispatch） |
| 质量门 | 脚本硬执行 | L0 Hooks（已实施）+ L1 Reviewer（已实施） |

**关键差异**：本方案接受 Claude Code 的会话模型约束（不要求物理隔离），但**保留文件级交接、Markdown 优先、双防线质量门**三个涅槃重构的核心原则。

---

## 11. 第十章 统一质量体系

所有 Agent 必须执行 **Evidence Driven Engineering**：

```
Decision
   必有
Evidence

Change
   必有
Validation

Completion
   必有
Acceptance
```

### 11.1 Evidence 五标签体系（已冻结）

源自 `Table-Refactoring-Expert-Master-Spec.md §11.1`，本仓库所有 Agent 强制使用：

| 标签 | 含义 | 证据要求 |
|---|---|---|
| `[KNOWN]` | 运行时数据 / 源码直接证据 | 必须可回溯到具体文件:行号 或 抓取命令输出 |
| `[COMPUTED]` | 由已知数据推导 | 推导过程需明示 |
| `[INFERRED]` | 多源推断 | 至少 2 个独立证据源 |
| `[GUESS]` | 假设 | 需明确标置信度（LOW/MED/HIGH） |
| `[DESIGN]` | 设计目标态 | 必须有反向 Evidence 或约束说明 |

### 11.2 Quality Gate 双防线（已实施）

| 层 | 实现 | 触发时机 |
|---|---|---|
| **L0 Hooks** | `.claude/settings.json` 注册 `guard-write` / `guard-bash` / `guard-finish` 等 | Write/Edit/Bash 时同步拦截 |
| **L1 Reviewer** | `.claude/souls/reviewer/soul.md` + 5 维 × 3 级审查 | 任务完成时审查 |

**禁区规则**（与 `Phase-7-Final-Report.md §七` 对齐）：

1. Hard Gate 不可简化
2. Evidence Sufficiency 阈值不可调低
3. 「不制造修改」能力不可移除
4. Core 不可知道 Extension 存在
5. Extension 不可改写 Core 判定
6. 不可新增「自动跳过 Hard Gate」开关

### 11.3 Change Record（已实施）

- 单一事实源：`docs/universal/Phase-8/JNPF-表级重构-登记表.csv`（机器可读）
- 人类可读：`docs/universal/Phase-8/JNPF-表级重构-技术变更目录.md`（59KB）
- 模板：`.claude/skills/generic-class-refactor-expert/references/P0-Evidence-Pack-template.md`

---

## 12. 第十一章 Agent 生命周期管理

每个 Agent 经历 **6 个阶段**：

```
Prototype
   ↓
Skill Validation        ← Pilot + Generic Validation
   ↓
Production Validation   ← Phase 8 等价物
   ↓
Agent Packaging         ← 任务型 sub-agent 包装
   ↓
Enterprise Usage        ← 进入日常流水线
   ↓
Continuous Evolution    ← 治理框架 + KPI 反馈
```

### 12.1 当前资产盘点（已冻结 / 已验证 / 在建）

| Agent | Prototype | Skill Validation | Production Validation | Agent Packaging | Enterprise Usage | Evolution |
|---|---|---|---|---|---|---|
| Table Refactoring | ✅ | ✅ Pilot 1-3 | ✅ Phase 8（93 表 / 0 事故） | ⏳ PENDING | — | — |
| Class Refactoring | ✅ | ✅ v6.0 | ⏳ TBD | ⏳ PENDING | — | — |
| API Refactoring | ✅ | ⏳ | ⏳ | ⏳ | — | — |
| Service Refactoring | ✅ | ⏳ | ⏳ | ⏳ | — | — |
| Aspire Refactoring | ✅ | ⏳ | ⏳ | ⏳ | — | — |
| Test | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| Debug | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| Code Review | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| Report | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| Plan | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| Architecture | ✅ | ✅ | ✅ | ✅ | ✅ | ⏳ |
| Requirement Analysis | ✅ | ⏳ | ⏳ | ⏳ | — | — |
| Domain Design | ✅ | ⏳ | ⏳ | ⏳ | — | — |
| Deployment | ✅ | ⏳ | ⏳ | ⏳ | — | — |

> 说明：「Enterprise Usage ✅」代表已有 `.claude/agents/{name}.md` 包装；8 个 soul 角色均已通过 `Phase 1-7` 多场景使用。

### 12.2 关键缺口

#### 缺口 1：表级重构 Agent 包装

- **现状**：Skill v1.0 已冻结，生产验证完成；缺任务型 sub-agent
- **必须产物**：`.claude/agents/table-refactoring-expert.md`（YAML frontmatter `tools:` + `skills: table-refactor-expert`）
- **模板**：照搬 `.claude/agents/jnpf-debugger.md:1-7`
- **优先级**：P0（用户已批准 Skill → Agent 升级路径）

#### 缺口 2：类级重构 Agent 包装

- **现状**：Skill v6.0 已冻结，无生产 KPI 基线
- **必须产物**：`.claude/agents/class-refactoring-expert.md`
- **前置**：建议补一次与 Phase 8 等价的 Production Validation（小规模，至少 10 个真实类）
- **优先级**：P1

#### 缺口 3：Chief Architect Agent

- **现状**：`.claude/souls/architect/soul.md` 是阶段角色，**不是用户级常驻 Agent**
- **用户级 Agent 缺失含义**：用户无法直接以「首席架构师」身份发起对话
- **必须决策**：是用 Sub-agent 形态包装，还是用 Cursor/OpenCode Agent 形态？
- **优先级**：P2（需要先选定运行时）

### 12.3 表级重构 Skill 的 references/ 缺口

为支撑 Agent 包装，Skill 必须自给自足（参考 `generic-class-refactor-expert/` 的 references 丰富度）：

```
.claude/skills/table-refactor-expert/
├── SKILL.md                       # 现有（345 行）
├── references/                    # 待补齐
│   ├── 00-VERSION.md              # v1.0 FROZEN 2026-08-30
│   ├── 01-Master-Spec-Summary.md  # 引用 docs/universal/...
│   ├── 02-Execution-Manual-Summary.md
│   ├── 03-JNPF-Extension.md
│   ├── 04-Foundry-Target-Profile.md
│   ├── 05-Phase-7-8-Evidence.md   # 冻结报告 + 闭环证据
│   ├── 06-Change-Catalog-Schema.md # 从 Registry CSV 抽字段 schema
│   ├── 07-Risk-Matrix-Template.md
│   ├── 08-Evidence-Pack-Template.md
│   ├── 09-Golden-Example-01-NoChange.md   # Pilot 1 BASE_AI_PIPELINE
│   ├── 10-Golden-Example-02-IndexRefactor.md # Pilot 2 BASE_KNOWLEDGE_EDGE
│   ├── 11-Golden-Example-03-HardGate.md    # Pilot 3 FLOW_TASK HG #5
│   └── 12-Hard-Gate-Catalog.md    # Master Spec §10.3 10 条 + 触发示例
├── scripts/                       # 空（与 class-level 一致）
└── CHANGELOG.md                   # 待补
```

---

## 13. 第十二章 未来演进路线

### Phase A：Agent Framework 建立（建议 1-2 周）

- 决策「用户级 Agent」的运行时（Claude Project / Cursor Agent / OpenCode sub-agent / 自建 orchestrator）
- 统一三形态映射规范（本方案 §3.2 / §5.2）
- 定义 `.claude/agents/{name}.md` 包装模板（基于 `jnpf-debugger.md`）

### Phase B：第一批工程 Agent（建议 1-2 月）

- 包装 Table Refactoring Expert Agent v1.0（P0）
- 包装 Class Refactoring Expert Agent v1.0（P1，前提：补 Production Validation）
- 冻结 API / Service / Aspire Refactoring Expert 的 Master Spec（仅 1 个 Pilot 即可启动）

### Phase C：扩展家族（建议 2-3 月）

- 完成 API / Service / Aspire 三个 Agent 的 Skill + 包装
- 建立 **Agent Registry**：`.claude/agents/REGISTRY.md` 列出所有可用 Agent + 触发条件

### Phase D：完整 AI 软件工厂（建议 6 月+）

- 业务型 Agent（需求 / 业务 / 领域 / 界面）全部 Agent 化
- 流水线编排从手工 → orchestrator 自动驱动
- Phase 8 等价物的生产 KPI 实时采集

---

## 14. 第十三章 最终战略定位

### 14.1 范式转变

```
传统：
  人类团队 + 工具

  ↓

AI 软件工程组织：
  多个专业 Agent
        ↓
  自动化软件生命周期
```

### 14.2 与既有工程实践的关系

本方案**不替换**已有的工作流：

| 已有 | 关系 |
|---|---|
| `.claude/rules/*` 编码铁律 | 保留，是 Agent 的运行时输入 |
| L0-L3 Hooks（L0-L11） | 保留，是所有 Agent 的强制守卫 |
| `.claude/souls/*` 8 个阶段角色 | 保留，是业务型 Agent 的工程实现 |
| `.claude/skills/*` 领域技能 | 保留，是专家型 Agent 的工程实现 |
| `.claude/agents/*` 任务单元 | 扩展，是任务型 Agent 的工程实现 |
| `Phase A/B/C` 开发态文档 | 保留，是 Agent 流水线的输入约束 |

**本方案只是把它们抽象为「AI 软件工程组织」概念，不引入新文件、新规则、新 Hooks**（除非必要）。

### 14.3 风险与未决议题

| 风险 | 描述 | 缓解 |
|---|---|---|
| **运行时锁定** | 用户级 Agent 需要选定运行时（Claude / Cursor / OpenCode / 自建） | Phase A 决策前不开始用户级 Agent 包装 |
| **形态混淆** | Soul / Skill / Sub-agent 三种形态边界模糊 | 持续以 §3.2 / §5.2 为准；新文件入库前审查 |
| **过度抽象** | 组织级概念易脱离工程实现 | 每个 Agent 必须有 §12.1 的状态矩阵行 |
| **空泛分类** | 5 大类 Agent 中部分目前仅是命名 | 不接受「只有名字没有资产」的 Agent 入库 |
| **跨系列割裂** | 本系列 1-7 篇有重叠讨论（Kimi 涅槃 / 专家组建议） | 通过附录 A 持续交叉引用 |

### 14.4 结论

> 让 AI 大模型具备资深系统架构师、技术负责人和完整研发团队的能力，在人类架构师治理下，实现企业级软件系统从 0→1 建设，以及旧系统持续重构优化的工业化流程。

**本方案作为「AI Software Engineering Organization Architecture Baseline v1.0」**，供所有架构师和 AI 工程师作为后续 Agent 建设、开发和执行的统一规范。

**冻结状态**：v1.0-draft（待 Phase A 决策后升级为 v1.0-final）。

---

## 附录 A：本方案与现有系列文档的映射

| 本方案章节 | 现有文档 | 关系 |
|---|---|---|
| §3 总体架构模型 | `1、重构rules、hooks、工作流水线为fugu聚合智能体.md` | 上层抽象（拒绝 Fugu 过度设计，采纳其角色化思路） |
| §3.2 三层映射 | `6、KIMI 涅槃重构.md:14-26` V1.0/V3.0 对比 | 采纳其「外部状态机 + 内部专家」原则，适配 Claude Code 约束 |
| §4 原则 2 治理统一 | `6、KIMI 涅槃重构.md:18-25` 双防线 | 直接复用 |
| §4 原则 3 Evidence | `_shared/assertion-discipline.md` | 直接复用 |
| §10.3 阶段对应 | `7、专家组的五条建议.md` 物理隔离 | 接受其原则，放弃其物理隔离手段 |
| §11 质量体系 | `5、架构上的拦截器和中间件.md` | 直接复用 L0 Hooks 设计 |
| §12.1 资产盘点 | `4、AI大模型的涅槃重生.md` 角色灵魂铸造 | 直接引用其 6 角色（已扩到 8） |
| §12.3 references 缺口 | 无 | 新建议（对齐 `generic-class-refactor-expert/`） |

## 附录 B：与现有系列关键差异

| 维度 | 涅槃重构方案（V3.0） | 本方案 |
|---|---|---|
| 调度器 | Python 外部状态机 | Claude Code 内置 + soul 调度 |
| 角色数量 | 6（架构/规划/开发/测试/审查/汇报） | 8（含 debugger、orchestrator） |
| Agent 形态 | 仅角色灵魂 | Soul + Skill + Sub-agent 三形态 |
| 演进路径 | 单线 V1→V3 | Phase A/B/C/D 分阶段，每阶段可选 |
| 与重构专家关系 | 未涉及 | Refactoring Expert Family 是 Engineering Agent 子家族 |
| 与生产验证关系 | 未涉及 | Table Refactoring Expert 的 Phase 8 成果作为范式基线 |

## 附录 C：术语对照表

| 本方案术语 | 工程实现 | 备注 |
|---|---|---|
| 业务型 Agent | `.claude/souls/{role}/soul.md` | 阶段角色，由 orchestrator 调度 |
| 专家型 Agent | `.claude/skills/{skill}/` | 领域技能，独立调用 |
| 任务型 Agent | `.claude/agents/{name}.md` | YAML frontmatter，main Claude dispatch |
| Agent 包装 | 把 Skill 暴露为 Sub-agent | 例：`.claude/agents/table-refactoring-expert.md` |
| Evidence Ledger | `Table-Refactoring-Expert-Master-Spec.md §11.1` | 五标签 + 阈值 |
| Hard Gate | `Master Spec §10.3`（10 条） | 不可简化 |
| Pilot | Phase 6 三表验证 | BASE_AI_PIPELINE / BASE_KNOWLEDGE_EDGE / FLOW_TASK |
| Production Validation | Phase 8（93 表 / 17 Batch） | R1+R2-COMP 双轨 |
| Frozen | Phase 7 + 9/9 Exit Gate | Skill 不再增删功能 |

## 附录 D：本方案 v1.0 升级条件（→ v1.0-final）

- [ ] Phase A 运行时决策已批准
- [ ] 表级重构 Expert Agent 包装完成（`.claude/agents/table-refactoring-expert.md`）
- [ ] Skill `references/` 缺口（§12.3）补齐
- [ ] 类级重构 Production Validation 完成（至少 10 真实类 KPI）
- [ ] API/Service/Aspire 三 Agent 的 Master Spec 至少 1 个已冻结

---

## 附录 E：版本信息

| 版本 | 日期 | 主要变更 | 状态 |
|---|---|---|---|
| v1.0-draft | 2026-08-30 | 专家规划方案承接 + 现状校准 + 缺口分析 | 当前 |

**主文件维护者**：本文档随 Phase A/B/C/D 推进而更新。
**变更控制**：任何重大结构调整必须先经首席架构师审批。
