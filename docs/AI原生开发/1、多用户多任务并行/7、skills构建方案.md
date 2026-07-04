# JNPF-AI 低代码平台：十大 Skill 完整落地计划

> 版本：v1.0 | 日期：2026-07-03 | 作者：技术架构组
>
> 本文档基于以下四份核心设计文档的全量共识编写：
> - `3、skills构建的知识.md`（认知双核定义 + 产品经理/系统需求分析师 Skill 的本质）
> - `5、真正的agent流水线和运行时设计.md`（事件驱动 DAG + A2A 协议 + 统一 IR + 稳定性门控）
> - `6、agent运行时.md`（Multi-Agent Harness：调度、工具治理、状态分层、成本控制）
> - `7、skills构建方案.md`（本文档）

---

## 目录

1. [总体架构与数据流](#1-总体架构与数据流)
2. [分阶段实施路线图](#2-分阶段实施路线图)
3. [十大 Skill 逐一详细设计](#3-十大-skill-逐一详细设计)
4. [关键技术决策记录](#4-关键技术决策记录)
5. [数据模型设计草案](#5-数据模型设计草案)
6. [关键风险与缓解措施](#6-关键风险与缓解措施)

---

## 1. 总体架构与数据流

### 1.1 架构总览

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      JNPF-AI 平台架构（三层分离）                         │
│                                                                          │
│  ┌────────────────────── 认知层 (Cognitive Layer) ─────────────────────┐ │
│  │  产品经理(ToT)  需求分析师(IOI)  架构设计  总体设计  数据库  UI 设计  │ │
│  │       开发 Skill    测试 Skill    部署 Skill    Bug 修复 Skill         │ │
│  └───────────────────────────┬─────────────────────────────────────────┘ │
│                              │ A2A 协议                                   │
│  ┌────────────────────── 执行层 (Harness Layer) ──────────────────────┐  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │  │
│  │  │ Orchestrator  │  │ Tool Registry │  │ 软路由资源调配器          │  │  │
│  │  │ (事件驱动DAG) │  │ (MCP 网关)    │  │ L1租户路由 + L2项目路由   │  │  │
│  │  └──────────────┘  └──────────────┘  └──────────────────────────┘  │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │  │
│  │  │ Token Budget  │  │ 沙箱管理器    │  │ 可观测性 / Trace 收集器   │  │  │
│  │  │ (四级降级)    │  │ (生命周期)    │  │ (四层 Eval Pipeline)     │  │  │
│  │  └──────────────┘  └──────────────┘  └──────────────────────────┘  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                              │                                             │
│  ┌────────────────────── 资产层 (Asset Layer) ────────────────────────┐   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │   │
│  │  │ IR 事件流     │  │ 稳定性判定器  │  │ 约束传播引擎              │  │   │
│  │  │ (追加不可变)  │  │ (SA步骤完成度)│  │ (规则引擎+语义检测)       │  │   │
│  │  └──────────────┘  └──────────────┘  └──────────────────────────┘  │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │   │
│  │  │ IR 投影引擎   │  │ 种子数据库    │  │ Schema Registry           │  │   │
│  │  │ (当前状态快照)│  │ (领域元数据)  │  │ (IR版本兼容校验)          │  │   │
│  │  └──────────────┘  └──────────────┘  └──────────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌──────────────────── 现有资产（直接复用）────────────────────────────┐   │
│  │  LLM Gateway(6 Provider) | SA 9步服务(TypeScript) | 前端IR类型体系  │   │
│  │  5阶段Pipeline底层调度  |  数据库表(sa_skeleton/sa_event_spec)       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 统一 IR 作为唯一数据契约

统一 IR（Intermediate Representation）是整套系统的**唯一数据语言**。所有 Skill 之间的通信、所有 SA 步骤的产出，都以 IR 事件的形式追加到事件流中，任何组件都不持有独立的私有数据副本。

**IR 分层模型：**

```
IR-0  骨架草案       ← 产品经理 Skill 产出（业务事件 + 角色矩阵 + 实体草案）
IR-1  事件规格       ← 系统需求分析师 Skill 产出（字段级规格 + 业务规则 + IOI不变量）
IR-2  设计模型       ← 架构/总体/数据库/UI 设计 Skill 产出（架构图 + DDL + 页面IR）
IR-3  实现制品       ← 开发/测试 Skill 产出（可编译代码 + 测试脚本 + 测试报告）
IR-E  错误与修复事件 ← Bug修复 Skill 产出（缺陷定位 + 修复补丁 + 受影响片段标记）
```

**IR 生长流水线（同一棵树上逐圈增长的年轮）：**

```
用户原始需求
    │
    ▼ IR-0 事件: SkeletonCreated
┌────────────┐         IR-0 片段写入 sa_skeleton.ir_content
│ 产品经理    │──────────────────────────────────────────►
└────────────┘         发布 A2A: project.{T}.{P}.skeleton-ready
    │
    ▼ 订阅 skeleton-ready，获取 IR-0
┌────────────────┐     IR-1 事件: EventSpecConfirmed(×N)
│ 系统需求分析师  │──────────────────────────────────────►
└────────────────┘     稳定性门控：9个SA步骤全部 Completed
    │
    ▼ 订阅 SA_Step_Completed(Scope~UI)，片段逐步稳定
┌─────────────────────────────────────────────────────┐
│  架构设计 + 总体设计 + 数据库设计 + UI设计（并行）      │──► IR-2
└─────────────────────────────────────────────────────┘
    │
    ▼ 订阅 DesignStabilized
┌──────────────┐     IR-3 事件: CodeGenerated + TestGenerated
│ 开发 + 测试  │──────────────────────────────────────►
└──────────────┘
    │
    ▼ 订阅 TestPassed
┌──────────┐     部署成功 → DeploymentCompleted 事件
│ 部署      │──────────────────────────────────────────►
└──────────┘
    │
    ▼ 异常/缺陷 → BugReported 事件
┌──────────┐     沿事件流回溯 → 只重激活受影响 Skill
│ Bug修复   │──────────────────────────────────────────►
└──────────┘
```

### 1.3 语义片段稳定性门控机制

**核心原则（来自文档共识）：稳定性由 SA 步骤完成度自动判定，不由 LLM 主观判断。**

```
                    SA步骤完成度判定器
                         │
 IR片段状态机：           │
 draft ──► in-progress ──► stable ──► locked
            │                            │
            │ SA_Step_Completed 事件      │ 用户显式锁定
            │ 触发状态升级                │ （不可再修改）
            ▼                            ▼
       发布 FragmentStabilized       发布 FragmentLocked
       事件，下游 Skill 可订阅消费    事件，约束传播引擎记录
```

下游 Skill 的激活规则：
- **激活条件**：订阅的 IR 片段类型全部达到 `stable` 或 `locked` 状态
- **触发方式**：Orchestrator 监听稳定性事件，自动派发任务到对应 Skill
- **禁止模式**：Skill 不得主动拉取 IR 状态；必须被动接收事件驱动

### 1.4 与现有 JNPF-AI 组件的对接关系

| 现有组件 | 改造策略 | 对接点 |
|---|---|---|
| 9个SA Agent（TypeScript） | **不重写，封装为被驱动的内部流程** | 系统需求分析师 Skill 通过 `SAOrchestrator` 顺序调度，每步完成后追加 `SA_Step_Completed` 事件 |
| 前端 IR 模型（FormPageIR等） | **直接作为 IR-2 UI 设计层的输出格式** | UI 设计 Skill 的产出直接符合现有 FormPageIR/ListPageIR Schema |
| 前端 6个Agent + Skills管理界面 | **注册新 Skill，实现 `IBaseSkill` 接口** | 新增 Skill 只需声明信息需求，Harness 自动处理激活和路由 |
| LLM Gateway（6个Provider） | **直接复用，增加模型路由策略** | Token Budget 管理器按任务复杂度自动路由到合适 Provider |
| 5阶段 Pipeline Orchestrator | **改造底层调度为发布/订阅模式** | 保留调度能力，替换串行触发为事件监听触发 |
| `sa_skeleton` / `sa_event_spec` | **增加 ir_events 字段，扩展为事件溯源投影表** | DDL 改造见第5节 |

---

## 2. 分阶段实施路线图

### 阶段总览

```
第1阶段（W1-W2）: IR 基础设施 + 软路由骨架
第2阶段（W3-W4）: 产品经理 Skill + 系统需求分析师 Skill（含SA九步改造）
第3阶段（W5-W6）: 架构/总体/数据库/UI 设计 Skill（并行交付）
第4阶段（W7-W8）: 开发 + 测试 Skill（代码生成链路）
第5阶段（W9-W10）: 部署 + Bug修复 Skill + 全链路联调
第6阶段（W11-W12）: 生产加固（Harness 成本控制 + 评估体系 + 可观测性）
```

---

### 阶段一：IR 基础设施与多租户骨架（W1-W2）

> **可执行展开文档（优先阅读）：** [`8、全链条第一阶段开发计划.md`](./8、全链条第一阶段开发计划.md)  
> 核心策略：**以「提交需求」页为 E2E 集成中枢**，每完成一个后端组件，当天在浏览器可验收；阶段一不做「后台先建完再对接前端」。

**核心目标：** 建立统一 IR 数据契约、事件溯源存储、A2A 事件总线、两级软路由骨架，所有后续 Skill 的数据通道就位；**同步交付 IR 观测台（Pipeline Observatory）嵌入提交需求页**。

**组件清单：**

| 组件 | 描述 | 负责方 |
|---|---|---|
| `ir-schema/v1` | 统一 IR Schema 定义（JSON Schema + TypeScript 类型），覆盖 IR-0 到 IR-3 | 平台组 |
| `ir-event-store` | IR 事件追加存储（SQL Server 的 `ir_events` 表，含投影视图） | 后端组 |
| `ir-projection-engine` | 从事件流重建当前 IR 状态的投影引擎（TypeScript 服务） | 后端组 |
| `stability-gate` | 语义片段稳定性判定器（SA步骤完成度 → 状态升级） | 后端组 |
| `a2a-event-bus` | 基于 SQL Server Service Broker（MVP）或 RabbitMQ（生产）的事件总线，Topic 命名空间隔离 | 基础组 |
| `soft-router` | 两级软路由：L1 租户路由 + L2 项目路由，路由表存 etcd | 基础组 |
| `tenant-sandbox-manager` | 沙箱生命周期管理（创建/热复用/归档/销毁） | 基础组 |
| `schema-registry` | IR Schema 版本注册与兼容性校验（初期用 JSON Schema Validator 代替） | 平台组 |

**关键任务依赖：**
```
定义 ir-schema/v1 ──► 建立 ir-event-store ──► 实现 ir-projection-engine
                                               └──► 实现 stability-gate
定义 etcd 路由表结构 ──► 实现 soft-router ──► 实现 tenant-sandbox-manager
```

**产出物：**
- `backend/modularity/JNPF.AI/IR/` 目录：IR Schema TypeScript 定义 + C# 镜像类型
- `ir_events` / `ir_fragment_snapshots` 数据库表 DDL（见第5节）
- etcd 路由表结构文档
- 事件总线 Topic 命名规范文档

**验收标准（可测试）：**
1. 向 `ir_events` 追加一个 `SkeletonCreated` 事件，投影引擎能在 200ms 内重建出正确的 IR-0 快照
2. 两个不同 tenantId 的项目请求，软路由能路由到不同沙箱实例，不发生数据交叉
3. SA步骤完成度判定器：模拟 9个 `SA_Step_Completed` 事件后，对应 IR 片段状态变更为 `stable`
4. IR Schema 版本校验：发布不兼容格式的事件时，Schema Registry 拒绝并返回错误信息

---

### 阶段二：产品经理 Skill + 系统需求分析师 Skill（W3-W4）

> **可执行展开文档（优先阅读）：** [`9、全链条第二阶段开发计划.md`](./9、全链条第二阶段开发计划.md)  
> 核心策略：**延续阶段一 E2E 中枢**；Day 1 将「模拟 SkeletonCreated」替换为 `pm-skill` 真实推理；UI 与 IR 观测 API 不变。

**核心目标：** 打通从用户原始需求到 SA 九步完整产出的主干链路，验证"认知双核"协作范式。

**组件清单：**

| 组件 | 描述 |
|---|---|
| `pm-skill` | 产品经理 Skill：ToT 推理引擎 + 领域评分器 + FSM 生成 + IR-0 发布 |
| `domain-knowledge-mcp` | 领域元数据种子 MCP Server（行业模式子图 + 业务术语表 + 标准ER子模型） |
| `sa-orchestrator` | 驱动 9个 SA Agent 顺序执行，每步完成追加 `SA_Step_Completed` 事件 |
| `analyst-skill` | 系统需求分析师 Skill：IOI 框架 + 事件精炼 + 上下文裁剪 + IR-1 发布 |
| `ioi-validator-mcp` | IOI 不变量校验 MCP Server（前置/后置条件 + 全局约束验证） |
| `context-builder` | IR 上下文构建器：根据 Skill 信息需求声明，从事件流提取精确够用的片段 |

**关键任务依赖：**
```
阶段一完成 ──► pm-skill（依赖 ir-event-store + a2a-event-bus）
              └──► domain-knowledge-mcp（独立开发，并行）
pm-skill 验收通过 ──► sa-orchestrator 改造（原 SA Agent 封装）
                    └──► analyst-skill（依赖 sa-orchestrator + ioi-validator-mcp）
```

**产出物：**
- `pm-skill/` 完整实现（含 ToT 推理、领域评分器、IR-0 Schema 生成）
- `domain-knowledge-mcp/` 种子数据 MCP Server（含 5 个行业领域初始种子包）
- `sa-orchestrator/` 改造版（原 9个 SA TypeScript Agent 的调度包装器）
- `analyst-skill/` 完整实现（含 IOI 框架、事件精炼、SA步骤驱动）
- IR-0 和 IR-1 Schema 通过 Schema Registry 注册

**验收标准（可演示）：**
1. 输入一份 800-1500 字的制造业/人事/工程管理原始需求描述
2. 产品经理 Skill 在 3 分钟内产出合法 IR-0（包含 6-15 个业务事件、完整角色矩阵、3-8 个实体草案）
3. 系统需求分析师 Skill 自动驱动 SA 9步，全部 `SA_Step_Completed` 事件写入事件流
4. IR-1 产出包含字段级规格、IOI 不变量声明，可被下游 Skill 消费
5. 两个并发用户提交需求，各自生成的 IR 事件流完全隔离，互不干扰

> **质量红线（v1.1）：** 阶段二除功能验收外，须满足 [`9、全链条第二阶段开发计划.md`](./9、全链条第二阶段开发计划.md) **§14** 四条 NFR（日志 / 并发隔离 / 完整性 / 泄漏）及 **D13–D16**。若 W4 末仍有 ≥2 项红灯，**不进入阶段三**，先执行 [`10、Phase2.5运行时加固施工包.md`](./10、Phase2.5运行时加固施工包.md)。

---

### 阶段三：设计类 Skill 并行交付（W5-W6）

> **可执行展开文档（优先阅读）：** [`11、全链条第三阶段开发计划.md`](./11、全链条第三阶段开发计划.md)  
> 核心策略：**订阅 AnalysisCompleted** 触发设计四 Skill；**Day 1 必须先合 LLM 门禁**（`SkillLlmBudgetGuard`）；UI 与 IR 观测 API 模式不变。  
> 质量红线：**五条生命线**（日志 / 并发隔离 / 业务边界 / 泄漏 / **LLM 调用控制**）见文档 11 §15。

**核心目标：** 基于稳定的 IR-1，并行交付架构设计、总体设计、数据库设计、UI设计四个 Skill。

**组件清单：**

| 组件 | 描述 | 并行度 |
|---|---|---|
| `architect-skill` | 架构设计 Skill：ToT 搜索架构模式 + 技术选型决策树 + IR-2 架构片段 | 并行 |
| `system-design-skill` | 总体设计 Skill：一致性校验 + 接口契约 + 模块划分 | 串行（依赖架构完成） |
| `db-design-skill` | 数据库设计 Skill：DDL生成 + ER图 IR + 范式化建议 | 并行 |
| `ui-design-skill` | UI设计 Skill：FormPageIR/ListPageIR 生成 + 交互协议 | 并行 |
| `constraint-engine` | 约束传播引擎（初期：基于规则；检测分层依赖方向 + 公共类绕过） | 共享组件 |
| `seed-data-mcp` | 扩展种子库：JNPF 已有的企业管理模板 + 数据字典 + 校验规则 | 复用 |

**架构师 Skill 与总体设计 Skill 的协作时序：**
```
IR-1 全部稳定
    ├──► architect-skill（订阅 EventSpecStabilized）
    │         └── 产出 ArchitectureDecisionRecorded 事件
    ├──► db-design-skill（订阅 EventSpecStabilized）
    │         └── 产出 DDLStabilized 事件
    └──► ui-design-skill（订阅 EventSpecStabilized）
              └── 产出 UIDesignStabilized 事件
    
    三者全部完成后 ──► system-design-skill
                         │ 一致性校验 + 冲突消解
                         └── 产出 SystemDesignLocked 事件（IR-2 完成）
```

**验收标准：**
1. 架构 Skill 对同一份 IR-1，至少生成 2 个候选架构方案（如分层架构 vs CQRS），并给出评分理由
2. 数据库 Skill 产出的 DDL 可直接在 SQL Server 执行（无语法错误），外键关系与 IR-1 实体草案一致
3. UI 设计 Skill 产出的 FormPageIR 符合现有前端 IR 类型体系（无需格式转换可直接渲染）
4. 约束传播引擎：数据库层调用上层 Controller（分层违规）时，自动生成冲突报告

---

### 阶段四：开发 + 测试 Skill（W7-W8）

**核心目标：** 基于完整 IR-2，自动生成可编译的 JNPF 模块代码和对应测试脚本。

**组件清单：**

| 组件 | 描述 |
|---|---|
| `developer-skill` | 开发 Skill：基于 IR-2 + JNPF `.vm` 模板生成 C# Service + 前端 Vue3 页面 |
| `code-sandbox` | 代码生成沙箱：`dotnet build` 验证 + ESLint 检查（隔离环境） |
| `tester-skill` | 测试 Skill：从 IR-1 事件规格推导测试场景 + 生成 E2E/单元测试脚本 |
| `test-runner-mcp` | 测试执行 MCP Server：隔离运行测试 + 收集结果 + 写入 IR-3 |
| `arch-guard` | 架构守卫：检查生成代码是否遵循分层规则和公共类约定 |

**重要约束：**
- 开发 Skill **必须优先驱动 `.vm` 模板生成代码**，禁止在已有模板覆盖文件上直接修改（对应架构红线 R3）
- 所有 API 通过 `IDynamicApiController` 自动映射，**禁止生成 Controller 代码**（对应架构红线 R1）
- 多租户过滤（`ITenantFilter`）必须在生成的每个 Service 中声明（对应架构红线 R4）

**验收标准：**
1. 对"请假审批"场景，开发 Skill 在 5 分钟内生成完整 JNPF 模块（含 Service + Entity + 前端页面 IR）
2. `dotnet build` 零 error 通过（允许警告）
3. 测试 Skill 从 IR-1 自动推导出至少 5 个测试场景（含边界条件和异常分支）
4. 架构守卫检测到分层违规时，测试 Skill 的测试报告标记为 `FAILED`，阻止 IR-3 进入 stable 状态

---

### 阶段五：部署 + Bug修复 Skill + 全链路联调（W9-W10）

**核心目标：** 完成全链路闭环，验证正向生成和反向修复在同一 IR 事件流上的一致性。

**组件清单：**

| 组件 | 描述 |
|---|---|
| `deploy-skill` | 部署 Skill：JNPF 应用发布 + 功能开关管理 + 部署验证 |
| `bugfix-skill` | Bug修复 Skill：缺陷回溯 + 最小化影响范围分析 + 增量修复 IR 事件 |
| `ir-diff-engine` | IR 差分引擎：识别修复导致的 IR 变更，标记需要重算的下游片段 |
| `e2e-integration-tests` | 全链路端到端测试套件（覆盖"请假审批"完整场景） |

**Bug 修复的事件溯源回溯机制：**
```
线上缺陷报告（BugReported 事件）
    │
    ▼
bugfix-skill 订阅 BugReported
    │ 查询 ir_events 事件流，找到相关 IR 片段
    │ 分析：缺陷注入在哪一层？
    │   IR-1: 规格遗漏（触发 SA 步骤重跑）
    │   IR-2: 设计错误（触发设计 Skill 重算）
    │   IR-3: 代码错误（触发开发 Skill 局部重生成）
    │
    ▼ 追加 BugRootCauseLocated 事件（含精确 IR 片段 ID）
    │
    ▼ 追加 AffectedFragmentsMarked 事件
    │ 标记：哪些 IR 片段状态退回 in-progress
    │
    ▼ 只激活受影响片段的下游 Skill 重算
    │ 不影响已 locked 的无关 IR 片段
    │
    ▼ 追加 BugFixed 事件 + 修复后的增量 IR 事件
```

**验收标准（端到端演示）：**
1. 全链路演示：输入原始需求 → 产品经理 → 需求分析（SA9步） → 架构+数据库+UI → 开发 → 测试 → 部署，全程在一个 projectId 的事件流上完成
2. 演示时间旅行：查询历史 IR 快照，能正确还原任意时刻的系统状态
3. 模拟一个字段级 Bug：修复 Skill 只重激活数据库设计 + 开发 + 测试三个 Skill，架构和 UI 设计的 IR 不变
4. 两个并发项目全程无数据串味（通过独立 IR 事件流 Trace ID 验证）

---

### 阶段六：生产加固（W11-W12）

**核心目标：** 使系统从"能跑"升级为"企业级可用"。

> **与阶段二 / Phase 2.5 的分工（必读）：**  
> 运行时能力按三层划分，避免与阶段二功能开发混做或重复建设：
>
> | 层级 | 文档 | 范围 |
> |---|---|---|
> | **Skill-local Harness（MVP 门禁）** | [`9、全链条第二阶段开发计划.md`](./9、全链条第二阶段开发计划.md) **§14** | 结构化日志 + runId、`TenantGuard`、`SkillRunGuard`、per-project 并行 ≤5、CT 透传、AnalysisCompleted 完整性门禁、SSE/前端泄漏防护 |
> | **Phase 2.5 运行时加固（条件触发）** | [`10、Phase2.5运行时加固施工包.md`](./10、Phase2.5运行时加固施工包.md) | 租户 pipeline 配额、`SkillExecutionScope`、sa-service 租户隔离与结构化日志、`inferred` soft-block |
> | **平台级 Multi-Agent Harness（本文档阶段六）** | 本节 + [`6、agent运行时.md`](./6、agent运行时.md) | Token Budget 四级降级、OpenTelemetry 全链路、etcd 软路由、Eval Pipeline、沙箱扩缩容 |
>
> **阶段六不重复实现** §14 已交付的 Skill-local 能力；阶段六在其之上叠加**跨 Skill、跨租户、跨进程**的平台级治理。

**加固清单：**

| 加固项 | 具体措施 |
|---|---|
| Token Budget | 实现四级降级策略（绿/黄/红/熔断），按租户设置每日配额 |
| 四层 Eval Pipeline | 实现组件评估 + 轨迹评估 + 任务完成度 + 端到端业务效果评估 |
| 可观测性 | 接入 OpenTelemetry，每个 Skill 调用 + 每个 IR 事件 + 每个 SA 步骤全链路 Trace（**承接** [`9、全链条第二阶段开发计划.md`](./9、全链条第二阶段开发计划.md) §14.2 的 `runId` 关联 ID，扩展为 W3C traceparent 跨进程传播） |
| 模型路由优化 | 分类/摘要用小模型，核心推理用强模型，动态切换 |
| 记忆遗忘机制 | 基于访问频次 + 重要性计算保留分数，低分记忆压缩或删除 |
| 沙箱自动扩缩容 | 基于队列深度自动扩容 Skill Worker 实例（初始：每租户最多 3 并发项目；**Phase 2.5** 先落地静态配额，阶段六升级为动态扩缩容） |
| 人工抽检 | 建立 Skill 产出的人工评审通道，校准 LLM-as-Judge 偏差 |
| Skill 质量排行榜 | 统计每个 Skill 的成功率、平均 token 消耗、用户满意度，指导模型和 Prompt 优化 |

**验收标准：**
1. 10 个并发项目同时运行，系统稳定无崩溃，Token 总消耗在预算内
2. 某个 Skill Worker 崩溃后，对应项目自动重启并从最后稳定的 IR 快照继续，不丢数据
3. 轨迹评估能识别：Agent 调用了冗余工具（同一工具调用 >3 次无进展 → 告警）
4. Token Budget 熔断机制生效：超出租户配额时，优雅返回 partial result，不中断整个 Harness

---

## 3. 十大 Skill 逐一详细设计

### 通用设计约定

所有 Skill 都实现以下接口：

```typescript
interface IBaseSkill {
  readonly skillId: string;           // 全局唯一标识，如 "skill:pm-v1"
  readonly version: string;           // 语义化版本号
  
  // 信息需求声明（Harness 据此构建上下文）
  readonly informationNeeds: {
    irFragmentTypes: IRFragmentType[];         // 需要哪些类型的 IR 片段
    requiredStability: 'draft' | 'stable' | 'locked'; // 要求最低稳定性
    canFilterByDomain?: string[];             // 可否按子域过滤
    seedDataCategories?: string[];            // 需要哪类种子数据
  };
  
  // 产出物声明（Harness 据此注册事件订阅）
  readonly outputs: {
    irEventTypes: IREventType[];       // 产出哪些 IR 事件类型
    constraintDeclarations?: ConstraintDecl[]; // 声明的设计约束
  };
  
  // 核心推理入口
  reason(context: SkillContext): AsyncIterableIterator<IREvent>;
  
  // 与上下游 Skill 的握手协议
  validateInput(irSnapshot: IRSnapshot): ValidationResult;
  validateOutput(events: IREvent[]): ValidationResult;
}
```

新增 Skill 只需实现此接口并在 Skill Registry 注册：
```typescript
skillRegistry.register({
  skill: new PMSkill(),
  triggers: ['project.*.requirements.submitted'],    // 订阅的 A2A Topic
  produces: ['project.*.skeleton-ready']             // 产出的 A2A Topic
});
```

---

### Skill 1：产品经理 Skill（PM Skill）

**认知角色：** 发散→收敛 的蓝图构建者。定义"做什么"，不分析"怎么做"。

**职责边界：**
- **做**：从混沌原始需求提取产品骨架（业务事件清单 + 角色矩阵 + 核心流程 + 实体草案）
- **不做**：字段级规格、业务规则细节、UI 设计、技术架构选型
- **握手协议**：产出合法 IR-0 后，发布 `SkeletonCreated` 事件，触发稳定性判定器；系统需求分析师订阅后接管

**信息需求声明：**
- IR 片段类型：无（起点 Skill，从原始需求出发）
- 种子数据：行业领域模式子图 + 业务术语表（通过 `domain-knowledge-mcp` 检索）
- 用户输入：原始需求文档（文本 / OCR 后的文本）

**核心推理机制：Tree-of-Thoughts（ToT）+ 领域约束评分器**

```
原始需求 + 领域知识子图
           │
           ▼
    ① 生成 N 个候选业务事件切分方案（N=3，Beam Search）
           │
           ▼
    ② 每个候选方案扩展三个维度：
       角色矩阵候选 + 核心流程候选 + 实体草案候选
           │
           ▼
    ③ 领域评分器（规则引擎 + 图匹配）：
       domain_alignment_score = 1 - graph_edit_distance(候选, KG子图)
           │
    ④ LLM 自评：
       requirement_cover_score = 覆盖原始需求的关键意图的比例
           │
           ▼
    ⑤ 综合得分选 Top-1，生成 IR-0（FormalizedSkeletonModel）
           │
           ▼
    ⑥ MCP 校验服务：
       - 事件引用的角色在角色矩阵中存在
       - 实体间外键引用有效
       - 流程节点引用的事件 ID 存在
           │
           ▼
    ⑦ 用户确认（Human-in-the-Loop）→ 追加 SkeletonCreated 事件
```

**产出物（IR-0 核心字段）：**

```typescript
interface IR0_Skeleton {
  skeletonId: string;
  version: number;
  businessEvents: Array<{
    eventId: string;               // EVT-001 等
    eventName: string;
    trigger: string;               // 什么操作触发此事件
    primaryActor: string;
    involvedRoles: string[];
    relatedEntityDrafts: string[]; // 关联实体名
    complexityHint: 'auto' | 'simple' | 'complex'; // 影响 SA 精炼策略
    dependsOn: string[];           // 上游事件 ID（依赖图）
  }>;
  roleMatrix: Array<{
    roleName: string;
    responsibilities: string[];
    permissionBoundary: string;
  }>;
  coreFlow: {
    nodes: Array<{ id: string; label: string; type: 'event' | 'decision' | 'start' | 'end' }>;
    edges: Array<{ from: string; to: string; label?: string }>;
  };
  entityDrafts: Array<{
    entityName: string;
    tableName: string;             // UPPER_SNAKE_CASE，如 WORK_ORDER
    fields: Array<{
      name: string;
      typeHint: 'string' | 'int' | 'datetime' | 'uuid' | 'decimal' | 'bool';
      isPrimaryKey?: boolean;
      fkRef?: string;             // 外键引用的实体名
      description: string;
    }>;
  }>;
  glossaryTermsUsed: string[];
}
```

**与 SA 九步的对接点：**
`entityDrafts` → SA Step 3（Dict）的起点；`businessEvents` → SA Step 0（Scope）的直接输入；`coreFlow` → SA Step 2（BPM）的基础骨架。

**选择 ToT 而非单次 LLM 的理由：**
单次 LLM 调用无法发现自身的"隐含假设"（如将用户说的"报工"误解为"工单审批"）。ToT 的多候选竞赛机制，配合领域评分器，让不符合行业认知的方案在评分环节被淘汰，而不是等到开发阶段才发现。

---

### Skill 2：系统需求分析师 Skill（Analyst Skill）

**认知角色：** 精确→可信 的规格精炼者。在既定产品边界内，将每个业务事件的行为规则和数据规格精确化。

**职责边界：**
- **做**：驱动 SA 九步完整执行；逐事件深度精炼（字段级规格 + 业务规则 + IOI 不变量）；上下游一致性维护
- **不做**：产品边界的重定义（这是产品经理 Skill 的职责）；技术架构选型
- **握手协议**：输入依赖 IR-0 全部片段达到 `stable`；产出 IR-1 后，对每个 SA 步骤追加 `SA_Step_Completed` 事件

**信息需求声明：**
- IR 片段类型：`IR0_Skeleton`，要求 `stable`
- 种子数据：标准业务规则片段 + 标准ER子模型 + 行业字典条目

**核心推理机制：IOI 框架 + 结构化多轮追问**

```
IR-0 骨架（已稳定）
    │
    ▼ 构建事件依赖图（拓扑排序）
    │
    loop 按依赖顺序处理每个业务事件
    │
    ▼ ① 上下文裁剪：只注入本事件相关的骨架 + 上游事件已确认规格
    │
    ▼ ② 调用 SAOrchestrator.runStep(eventId, step)：
    │     Step 0 Scope → Step 1 DFD → Step 2 BPM → Step 3 Dict
    │     Step 4 PSpec → Step 5 DecisionTable → Step 6 ER
    │     Step 7 StateMachine → Step 8 UI
    │   每步完成 → 追加 SA_Step_Completed{eventId, stepName, outputRef} 事件
    │
    ▼ ③ 复杂事件启动多轮对话：
    │     Round 1: 流程完整性
    │     Round 2: 外键业务规则
    │     Round 3: 字段数据细节
    │     Round 4: 最终确认
    │
    ▼ ④ IOI 不变量校验（通过 ioi-validator-mcp）：
    │     新定义的字段/规则是否违反全局不变量？
    │     前置条件 / 后置条件是否与上游规格一致？
    │
    ▼ ⑤ 追加 EventSpecConfirmed{eventId, irRef, invariants[]} 事件
    │
    end loop
    │
    ▼ 全部事件规格确认后：
      判断 SA 9步全部 SA_Step_Completed → 稳定性判定器升级 IR-1 为 stable
      追加 AnalysisCompleted 事件，触发下游设计 Skill 激活
```

**SA 九步与 IR 事件的精确对应：**

| SA 步骤 | SA Agent 调用 | 产出的 IR 事件类型 | IR 层次 |
|---|---|---|---|
| Step 0: Scope | `ScopeAgent.analyze()` | `ScopeAnalyzed` | IR-1 |
| Step 1: DFD | `DFDAgent.generate()` | `DFDGenerated` | IR-1 |
| Step 2: BPM | `BPMAgent.generate()` | `BPMGenerated` | IR-1 |
| Step 3: Dict | `DictAgent.extract()` | `DictionaryExtracted` | IR-1 |
| Step 4: PSpec | `PSpecAgent.specify()` | `ProcessSpecified` | IR-1 |
| Step 5: DecisionTable | `DecisionTableAgent.build()` | `DecisionTableBuilt` | IR-1 |
| Step 6: ER | `ERAgent.design()` | `ERDesigned` | IR-1 |
| Step 7: StateMachine | `StateMachineAgent.model()` | `StateMachineModeled` | IR-1 |
| Step 8: UI | `UIAgent.prototype()` | `UIPrototyped` | IR-1 |

**IOI 框架保证的平衡（目标四的核心回答）：**

> 系统需求分析环节的本质挑战：既要发挥 LLM 理解模糊意图的优势，又要防止 LLM 编造业务规则。
>
> 解法：**LLM 负责候选提案，IOI 框架负责形式化校验**。
> - LLM：根据上下文生成候选事件规格（字段、规则、状态）
> - IOI Validator：每个候选项必须通过不变量校验才能写入 IR
> - 具体：如"工单总成本 = 所有工序成本之和"这条不变量，在精炼"报工成本"字段时，IOI Validator 会自动验证新字段的加减逻辑不会破坏此约束。违反即拒绝，要求 LLM 重新提案。

---

### Skill 3：架构设计 Skill（Architect Skill）

**认知角色：** 决策层，确定系统的高层技术架构和模块边界。

**职责边界：**
- **做**：架构模式选型（分层/CQRS/事件驱动）、技术栈确认、核心模块边界、非功能需求（性能/安全/扩展性）的实现策略
- **不做**：具体 DDL 设计、UI 布局、代码实现
- **握手协议**：输入依赖 IR-1 全部达到 stable；产出 `ArchitectureDecisionRecorded` 事件，总体设计 Skill 订阅

**信息需求声明：**
- IR 片段类型：`IR1_EventSpec[]`（全部），`IR0_Skeleton`（参考），要求 `stable`
- 可选过滤：可按子域/模块过滤相关 IR-1 片段

**核心推理机制：ToT + 多架构方案对比**

架构评分维度：
- 模块耦合度（基于 IR-1 中的跨事件数据依赖频率）
- 与 JNPF 现有框架的兼容度（分层 + IDynamicApiController + SqlSugar）
- 团队能力适配度
- 可扩展性（新业务事件接入成本）

**产出物（IR-2 架构片段）：**
```typescript
interface IR2_Architecture {
  pattern: 'layered' | 'cqrs' | 'event-driven' | 'microkernel';
  modules: Array<{
    moduleId: string;
    name: string;
    layer: 'presentation' | 'application' | 'domain' | 'infrastructure';
    responsibleEvents: string[];    // 处理哪些业务事件
    publicInterfaces: string[];
    dependencies: string[];         // 依赖哪些其他模块（单向）
  }>;
  crossCuttingConcerns: string[];   // 日志、认证、多租户 等横切关注点
  nonFunctionalDecisions: Record<string, string>;
  adrs: Array<{
    title: string; context: string; decision: string; consequences: string;
  }>;
}
```

**架构约束的声明与约束传播引擎的对接：**
架构 Skill 产出的 `modules[].dependencies` 声明了合法的依赖方向（如 `application → domain`，禁止 `domain → application`）。约束传播引擎读取这些声明，作为后续代码检查的规则基础。

---

### Skill 4：总体设计 Skill（System Design Skill）

**认知角色：** 决策层中的横向协调者。不做具体设计，只做三方（架构/数据库/UI）产出的一致性验证和冲突消解。

**职责边界：**
- **做**：检查架构模块边界与 DB 表归属是否一致；检查 UI 页面数据来源与 ER 实体是否匹配；定义模块间接口契约；输出集成点定义
- **不做**：修改架构、数据库、UI 的设计决策（只指出冲突，让对应 Skill 修改）
- **激活条件**：架构/数据库/UI 三个设计 Skill 全部产出 `stable` IR-2 片段

**一致性校验规则（初期基于结构化规则，不依赖 LLM 判断）：**

```typescript
// 规则1：每个 IR-1 业务事件必须在 IR-2 架构中有对应的处理模块
// 规则2：IR-2 DDL 的每个实体必须对应 IR-1 中的某个实体草案（无悬挂表）
// 规则3：UI 设计 Skill 产出的每个 FormPageIR 的字段必须能映射到 DDL 中的列
// 规则4：模块间接口调用方向必须符合架构 Skill 声明的依赖方向

interface ConsistencyReport {
  passed: boolean;
  conflicts: Array<{
    type: 'missing_handler' | 'dangling_entity' | 'unmapped_field' | 'dependency_violation';
    description: string;
    affectedSkill: 'architect' | 'db-design' | 'ui-design';
    suggestion: string;
  }>;
}
```

若有冲突，总体设计 Skill 追加 `InconsistencyDetected` 事件，将 `affectedSkill` 对应的 IR 片段状态退回 `in-progress`，触发对应 Skill 重算。

---

### Skill 5：数据库设计 Skill（DB Design Skill）

**认知角色：** 精炼层，将 IR-1 事件规格转化为精确的数据库设计（DDL + ER 图）。

**职责边界：**
- **做**：从 IR-1 聚合根推导表结构；范式化分析；索引策略；外键约束；JNPF 命名规范适配（UPPER_SNAKE_CASE）
- **不做**：业务规则的重新定义；ORM 层代码生成（这是开发 Skill 的职责）

**信息需求声明：**
- IR 片段类型：`IR1_EventSpec[]`（全部），要求 `stable`
- 种子数据：JNPF 现有表命名规范 + 现有公共字段约定（F_Id, F_TenantId, F_DeleteMark 等）

**核心推理机制：增量 DDL 生成**

从 IR-0 实体草案（类型提示）→ IR-1 确认字段（精确类型 + 约束） → 生成 DDL 的过程是增量的：

```
IR-0 entityDraft.fields[].typeHint = "string"
          ↓ IOI 精炼后
IR-1 confirmedFields[].type = NVARCHAR(200) NOT NULL
          ↓ DB Skill 应用范式 + JNPF 命名规范
IR-2 DDL: [F_WorkOrderCode] NVARCHAR(200) NOT NULL -- 工单编号
```

**产出物：**
- 完整 DDL 文件（含 CREATE TABLE + 索引 + 约束 + 扩展属性注释）
- ER 图的 IR 表示（含实体、关系、基数）
- 公共类使用声明（是否继承 `EntityBase`，是否使用 `SqlSugar` 特性）

---

### Skill 6：UI 设计 Skill（UI Design Skill）

**认知角色：** 精炼层，将 IR-1 中与用户交互相关的事件规格转化为前端 IR（FormPageIR / ListPageIR 等）。

**职责边界：**
- **做**：根据 IR-1 事件规格生成符合现有 JNPF 前端 IR 类型体系的页面描述；交互协议定义；组件选型建议
- **不做**：自定义前端组件开发；视觉样式定制（交给 `jnpf-ui-enhance` Skill）

**信息需求声明：**
- IR 片段类型：`IR1_EventSpec[]`，可**按子域过滤**（如只订阅"入场管理"子域的事件规格）
- 可并行处理：多个事件的页面 IR 并行生成，无顺序依赖

**核心推理机制：模板化页面 IR 生成**

```
业务事件类型 → 页面类型映射规则：
  - CRUD 类事件 → FormPageIR（表单页）+ ListPageIR（列表页）
  - 流程审批类事件 → WorkflowPageIR（流程页）
  - 数据展示类事件 → DetailPageIR（详情页）
  - 报表统计类事件 → ReportPageIR（报表页）

每个 IR-1 字段 → FormItemIR 组件类型映射：
  - string/NVARCHAR → InputItem
  - int/datetime → NumberItem/DatetimeItem
  - 外键引用 → SelectItem（关联现有下拉数据源）
  - bool → SwitchItem
  - enum（来自 Decision Table）→ RadioItem / SelectItem
```

**与现有 IR 类型体系的无缝对接：**
UI 设计 Skill 的产出直接符合 `jnpf-web-vue3/src/views/ai/` 中已有的 FormPageIR / ListPageIR TypeScript 类型定义，无需任何格式转换。

---

### Skill 7：开发 Skill（Developer Skill）

**认知角色：** 执行层，将完整 IR-2 转化为可编译的 JNPF 模块代码。

**职责边界：**
- **做**：驱动 `.vm` 模板生成 C# Service + Entity；生成前端 Vue3 页面（基于 IR-2 的 FormPageIR）；写入架构层次信息（供 Bug 修复 Skill 和约束传播引擎使用）
- **不做**：直接修改任何模板生成的输出文件；手写 Controller（架构红线 R1/R3 严格遵守）

**信息需求声明：**
- IR 片段类型：`IR2_Architecture` + `IR2_DDL` + `IR2_UIDesign`，全部要求 `stable`
- 触发条件：总体设计 Skill 发出 `SystemDesignLocked` 事件

**架构层次信息的提取与存储：**
```typescript
// 开发 Skill 在生成代码后，向 IR 追加架构元数据事件
interface ArchitectureMetadataEvent {
  type: 'ArchitectureMetadataRecorded';
  payload: {
    layers: Array<{
      name: 'Controller(Auto)' | 'Service' | 'Repository' | 'Entity';
      classes: Array<{ className: string; filePath: string; methods: string[] }>;
    }>;
    commonComponents: Array<{
      name: string;              // 如 BaseService, SqlSugarRepository
      usedBy: string[];          // 被哪些生成类引用
    }>;
    dependencyRules: Array<{
      from: string; to: string; allowed: boolean;
    }>;
  };
}
```

这份元数据是 Bug 修复 Skill 和约束传播引擎的关键输入，存储在 IR 事件流中，可随时重建。

**代码生成沙箱验证流程：**
```
生成代码文件
    │
    ▼ 写入隔离沙箱目录（tenant-specific temp dir）
    │
    ▼ 执行 `dotnet build`
    │  通过 → 继续
    │  失败 → 追加 CodeGenFailed 事件 → 触发 Debugger（系统性调试 Skill）
    │
    ▼ 执行 ESLint（前端代码）
    │  通过 → 继续
    │  失败 → 修复后重试（最多 3 次）
    │
    ▼ 执行 `arch-guard` 架构守卫检查
    │  通过 → 追加 CodeGenerated 事件
    │  失败 → 追加 ArchViolationDetected 事件，阻止 IR-3 进入 stable
```

---

### Skill 8：测试 Skill（Tester Skill）

**认知角色：** 执行层，从 IR-1 事件规格自动推导测试场景，验证 IR-3 代码制品。

**职责边界：**
- **做**：从事件规格推导正向测试 + 边界条件 + 异常分支；生成可执行的测试脚本（Xunit C# + Playwright E2E）；执行测试并将结果写入 IR
- **不做**：手工编写业务逻辑测试（必须基于 IR-1 的规格自动推导，不猜测）

**信息需求声明：**
- IR 片段类型：`IR1_EventSpec[]`（推导测试场景）+ `IR3_GeneratedCode`（执行测试目标），均要求 `stable`

**测试场景推导规则（确定性规则，不依赖 LLM 主观判断）：**

```typescript
// 对每个 IR-1 事件规格，自动推导以下类型的测试用例：
type TestCaseType =
  | 'happy-path'          // 主流程：合法输入 → 预期输出
  | 'boundary-value'      // 边界值：字段最大/最小/边界
  | 'null-check'          // 非空约束：必填字段为空
  | 'fk-violation'        // 外键约束：引用不存在的实体
  | 'ioi-invariant'       // IOI 不变量：验证约束不被破坏
  | 'permission-boundary' // 权限边界：角色矩阵定义的操作权限
  | 'concurrent-update';  // 并发安全：乐观锁版本冲突
```

**LLM-as-Judge 的使用范围限制（来自 `agent运行时.md` 的生产原则）：**
- **用 LLM 评估**：测试覆盖率的合理性、测试描述的清晰度
- **不用 LLM 评估**：代码能否编译、测试能否执行、SQL 结果是否正确（用确定性检查）

---

### Skill 9：部署 Skill（Deploy Skill）

**认知角色：** 执行层，将通过测试的 IR-3 制品发布到目标环境。

**职责边界：**
- **做**：驱动 JNPF 的应用发布流程（菜单注册 + 权限配置 + 表结构迁移 + 功能开关启用）；部署后冒烟测试（Playwright）
- **不做**：修改基础设施配置；操作 JNPF 手工平台的生产数据

**与 JNPF 手工平台的隔离原则：**
- JNPF-AI 生成的应用发布到独立的 `AI_GENERATED` 功能分区
- 数据库表使用独立的 `AI_` 前缀（如 `AI_WORK_ORDER`）与手工平台表隔离
- 发布操作通过 JNPF 的标准 API（`/api/visualDev/publish`）进行，不直接操作数据库

**冒烟测试产出 E2E 截图证据（Supreme Iron Law 合规）：**
```typescript
// 部署验证流程
const playwright = await chromium.launch();
const page = await playwright.newPage();
await page.goto(`http://localhost:3100/ai-generated/${projectId}`);
await page.screenshot({ path: `.claude/evidence/${projectId}-deploy-${Date.now()}.png` });
// 追加 DeploymentCompleted 事件，携带截图引用
```

---

### Skill 10：Bug 修复 Skill（BugFix Skill）

**认知角色：** 横向闭环的修复者。按缺陷类型路由到对应 IR 层次，只修复最小化受影响范围，不触发全局回退。

**职责边界：**
- **做**：沿事件溯源流回溯定位缺陷注入点；标记受影响 IR 片段；触发最小化 Skill 重算；追加修复事件
- **不做**：重新分析整个项目（只看受影响片段）；绕过 IOI 校验（修复方案仍需通过约束验证）

**信息需求声明：**
- IR 片段类型：`ir_events` 全量（用于回溯分析），无稳定性要求（只读历史事件）

**缺陷定位的事件溯源回溯算法：**

```typescript
async function locateBugRootCause(bugReport: BugReport): Promise<BugLocation> {
  // 1. 从错误堆栈提取关键词（类名、方法名、字段名）
  const keywords = extractKeywords(bugReport.stackTrace);
  
  // 2. 在 ir_events 表中搜索相关 IR 片段
  const relatedEvents = await irEventStore.search({
    projectId: bugReport.projectId,
    keywords,
    eventTypes: ['EventSpecConfirmed', 'SA_Step_Completed', 'CodeGenerated', 'DDLStabilized']
  });
  
  // 3. 按事件发生时间排序，找到最早产生问题的 IR 决策
  const rootCauseEvent = relatedEvents.sort(byTimestamp).find(e => 
    e.payload.affectsField === bugReport.reportedField ||
    e.payload.ruleId === bugReport.violatedRule
  );
  
  // 4. 确定缺陷层次
  const layer = determineLayer(rootCauseEvent);
  // - SA_Step_Completed → IR-1 层（需求分析问题）
  // - DDLStabilized → IR-2 层（数据库设计问题）
  // - CodeGenerated → IR-3 层（代码生成问题）
  
  return { layer, irFragmentId: rootCauseEvent.fragmentId, rootCauseEvent };
}
```

**最小化影响范围分析：**

```
发现 IR-1 的某个 EventSpec 有错误（如字段约束遗漏）
    │
    ▼ 查询约束传播引擎：
      哪些 IR-2 片段引用了这个 EventSpec？
      哪些 IR-3 代码片段由这些 IR-2 片段生成？
    │
    ▼ 标记受影响片段（状态退回 in-progress）
      IR-1: EventSpec_EVT-003  ← 修复这里
      IR-2: DDL_WORK_ORDER（引用了 EVT-003 的字段）
      IR-3: WorkOrderService.cs（由上述 DDL 生成）
      不影响：IR-2 ArchitectureDecision、UI_FormPageIR_EVT-001 等
    │
    ▼ 追加 AffectedFragmentsMarked 事件
      只触发：analyst-skill（修复规格）→ db-design-skill → developer-skill
```

**架构保持的三重机制（目标六的具体实现）：**

1. **分层信息识别**：开发 Skill 生成代码时追加 `ArchitectureMetadataRecorded` 事件，记录 Controller/Service/Repository/Entity 的完整类图
2. **约束声明检测**：约束传播引擎读取架构 Skill 的 `dependencyRules`，在每次代码生成后执行分层依赖方向检测
3. **架构自愈建议**：当同一 IR 片段被 BugFix Skill 修复超过 3 次时，追加 `ArchitectureSmellDetected` 事件，建议人工介入审查该 IR 片段的设计合理性

---

## 4. 关键技术决策记录

### TDR-001：为什么选事件规格（EventSpec）作为 IR 最小稳定单元？

**背景：** 系统需要一个"稳定"的 IR 粒度，作为下游 Skill 激活的门控条件。候选粒度有：整个项目、SA步骤、子域、单个事件规格。

**决策：选单个 EventSpec（业务事件规格）作为最小稳定单元。**

**理由：**
- **粒度匹配**：一个 EventSpec 对应一个完整的业务功能（如"创建工单"），是最小的独立可交付单元，与业务语言天然对齐
- **并行粒度合适**：10-20 个业务事件可以在精炼阶段大量并行，粒度太粗（按子域）会导致大块阻塞，太细（按字段）会导致编排开销过大
- **已被否定的粒度**：按阶段或子域做并行边界（文档共识：粒度太粗，无法处理跨域约束）；按字段做稳定单元（粒度太细，编排成本高于收益）

---

### TDR-002：为什么用 SA 步骤完成度自动判定稳定性，而非 LLM 主观判断？

**背景：** 稳定性门控需要一个客观、可重复的判定机制。

**决策：完成度 = 结构化的 SA 步骤 Completed 事件计数，不依赖 LLM 主观评估。**

**理由：**
- **可重复性**：LLM 对"是否稳定"的判断受上下文影响，两次相同输入可能给出不同答案；SA步骤计数是确定性的
- **可审计性**：`SA_Step_Completed` 事件是不可变记录，任何时刻可验证；LLM 判断是黑盒
- **防止提前触发**：LLM 倾向于乐观估计完成度（减少"没完成"带来的用户等待感），会导致下游 Skill 消费到不完整的 IR-1

**实现：** 稳定性判定器维护一个 `{projectId, eventId} → completedSteps: Set<StepName>` 的计数器。当 `completedSteps.size === 9`（全部 SA 步骤），对应 EventSpec 片段升级为 `stable`。

---

### TDR-003：上下文构建策略——增量上下文包含什么？如何避免窗口溢出？

**背景：** 每个 Skill 激活时需要构建一个 LLM 上下文，既要包含足够信息，又不能超出模型的 Context Window。

**决策：基于信息需求声明的精准上下文裁剪（Precision Context Slicing）。**

**增量上下文构建规则：**

```typescript
class ContextBuilder {
  build(skill: IBaseSkill, irSnapshot: IRSnapshot): PromptContext {
    const { irFragmentTypes, requiredStability, canFilterByDomain } = skill.informationNeeds;
    
    // 1. 只包含 Skill 声明需要的 IR 片段类型
    const relevantFragments = irSnapshot.fragments.filter(f => 
      irFragmentTypes.includes(f.type) && 
      f.stability >= requiredStability
    );
    
    // 2. 可选的子域过滤（减少无关信息）
    const filtered = canFilterByDomain 
      ? relevantFragments.filter(f => canFilterByDomain.includes(f.domain))
      : relevantFragments;
    
    // 3. Token 预算估算（在注入前检查）
    const estimatedTokens = countTokens(filtered);
    if (estimatedTokens > TOKEN_BUDGET_THRESHOLD) {
      // 压缩策略：只保留字段名 + 类型 + 约束，省略描述文本
      return compressFragments(filtered, 'minimal');
    }
    
    // 4. 种子数据检索（精准匹配，不全量注入）
    const seedData = await seedDataMCP.retrieve({
      categories: skill.informationNeeds.seedDataCategories,
      keywords: extractKeywords(filtered),
      maxItems: 20     // 每类最多 20 条种子数据
    });
    
    return { irFragments: filtered, seedData, systemPrompt: skill.systemPrompt };
  }
}
```

**避免窗口溢出的三道防线：**
1. 信息需求声明（Skill 只声明需要的 IR 片段类型，Harness 不全量注入）
2. 子域过滤（可选，按业务事件所属子域过滤相关片段）
3. 动态压缩（超出阈值时，IR 片段只保留字段摘要，省略详细描述）

---

### TDR-004：约束冲突检测算法——初期和远期策略

**背景：** 需要检测生成代码是否违反架构约束（分层依赖方向 + 公共类使用规范）。

**初期策略（W1-W8）：基于规则的确定性检测。**

```typescript
// 分层依赖方向检测
const DEPENDENCY_RULES = [
  { from: 'Presentation', to: 'Application', allowed: true },
  { from: 'Application', to: 'Domain', allowed: true },
  { from: 'Domain', to: 'Infrastructure', allowed: true },
  { from: 'Domain', to: 'Application', allowed: false },  // 违规
  { from: 'Domain', to: 'Presentation', allowed: false }, // 违规
];

// 检测方式：解析 C# 文件的 using 语句 + 命名空间，判断层次归属
function detectLayerViolation(csharpFile: string): ViolationReport[];
```

**远期策略（W9+）：语义匹配补充检测。**
- 对"规避公共类"的隐式绕过（如直接重写 `BaseService` 的功能而非继承），用 AST 语义分析检测
- 引入 Roslyn Analyzer（已有 `JNPF.Analyzers` 项目）定制规则，集成到开发 Skill 的代码沙箱验证流程

**约束自愈（架构的"自愈"能力）：**
当 Bug 修复 Skill 在同一 IR 片段触发修复超过 3 次，且根因都指向同一个公共类或分层决策，追加 `ArchitectureSmellDetected` 事件：

```json
{
  "type": "ArchitectureSmellDetected",
  "payload": {
    "smellType": "repeated-fix-on-same-component",
    "componentPath": "Domain.WorkOrder.WorkOrderService",
    "fixCount": 4,
    "suggestion": "建议审查 WorkOrderService 的职责划分，可能需要拆分为 WorkOrderCommandService + WorkOrderQueryService",
    "relatedIrFragments": ["IR1_EVT-003", "IR1_EVT-007"]
  }
}
```

---

### TDR-005：与 JNPF 手工平台的关系——代码层面如何隔离？数据库是否共用？

**决策：逻辑隔离 + 数据库共实例但表前缀分离 + API 路由分离。**

**代码层面隔离：**
```
backend/
├── modularity/JNPF.xxx/          ← 手工平台代码（禁止 AI 修改）
└── modularity/JNPF.AI.xxx/       ← AI 平台专属代码（AI 生成 + AI 运行时）

jnpf-web-vue3/src/views/
├── {手工功能模块}/               ← 手工平台页面（禁止 AI 修改）
└── ai/                           ← AI 平台专属页面
    └── generated/{projectId}/    ← AI 生成的应用页面（运行时渲染）
```

**数据库表共实例，表名前缀分离：**
- 手工平台：`BASE_USER`, `FLOW_TASK`, `FORM_DATA` 等（无前缀或原有前缀）
- AI 平台运行时：`AI_IR_EVENTS`, `AI_SA_SKELETON`, `AI_PROJECT` 等（`AI_` 前缀）
- AI 生成的业务表：`AI_GEN_{PROJECT_SHORT}_{ENTITY}` 等（可配置前缀，与手工平台表名不冲突）

**API 路由分离：**
- 手工平台：`/api/{module}/` 不变
- AI 平台：`/api/ai/` 前缀（独立路由命名空间）

**功能开关隔离：**
AI 平台功能通过 Feature Flag 控制，`appsettings.json` 中 `Features:AIEnabled: false` 即可在不影响手工平台的情况下完全禁用 AI 模块。

---

## 5. 数据模型设计草案

### 5.1 核心表结构

#### `ai_ir_events`（IR 事件溯源主表）

```sql
CREATE TABLE [dbo].[ai_ir_events] (
    [F_Id]              NVARCHAR(50)    NOT NULL,                    -- 事件全局唯一ID（ULID格式，天然有序）
    [F_ProjectId]       NVARCHAR(50)    NOT NULL,                    -- 所属项目ID
    [F_TenantId]        NVARCHAR(50)    NOT NULL,                    -- 租户ID（L1路由键）
    [F_EventType]       NVARCHAR(100)   NOT NULL,                    -- 事件类型枚举（见下方枚举表）
    [F_FragmentType]    NVARCHAR(50)    NULL,                        -- 关联的IR片段类型（IR0/IR1/IR2/IR3/IRE）
    [F_FragmentId]      NVARCHAR(50)    NULL,                        -- 关联的IR片段ID（如 EVT-003, arch-001）
    [F_FragmentVersion] INT             NOT NULL DEFAULT 1,          -- 片段版本号（每次修改+1）
    [F_Payload]         NVARCHAR(MAX)   NOT NULL,                    -- 事件数据（JSON，符合IR Schema）
    [F_SkillId]         NVARCHAR(100)   NULL,                        -- 产出此事件的Skill ID
    [F_SAStepName]      NVARCHAR(50)    NULL,                        -- SA步骤名（仅SA_Step_Completed事件有值）
    [F_Sequence]        BIGINT          IDENTITY(1,1) NOT NULL,      -- 全局追加序号（投影重建用）
    [F_CreatedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),-- UTC时间（时间旅行基准）
    [F_IsRollback]      BIT             NOT NULL DEFAULT 0,          -- 是否为回滚/修复产生的事件

    CONSTRAINT [PK_ai_ir_events] PRIMARY KEY ([F_Id]),
    CONSTRAINT [CK_ir_events_type] CHECK ([F_EventType] IN (
        -- 产品经理 Skill
        'SkeletonCreated', 'SkeletonUpdated',
        -- 系统需求分析师 Skill
        'SA_Step_Completed', 'EventSpecConfirmed', 'EventSpecRevised', 'AnalysisCompleted',
        -- 设计 Skill
        'ArchitectureDecisionRecorded', 'DDLStabilized', 'UIDesignStabilized',
        'SystemDesignLocked', 'InconsistencyDetected',
        -- 开发/测试 Skill
        'CodeGenerated', 'TestGenerated', 'TestPassed', 'TestFailed',
        'ArchitectureMetadataRecorded', 'ArchViolationDetected', 'CodeGenFailed',
        -- 部署 Skill
        'DeploymentCompleted', 'DeploymentFailed',
        -- Bug修复 Skill
        'BugReported', 'BugRootCauseLocated', 'AffectedFragmentsMarked', 'BugFixed',
        'ArchitectureSmellDetected',
        -- 系统控制事件
        'FragmentStabilized', 'FragmentLocked', 'ProjectArchived'
    ))
);

-- 租户+项目的事件流查询（核心查询路径）
CREATE INDEX [IX_ir_events_project]
    ON [dbo].[ai_ir_events] ([F_TenantId], [F_ProjectId], [F_Sequence])
    INCLUDE ([F_EventType], [F_FragmentId], [F_CreatedAt]);

-- 片段历史查询（时间旅行）
CREATE INDEX [IX_ir_events_fragment]
    ON [dbo].[ai_ir_events] ([F_ProjectId], [F_FragmentId], [F_FragmentVersion]);

-- SA步骤完成度查询（稳定性判定器使用）
CREATE INDEX [IX_ir_events_sa_steps]
    ON [dbo].[ai_ir_events] ([F_ProjectId], [F_FragmentId], [F_SAStepName])
    WHERE [F_EventType] = 'SA_Step_Completed';
```

#### `ai_ir_fragment_snapshots`（IR 片段快照投影表）

```sql
CREATE TABLE [dbo].[ai_ir_fragment_snapshots] (
    [F_Id]              NVARCHAR(50)    NOT NULL,                    -- 快照ID
    [F_ProjectId]       NVARCHAR(50)    NOT NULL,
    [F_TenantId]        NVARCHAR(50)    NOT NULL,
    [F_FragmentId]      NVARCHAR(50)    NOT NULL,                    -- IR片段标识（如 EVT-003）
    [F_FragmentType]    NVARCHAR(50)    NOT NULL,                    -- IR0_Skeleton / IR1_EventSpec / IR2_DDL 等
    [F_CurrentVersion]  INT             NOT NULL,                    -- 当前版本号
    [F_StabilityState]  NVARCHAR(20)    NOT NULL DEFAULT 'draft',    -- draft/in-progress/stable/locked
    [F_IrContent]       NVARCHAR(MAX)   NOT NULL,                    -- 当前状态快照（JSON-LD，事件流折叠结果）
    [F_SAStepsCompleted]NVARCHAR(500)   NULL,                        -- 已完成的SA步骤列表（JSON数组，仅IR1有值）
    [F_LastEventId]     NVARCHAR(50)    NOT NULL,                    -- 生成此快照的最后一个事件ID
    [F_UpdatedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    [F_DeleteMark]      BIT             NOT NULL DEFAULT 0,

    CONSTRAINT [PK_ai_ir_fragment_snapshots] PRIMARY KEY ([F_Id]),
    CONSTRAINT [UQ_fragment_current] UNIQUE ([F_ProjectId], [F_FragmentId])  -- 每个片段只有一个当前快照
);

-- 片段类型+稳定性查询（下游Skill订阅时使用）
CREATE INDEX [IX_fragment_stability]
    ON [dbo].[ai_ir_fragment_snapshots] ([F_TenantId], [F_ProjectId], [F_FragmentType], [F_StabilityState])
    WHERE [F_DeleteMark] = 0;
```

#### `ai_sa_skeleton`（扩展原 sa_skeleton 表）

```sql
-- 在现有 sa_skeleton 表基础上扩展（不破坏现有字段）
-- 新增字段：
ALTER TABLE [dbo].[sa_skeleton] ADD
    [F_IrFragmentId]    NVARCHAR(50)    NULL,    -- 对应 ai_ir_fragment_snapshots 的 F_FragmentId
    [F_IrVersion]       INT             NULL,    -- IR 版本号（来自事件流）
    [F_IrContent]       NVARCHAR(MAX)   NULL,    -- IR-0 的完整 JSON-LD 内容（与投影表同步）
    [F_StabilityState]  NVARCHAR(20)    NULL DEFAULT 'draft';

-- 现有 ir_content 字段（如已存在）复用为 IR-0 存储
-- 新增 F_IrContent 存储完整 JSON-LD（含 @context + @id + 所有 IR-0 字段）
```

#### `ai_sa_event_specs`（扩展原 sa_event_spec 表）

```sql
ALTER TABLE [dbo].[sa_event_spec] ADD
    [F_IrFragmentId]        NVARCHAR(50)    NULL,    -- 对应 IR-1 EventSpec 片段
    [F_IrVersion]           INT             NULL,
    [F_SAStepsCompleted]    NVARCHAR(500)   NULL,    -- JSON 数组：已完成的SA步骤名称
    [F_IOIInvariants]       NVARCHAR(MAX)   NULL,    -- JSON：此 EventSpec 声明的 IOI 不变量
    [F_StabilityState]      NVARCHAR(20)    NULL DEFAULT 'draft';
```

#### `ai_constraint_violations`（约束冲突报告表）

```sql
CREATE TABLE [dbo].[ai_constraint_violations] (
    [F_Id]              NVARCHAR(50)    NOT NULL,
    [F_ProjectId]       NVARCHAR(50)    NOT NULL,
    [F_TenantId]        NVARCHAR(50)    NOT NULL,
    [F_ViolationType]   NVARCHAR(100)   NOT NULL,    -- dependency_direction / missing_handler / ioi_violation 等
    [F_SourceFragment]  NVARCHAR(50)    NOT NULL,    -- 产生冲突的 IR 片段 ID
    [F_TargetFragment]  NVARCHAR(50)    NULL,        -- 被影响的 IR 片段 ID（可选）
    [F_Description]     NVARCHAR(2000)  NOT NULL,    -- 冲突描述
    [F_Suggestion]      NVARCHAR(2000)  NULL,        -- 修复建议
    [F_Status]          NVARCHAR(20)    NOT NULL DEFAULT 'open',  -- open/resolved/ignored
    [F_ResolvedAt]      DATETIME2(7)    NULL,
    [F_CreatedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    [F_DeleteMark]      BIT             NOT NULL DEFAULT 0,

    CONSTRAINT [PK_ai_constraint_violations] PRIMARY KEY ([F_Id])
);
```

#### `ai_projects`（AI 项目主表）

```sql
CREATE TABLE [dbo].[ai_projects] (
    [F_Id]              NVARCHAR(50)    NOT NULL,    -- projectId
    [F_TenantId]        NVARCHAR(50)    NOT NULL,
    [F_ProjectName]     NVARCHAR(200)   NOT NULL,
    [F_Status]          NVARCHAR(50)    NOT NULL DEFAULT 'requirements',
    -- requirements → analysis → design → development → testing → deployed → archived
    [F_CurrentPhase]    NVARCHAR(50)    NOT NULL DEFAULT 'pm-skill',
    [F_SandboxId]       NVARCHAR(100)   NULL,        -- 关联的沙箱实例
    [F_SkeletonId]      NVARCHAR(50)    NULL,        -- IR-0 骨架片段 ID
    [F_TokenConsumed]   BIGINT          NOT NULL DEFAULT 0,  -- 累计 Token 消耗
    [F_TokenBudget]     BIGINT          NOT NULL DEFAULT 500000, -- 项目 Token 上限
    [F_CreatorUserId]   NVARCHAR(50)    NOT NULL,
    [F_CreatedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    [F_LastModifyTime]  DATETIME2(7)    NULL,
    [F_DeleteMark]      BIT             NOT NULL DEFAULT 0,

    CONSTRAINT [PK_ai_projects] PRIMARY KEY ([F_Id])
);
```

#### `ai_route_table`（L2 项目路由表 — etcd 的 SQL 备份）

```sql
-- etcd 为路由主存储，此表为 etcd 的持久化备份（etcd 重建时用）
CREATE TABLE [dbo].[ai_route_table] (
    [F_Id]              NVARCHAR(50)    NOT NULL,
    [F_TenantId]        NVARCHAR(50)    NOT NULL,
    [F_ProjectId]       NVARCHAR(50)    NOT NULL,
    [F_SandboxId]       NVARCHAR(100)   NOT NULL,
    [F_SandboxType]     NVARCHAR(20)    NOT NULL DEFAULT 'shared',  -- dedicated/shared
    [F_SandboxStatus]   NVARCHAR(20)    NOT NULL DEFAULT 'creating', -- creating/running/archiving/destroyed
    [F_SandboxEndpoint] NVARCHAR(200)   NULL,
    [F_EtcdKey]         NVARCHAR(500)   NOT NULL,    -- 对应的 etcd key
    [F_CreatedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    [F_LastHeartbeat]   DATETIME2(7)    NULL,

    CONSTRAINT [PK_ai_route_table] PRIMARY KEY ([F_Id]),
    CONSTRAINT [UQ_route_project] UNIQUE ([F_TenantId], [F_ProjectId])
);
```

### 5.2 与现有 JNPF 手工平台表的兼容性

| 原则 | 具体措施 |
|---|---|
| 不修改手工平台原有表结构 | 所有新增字段通过 `ALTER TABLE ADD` 可选列方式追加（允许 NULL），不影响现有业务 |
| 不污染手工平台数据 | AI 平台数据通过 `F_TenantId` 隔离，且 AI 项目的 `F_TenantId` 使用 `AI_` 前缀租户（如 `AI_T001`） |
| 新表统一 `AI_` 前缀 | 所有 AI 运行时新增表使用 `ai_` 小写前缀（SQL Server 大小写不敏感，代码中统一 `ai_` 表名） |
| 共用 JNPF 鉴权体系 | AI 平台的 API 请求复用 JNPF 的 JWT Token 验证（`R8` 架构红线：必须声明 `[SecurityDefine]`） |

---

## 6. 关键风险与缓解措施

### 风险 1：ToT 多候选推理带来的 Token 成本爆炸

**风险描述：** 产品经理 Skill 采用 Beam Search（N=3 候选），加上领域评分器多轮调用，单个项目的需求分析阶段可能消耗 50 万+ Token。

**严重程度：** 高（直接影响运营成本和用户体验）

**缓解策略：**
1. **缩小 Beam Width（早期剪枝）**：第一步只生成 2-3 个候选事件切分方案，领域评分器淘汰最差的 1 个后，再扩展剩余候选的子方案（不做笛卡尔积展开）
2. **缓存领域知识**：领域评分器的知识图谱查询结果按 `{tenantId, industry}` 缓存，24 小时内同行业项目复用
3. **按复杂度选推理深度**：简单项目（<5 个业务事件）直接单次生成，不启动 ToT；只有 `complexityHint: complex` 的事件才触发多轮精炼
4. **模型路由**：ToT 的候选生成用小模型（成本低），最终选优和 IOI 校验用强模型

---

### 风险 2：SA 九步 Agent 封装后的质量退化

**风险描述：** 现有 9 个 TypeScript SA Agent 是独立运行的服务，封装为被 `SAOrchestrator` 驱动的内部流程后，原有的 Prompt 和输入格式可能需要调整，导致质量波动。

**严重程度：** 高（SA 九步是 JNPF 的核心资产，质量退化直接影响系统可用性）

**缓解策略：**
1. **保持 Agent 接口不变**：`SAOrchestrator` 通过 HTTP 调用现有 SA Agent 服务，不修改 Agent 内部代码。封装只增加"输入 IR 上下文"和"输出写 IR 事件流"这两个适配层
2. **渐进式集成**：先封装前 3 步（Scope → DFD → BPM），验证质量后再封装后续步骤
3. **A/B 测试对比**：在阶段二验收时，对同一份需求同时运行原始 SA Agent 和封装版，对比产出质量（由人工评审）
4. **回滚机制**：如封装版质量不达标，可在配置中切回直接调用 SA Agent 的模式，不影响其他 Skill

---

### 风险 3：多租户软路由的工程复杂度超出团队能力

**风险描述：** etcd 集群运维 + 沙箱生命周期管理 + 软路由无状态设计，对工程质量要求极高（文档原话："一般的工程师写不出来"）。

**严重程度：** 中高（影响多用户并发可靠性）

**缓解策略：**
1. **MVP 阶段简化**：阶段一用 SQL Server 表（`ai_route_table`）替代 etcd 存路由，用 Redis（JNPF 已有）替代 etcd 的 Watch 机制；etcd 在阶段六（生产加固）引入
2. **沙箱先用进程级隔离**：MVP 阶段沙箱 = 独立的 Node.js Worker Process，通过 `projectId` 标记上下文；容器级沙箱（Docker）留到阶段六
3. **强制 Code Review**：软路由核心逻辑（路由查询 + 沙箱分配）必须经过 code-reviewer 子代理审查后才可合并
4. **路由表变更幂等设计**：软路由的所有操作必须幂等（多次执行结果相同），防止网络抖动导致沙箱重复创建

---

### 风险 4：IR 事件流随项目规模增长导致查询性能下降

**风险描述：** 一个复杂项目（20+ 业务事件 × 9 SA 步骤 × 多次修复）可能产生 1000+ 条 IR 事件，投影引擎重建时间过长。

**严重程度：** 中（影响系统响应速度）

**缓解策略：**
1. **快照 + 增量重建**：`ai_ir_fragment_snapshots` 存储每个片段的当前状态快照；投影引擎只需重放最后一个快照之后的事件，而不是全量重放
2. **快照触发时机**：每当 IR 片段稳定性升级（draft → stable → locked），立即生成新快照。快照间的事件数量通常 < 20 条
3. **索引优化**：`ix_ir_events_project` 覆盖索引已包含查询所需的全部字段（见第5节），避免全表扫描
4. **归档策略**：项目 `DeploymentCompleted` 后，将 `ai_ir_events` 中该项目的早期草稿事件（`F_FragmentVersion < 当前版本 - 5`）移至归档表，保持主表数据量可控

---

### 风险 5：Bug修复 Skill 的回溯精度不足

**风险描述：** Bug 修复 Skill 依赖事件流回溯定位根因，但如果缺陷涉及多个 IR 片段的组合错误（而非单一片段错误），可能无法准确识别最小受影响范围，导致过度触发下游 Skill 重算（全局退回而非最小化修复）。

**严重程度：** 中（影响 Bug 修复效率，但不影响系统正确性）

**缓解策略：**
1. **错误堆栈关键词提取**：从 Bug 报告的错误堆栈中提取类名、方法名、字段名，用于精确匹配 IR 事件中的 `fragmentId`（不依赖 LLM 语义理解）
2. **约束传播引擎辅助**：约束传播引擎维护 IR 片段间的依赖图（哪个 DDL 引用了哪个 EventSpec），Bug 修复 Skill 查询此图获得精确的受影响集合
3. **保守策略优先**：当无法确定最小影响范围时，采用"按层次保守回退"策略（只退回发现问题的那一层，不跨层退回）
4. **人工介入通道**：当 Bug 修复 Skill 确认度低于阈值时，追加 `HumanReviewRequired` 事件，由人工工程师审查后确认回溯范围

---

### 风险 6（补充）：LLM 幻觉在 IR-1 精炼阶段注入错误业务规则

**风险描述：** IOI 框架只能校验已显式声明的不变量，但 LLM 可能在精炼过程中"发明"原始需求中不存在的业务规则，而这些规则未被不变量覆盖，因此通过了校验。

**缓解策略：**
1. **需求文本锚定**：每次 LLM 精炼调用，上下文中必须包含原始需求文档的相关段落（而非仅包含 IR-0 骨架），让 LLM 的推理锚定在用户实际说过的话上
2. **用户确认节点**：IR-1 的每个 EventSpec 在写入 `stable` 之前，向用户展示关键业务规则列表，要求用户确认（Human-in-the-Loop，阶段二验收标准的核心）
3. **规则来源标记**：每条业务规则在 IR-1 中标记来源：`user-stated`（用户明确说过）/ `inferred`（LLM 推断）/ `seed-data`（来自种子库）。`inferred` 类规则在用户确认前不得进入 `stable`

---

## 附录 A：IR 片段类型枚举

```typescript
type IRFragmentType =
  | 'IR0_Skeleton'           // 产品经理 Skill 产出
  | 'IR1_EventSpec'          // 系统需求分析师 Skill 产出（每个业务事件一个）
  | 'IR2_Architecture'       // 架构设计 Skill 产出
  | 'IR2_SystemDesign'       // 总体设计 Skill 产出
  | 'IR2_DDL'                // 数据库设计 Skill 产出
  | 'IR2_UIDesign'           // UI 设计 Skill 产出（每个页面一个）
  | 'IR3_GeneratedCode'      // 开发 Skill 产出
  | 'IR3_TestSuite'          // 测试 Skill 产出
  | 'IR3_TestReport'         // 测试 Skill 产出（测试结果）
  | 'IRE_BugReport'          // Bug 修复 Skill 输入
  | 'IRE_BugFix'             // Bug 修复 Skill 产出
  | 'IRE_ArchSmell';         // 架构异味报告
```

## 附录 B：A2A Topic 命名规范

```
格式：project.{tenantId}.{projectId}.{eventType}

示例：
  project.T001.P100.skeleton-ready         ← 产品经理完成
  project.T001.P100.sa-step-completed      ← SA 步骤完成
  project.T001.P100.analysis-completed     ← 需求分析完成
  project.T001.P100.architecture-locked    ← 架构设计锁定
  project.T001.P100.design-all-stable      ← 三个设计 Skill 全部完成
  project.T001.P100.code-generated         ← 代码生成完成
  project.T001.P100.test-passed            ← 测试通过
  project.T001.P100.deployed               ← 部署完成
  project.T001.P100.bug-reported           ← 缺陷报告
```

---

## 附录 C：Skill 激活顺序总览

```
用户提交需求
    │
    ▼ pm-skill（激活条件：用户提交）
    │     订阅: project.*.requirements.submitted
    │     产出: project.{T}.{P}.skeleton-ready
    │
    ▼ analyst-skill（激活条件：IR-0 stable）
    │     订阅: project.{T}.{P}.skeleton-ready
    │     产出: project.{T}.{P}.analysis-completed（所有 EventSpec stable）
    │
    ├──► architect-skill（激活条件：IR-1 all stable）
    ├──► db-design-skill （激活条件：IR-1 all stable）  ← 三者并行
    └──► ui-design-skill （激活条件：IR-1 all stable）
    │
    ▼ system-design-skill（激活条件：arch + db + ui 全部 stable）
    │     产出: project.{T}.{P}.design-all-stable
    │
    ▼ developer-skill（激活条件：IR-2 SystemDesign locked）
    │     产出: project.{T}.{P}.code-generated
    │
    ▼ tester-skill（激活条件：IR-3 code stable）
    │     产出: project.{T}.{P}.test-passed 或 test-failed
    │
    ▼ deploy-skill（激活条件：test-passed）
    │     产出: project.{T}.{P}.deployed
    │
    ─────────────────── Bug 修复（随时可触发）──────────────────────
    ▼ bugfix-skill（激活条件：bug-reported 事件）
          产出: 受影响 Skill 的激活事件（最小化集合）
```

---

## 7. 可行性核查：2小时内生成完整企业管理系统 + JNPF 工具可定制化

> 本节是对原计划的系统性复查，基于 JNPF 源码实证（`VisualDevEntity.cs` + `FlowTemplateJsonEntity.cs`），补充两个关键设计：**混合输出策略**（解决可定制性）和**关键路径优化**（确保2小时可达）。

### 7.1 可行性结论

| 问题 | 结论 | 前提条件 |
|---|---|---|
| **2小时内完成？** | **是，最优场景 50 分钟，保守场景 110 分钟** | 种子数据覆盖率 ≥ 60%；SA 分析按事件并行 |
| **JNPF 工具可定制化？** | **是，但需要补充"混合输出策略"** | UI/工作流输出 JNPF 原生元数据格式，不是中间 IR |

原计划存在两个关键设计缺口，必须在工程实施前补充完整：

- **缺口 A**：UI 设计 Skill 输出的是 `FormPageIR`（TypeScript 抽象类型），而非 JNPF 手工平台实际读写的 `BASE_VISUAL_DEV.F_FORM_DATA` JSON。二者中间隔了一层转换，导致 JNPF 视觉工具无法直接打开 AI 生成的表单。
- **缺口 B**：工作流驱动的业务事件（请假审批、采购审批等），SA Step 2（BPM）产出的是抽象流程描述，而非 JNPF 工作流设计器实际读写的 `FLOW_TEMPLATE_JSON.F_FLOW_TEMPLATE_JSON` 格式，导致 JNPF 工作流引擎无法直接驱动。

---

### 7.2 JNPF 原生元数据架构（实证基础）

通过阅读源码，JNPF 手工平台的可视化工具链建立在两张核心元数据表上：

```
BASE_VISUAL_DEV（表 VisualDevEntity）
├── F_FORM_DATA      NVARCHAR(MAX)  ← 表单组件树 JSON（JNPF 表单设计器的"源文件"）
├── F_COLUMN_DATA    NVARCHAR(MAX)  ← 列表配置 JSON（列、搜索条件、操作按钮）
├── F_TABLES_DATA    NVARCHAR(MAX)  ← 关联的数据库表定义
├── F_WEB_TYPE       INT           ← 1=纯表单 2=表单+列表 3=系统表单 4=数据视图
├── F_ENABLE_FLOW    INT           ← 是否启用工作流
└── F_TYPE           INT           ← 1=Web设计 3=流程表单 4=Web表单

FLOW_TEMPLATE + FLOW_TEMPLATE_JSON（FlowTemplateEntity + FlowTemplateJsonEntity）
├── FLOW_TEMPLATE.F_FULL_NAME     ← 流程名称
├── FLOW_TEMPLATE.F_TYPE          ← 0=发起流程 1=功能流程
└── FLOW_TEMPLATE_JSON.F_FLOW_TEMPLATE_JSON  NVARCHAR(MAX) ← 完整 BPMN-like JSON
```

**关键洞察**：JNPF 手工平台是一个**元数据驱动的运行时**。它不渲染 Vue 源码，而是在运行时读取 `F_FORM_DATA` JSON 动态渲染表单、读取 `F_FLOW_TEMPLATE_JSON` 驱动审批流。这意味着：**AI 只要能生成这两份 JSON，生成的系统就天然可以被 JNPF 的视觉工具打开和修改。**

---

### 7.3 混合输出策略（Hybrid Output Strategy）— 补充设计

废弃原计划中"UI 设计 Skill → FormPageIR → 代码生成"的路径，改为：

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       混合输出策略（Strategy C）                          │
│                                                                          │
│  UI层（可视化工具可编辑）          后端层（代码工具可编辑）                │
│  ┌─────────────────────────┐      ┌──────────────────────────────────┐  │
│  │ AI 生成 VisualDev JSON  │      │ AI 生成 C# Service + Entity       │  │
│  │ → 写入 BASE_VISUAL_DEV  │      │ → 通过 .vm 模板生成               │  │
│  │   F_FORM_DATA           │      │ → dotnet build 验证               │  │
│  │   F_COLUMN_DATA         │      └──────────────────────────────────┘  │
│  │   F_TABLES_DATA         │                                            │
│  └─────────────────────────┘      ┌──────────────────────────────────┐  │
│                                   │ AI 生成 DDL                       │  │
│  ┌─────────────────────────┐      │ → 执行建表                        │  │
│  │ AI 生成 Flow JSON        │      │ → 注册到 JNPF 数据管理            │  │
│  │ → 写入 FLOW_TEMPLATE     │      └──────────────────────────────────┘  │
│  │   FLOW_TEMPLATE_JSON    │                                            │
│  └─────────────────────────┘                                            │
│                                                                          │
│  用户用 JNPF 表单设计器改 UI     用户用 IDE / JNPF 代码工具改后端         │
│  用户用 JNPF 工作流设计器改审批流  用户用 JNPF 数据管理改表结构            │
└─────────────────────────────────────────────────────────────────────────┘
```

**UI 设计 Skill 的输出格式修正（覆盖原第3节 Skill 6 的定义）：**

```typescript
// 修正：UI 设计 Skill 不再输出 FormPageIR（抽象类型）
// 而是直接输出 JNPF VisualDev 原生 JSON 格式

interface IR2_UIDesign_NativeJNPF {
  // 写入 BASE_VISUAL_DEV 的完整记录
  visualDevRecord: {
    fullName: string;              // 功能名称（如"工单管理"）
    enCode: string;                // 功能编码（如"workorder-manage"）
    webType: 1 | 2 | 3 | 4;       // 1=纯表单 2=表单+列表（最常用）
    enableFlow: 0 | 1;            // 是否启用工作流（来自 SA Step 2 BPM 分析）
    type: 1 | 3 | 4;              // 1=Web设计
    tablesData: string;           // JSON: 关联表定义（来自 IR-2 DDL）
    formData: string;             // JSON: 表单组件树（JNPF 表单设计器格式）
    columnData: string;           // JSON: 列表配置（JNPF 列表设计器格式）
    appColumnData?: string;       // JSON: App端列表配置（可选）
  };
  // 如果有工作流，同时产出 FlowTemplate 记录
  flowTemplateRecord?: {
    fullName: string;
    type: 0 | 1;                  // 0=发起流程
    flowTemplateJson: string;     // JSON: JNPF BPMN 流程定义（工作流设计器格式）
  };
}
```

**`formData` 的 JSON 结构（JNPF 原生格式，对应 F_FORM_DATA）：**

```json
{
  "fields": [
    {
      "__config__": {
        "label": "工单编号",
        "tag": "el-input",
        "tagIcon": "input",
        "required": true,
        "dataType": "static"
      },
      "__vModel__": "workOrderCode",
      "placeholder": "请输入工单编号",
      "style": { "width": "100%" },
      "clearable": true
    },
    {
      "__config__": {
        "label": "状态",
        "tag": "el-select",
        "tagIcon": "select",
        "required": true,
        "dataType": "dictionary",
        "dictionaryType": "work_order_status"
      },
      "__vModel__": "status"
    }
  ],
  "labelWidth": 100,
  "labelPosition": "right",
  "size": "medium"
}
```

> **AI 生成此 JSON 的方式**：不是 LLM 自由发挥，而是：
> 1. 从 IR-1 确认字段 → 字段类型映射到 JNPF 组件（string→el-input，enum→el-select，FK→关联选择器）
> 2. 从 IR-1 IOI 约束 → 映射到组件的 `required` / `min` / `max` / `pattern` 属性
> 3. 组件树结构由模板固化，LLM 只填充字段名、标签、字典类型等业务参数
> 4. **不调用 LLM**：整个 `formData` 生成是确定性的 IR→JSON 映射，速度 < 1 秒

**`flowTemplateJson` 的结构（JNPF 工作流原生格式，对应 F_FLOW_TEMPLATE_JSON）：**

来源：SA Step 2（BPM）的产出，需要从抽象 BPM 图转换为 JNPF 的节点-连线格式：

```json
{
  "nodes": [
    { "id": "start", "type": "start-event", "name": "发起" },
    {
      "id": "approve-1",
      "type": "approval",
      "name": "直属主管审批",
      "approveType": 1,
      "approvers": [{ "type": "role", "roleId": "direct-manager" }],
      "formAuth": [
        { "fieldId": "workOrderCode", "auth": "read" },
        { "fieldId": "amount", "auth": "write" }
      ]
    },
    { "id": "end", "type": "end-event", "name": "结束" }
  ],
  "edges": [
    { "source": "start", "target": "approve-1" },
    { "source": "approve-1", "target": "end", "label": "同意" }
  ]
}
```

> `flowTemplateJson` 的生成：SA Step 2（BPM）产出抽象流程图 → `bpm-to-jnpf-converter`（确定性转换器）映射为 JNPF 节点格式 → LLM 只负责确认审批人规则和字段权限（`formAuth`）。

---

### 7.4 关键路径分析：2小时可达性

#### 7.4.1 典型企业管理系统的规模基准

以"人事+考勤+请假"模块为例（中等复杂度，典型通用企业管理系统）：

| 维度 | 数量 |
|---|---|
| 业务事件数 | 12个 |
| 其中 auto 复杂度（种子全覆盖） | 4个（如"查看列表"、"导出数据"） |
| 其中 simple 复杂度（种子部分覆盖） | 5个（如"新增员工"、"修改考勤"） |
| 其中 complex 复杂度（需多轮对话） | 3个（如"请假审批"、"跨月结算"） |
| 数据库表数 | ~15张 |
| 带工作流的事件 | 2个 |

#### 7.4.2 关键路径时序（混合策略 + 并行优化后）

```
时间轴（分钟）
 0    5    10   15   20   25   30   35   40   45   50   55   60   65
 │                                                                  │
 ├────┤
 PM Skill（3-5 min）→ IR-0：12个业务事件清单 + 角色矩阵 + 15张表草案

      ├──────────────────────────────┤
      SA 分析 auto×4（种子自动填充，无LLM）：~30秒 × 4 = 2 min
      SA 分析 simple×5（种子+1次LLM）：~2 min × 5 = 10 min（5个并行，实际10 min）
      SA 分析 complex×3（多轮对话）：~5 min × 3 = 15 min（3个并行，实际15 min）
      → SA 分析总用时：~17 min（并行执行）

                              ├──────────────┤
                              DB Design Skill：DDL生成（~10 min）
                              （与SA分析后半段并行启动，收到stable片段即开始）

                                    ├────────────┤
                                    UI Design Skill：
                                    VisualDev JSON生成（确定性映射，~3 min/功能）
                                    12个功能 × 但大量并行 ≈ 8 min

                                          ├──────┤
                                          Flow JSON生成（2个工作流，~4 min）

                              ├──────┤
                              Arch Skill：架构决策（~8 min，与DB并行）

                                                   ├────────┤
                                                   Backend codegen（~5 min）
                                                   dotnet build（~3 min）

                                                            ├──┤
                                                            JNPF Visual Dev注册
                                                            + 菜单创建（~3 min）

                                                               ├──┤
                                                               冒烟测试（~3 min）
 │                                                                  │
 0                                                                 ~55 min
```

**最优场景：约 50-55 分钟**（种子数据质量好，事件依赖简单，API 响应正常）

**保守场景：约 95-110 分钟**（complex 事件多、需要多轮用户确认、SA步骤部分串行）

**绝对上限（仍在2小时内）：约 110 分钟**，前提是种子库覆盖 ≥ 30%

#### 7.4.3 保证2小时的五个硬性设计约束

这五条必须写入工程实现规范，否则2小时目标不可保证：

**约束 HC-1：SA 分析必须按事件并行，不得串行等待全部前序事件完成。**

原计划第3节描述"按依赖顺序处理每个事件"——这句话容易被误解为完全串行。修正为：

```
事件依赖图中无依赖关系的事件 → 立即并行启动 SA 分析
有依赖关系的事件 → 等待上游事件 EventSpecConfirmed 后立即启动（不等其他独立事件）
并发度上限：同一项目最多 5 个事件的 SA 分析同时运行（Token 预算控制）
```

**约束 HC-2：`auto` 复杂度事件不得触发 LLM 调用，全部由种子数据自动填充。**

```typescript
if (event.complexityHint === 'auto') {
  // 从种子库直接查询匹配的事件模板，填充所有 SA 步骤
  const template = await seedDataMCP.findEventTemplate(event.eventName, industry);
  if (template.coverageScore > 0.85) {
    // 直接产出 EventSpecConfirmed，无 LLM 调用
    return applyTemplate(event, template);
  }
  // 覆盖度不足时降级为 simple
  event.complexityHint = 'simple';
}
```

**约束 HC-3：UI 设计 Skill 的 `formData` / `columnData` 生成必须是确定性映射，不得调用 LLM。**

LLM 只负责：`flowTemplateJson` 中的审批人规则描述和字段权限配置。UI 布局是纯规则映射。

**约束 HC-4：Human-in-the-Loop 只有一个强制检查点：IR-0 骨架用户确认。**

禁止在每个 EventSpec 确认后停下来等用户；禁止在 UI 预览后等用户逐页确认。全流程只有一个强制人工确认点（骨架确认）。其余步骤用户可选择性干预（通过实时进度界面），但不阻塞流水线。

**约束 HC-5：`dotnet build` 必须在 60 秒内完成（增量编译），否则判定为超时失败。**

开发 Skill 生成的代码必须是增量的（只添加新文件，不修改已有文件）。增量编译时间远低于全量编译。沙箱预热（预加载 .NET SDK + JNPF 依赖）须在项目创建时完成，不占用生成时间。

---

### 7.5 JNPF 工具定制化能力矩阵

按照混合输出策略，AI 生成的系统各层的定制化能力如下：

| 层次 | AI 产出物 | 存储位置 | 可用的 JNPF 工具 | 定制深度 |
|---|---|---|---|---|
| **表单 UI** | `BASE_VISUAL_DEV.F_FORM_DATA` JSON | JNPF 在线数据库 | **表单设计器**（拖拽） | 全量可编辑：增删字段、调整布局、修改校验规则、换组件类型 |
| **列表 UI** | `BASE_VISUAL_DEV.F_COLUMN_DATA` JSON | JNPF 在线数据库 | **列表设计器**（配置面板） | 全量可编辑：调整列、搜索条件、操作按钮、排序 |
| **工作流** | `FLOW_TEMPLATE_JSON.F_FLOW_TEMPLATE_JSON` JSON | JNPF 在线数据库 | **工作流设计器**（节点拖拽） | 全量可编辑：增删审批节点、修改条件分支、调整审批人规则 |
| **数据模型** | DDL + JNPF 数据管理注册 | SQL Server 物理表 | **JNPF 数据管理**（在线DDL） | 部分可编辑：增字段（ALTER TABLE ADD）；删字段/改类型需谨慎 |
| **后端业务逻辑** | C# Service / Entity 代码 | 源码文件 | **IDE 编辑** | 全量可编辑：任意 C# 代码修改 |
| **API** | `IDynamicApiController` 自动生成 | 运行时自动 | 无需编辑（自动） | 通过修改 Service 方法间接调整 |
| **权限** | 菜单 + 角色配置 | JNPF 系统表 | **JNPF 权限管理**（配置面板） | 全量可编辑：绑定角色、设置菜单权限、字段权限 |

**用户视角的定制化工作流（无需接触 AI 代码）：**

```
AI 生成完成
    │
    ▼ 产品经理/业务人员（JNPF 视觉工具）：
    │   打开"表单设计器" → 调整字段顺序、修改必填规则、添加关联选择器
    │   打开"工作流设计器" → 增加会签节点、修改条件分支、调整抄送人
    │   打开"列表设计器" → 调整列显示、添加批量操作按钮
    │
    ▼ 开发工程师（IDE）：
    │   修改 C# Service 中的业务规则（AI 生成的 Service 有完整注释）
    │   添加定制化验证逻辑、扩展 API
    │
    ▼ DBA（JNPF 数据管理 / SQL Server Management Studio）：
        扩展表字段、添加索引、调整外键关系
```

---

### 7.6 需要补充到实施路线图的工程任务

基于上述分析，以下任务必须加入各阶段实施计划：

#### 补充到阶段二（W3-W4）：

| 任务 | 描述 | 工期 |
|---|---|---|
| `bpm-to-jnpf-converter` | 将 SA Step 2 BPM 抽象流程图转换为 JNPF `FlowTemplateJson` 格式的确定性转换器 | 2天 |
| `ir1-to-visualdev-mapper` | 将 IR-1 EventSpec 字段映射到 JNPF `formData` / `columnData` 组件树的确定性映射器 | 2天 |
| 事件并行调度器 | 在 `SAOrchestrator` 中实现事件并行调度（最大并发度控制 + 依赖图拓扑排序） | 1天 |

#### 补充到阶段三（W5-W6）：

| 任务 | 描述 | 工期 |
|---|---|---|
| `ui-skill-native-output` | 修改 UI 设计 Skill 的输出目标：直接产出 `VisualDevFormData` + `FlowTemplateJson`，废弃 FormPageIR 中间层 | 2天 |
| JNPF 原生格式验证器 | 验证 AI 产出的 `formData` / `flowTemplateJson` 能被 JNPF 的视觉工具正确加载（Playwright 自动化测试） | 1天 |

#### 补充到阶段五（W9-W10）：

| 任务 | 描述 | 工期 |
|---|---|---|
| `jnpf-visual-dev-register` | Deploy Skill 增加注册步骤：写入 `BASE_VISUAL_DEV` + `FLOW_TEMPLATE` + `FLOW_TEMPLATE_JSON` + 菜单配置 | 2天 |
| 2小时基准测试 | 用3个标准场景（简单/中等/复杂）跑全链路计时，验证各阶段耗时符合预期 | 1天 |

---

### 7.7 种子数据库：决定2小时目标能否达成的最关键因素

原计划将种子数据库定位为"辅助加速"。基于上述关键路径分析，**种子数据库实际上是2小时目标的决定性因素**，必须将其提升为一级基础设施，与 IR 事件流并列。

**种子库质量要求（最低阈值）：**

| 行业 | 标准场景覆盖数 | auto 事件覆盖率 | simple 事件覆盖率 |
|---|---|---|---|
| 人事/HR | ≥ 30 个标准事件模板 | ≥ 80% | ≥ 60% |
| 制造/工厂 | ≥ 25 个标准事件模板 | ≥ 70% | ≥ 55% |
| 工程/施工 | ≥ 20 个标准事件模板 | ≥ 65% | ≥ 50% |
| 通用（默认） | ≥ 40 个标准事件模板 | ≥ 75% | ≥ 60% |

**种子数据的内容结构（每条种子模板）：**

```json
{
  "templateId": "seed-hr-leave-apply-001",
  "industry": ["hr", "general"],
  "eventNamePatterns": ["请假申请", "假期申请", "年假申请"],
  "complexityHint": "complex",
  "saStepOutputs": {
    "scope": { "moduleContext": "HR", "actors": ["员工", "直属主管", "HR专员"] },
    "dfd": "...（预存数据流图JSON）",
    "bpm": "...（预存BPMN JSON，含2级审批标准流程）",
    "dict": {
      "leaveType": ["年假", "事假", "病假", "调休"],
      "approvalStatus": ["待审批", "审批中", "已通过", "已拒绝", "已撤回"]
    },
    "entityDrafts": [
      {
        "tableName": "HR_LEAVE_APPLY",
        "fields": [
          { "name": "F_LEAVE_TYPE", "type": "NVARCHAR(20)", "required": true },
          { "name": "F_START_DATE", "type": "DATETIME", "required": true },
          { "name": "F_END_DATE", "type": "DATETIME", "required": true },
          { "name": "F_DAYS", "type": "DECIMAL(4,1)", "required": true },
          { "name": "F_REASON", "type": "NVARCHAR(500)", "required": false }
        ]
      }
    ],
    "formData": "...（预存的 JNPF formData JSON）",
    "columnData": "...（预存的 JNPF columnData JSON）",
    "flowTemplateJson": "...（预存的 JNPF 工作流 JSON，含标准2级审批）"
  },
  "coverageScore": 0.92,
  "lastUpdatedAt": "2026-07-03"
}
```

> **运作机制**：`complexityHint: complex` 的请假申请事件，覆盖分 0.92 > 0.85 阈值，系统直接加载种子模板并展示给用户确认，用户可在种子基础上微调（如把"2级审批"改为"3级审批"），而不是从零对话。这将 complex 事件的对话轮次从 4 轮降为 1-2 轮（只确认差异），耗时从 5 分钟降为 1.5 分钟。

**种子数据建设计划（纳入阶段一，与 IR 基础设施并行）：**

| 阶段 | 种子数量 | 覆盖场景 |
|---|---|---|
| 阶段一（W1-W2） | ≥ 40 条 | 通用 HR/OA 场景（请假/报销/审批/考勤） |
| 阶段二（W3-W4） | + 30 条 | 制造业（工单/质检/设备/物料） |
| 阶段三（W5-W6） | + 30 条 | 工程/施工（合同/进度/安全/验收） |
| 阶段六（W11-W12） | + 50 条（用户反馈沉淀） | 基于真实项目运行日志，自动提炼高频模式 |

---

### 7.8 最终结论

基于上述补充分析，原计划在以下两点需要修正：

| 修正项 | 原计划 | 修正后 |
|---|---|---|
| UI 设计 Skill 输出 | FormPageIR（TypeScript 抽象类型） | **JNPF VisualDev 原生 JSON**（直接写入 `BASE_VISUAL_DEV`） |
| 工作流产出 | 抽象 BPM 图（IR-1 内） | **JNPF FlowTemplateJson**（直接写入 `FLOW_TEMPLATE_JSON`） |
| 种子库定位 | 辅助加速组件 | **一级基础设施**，与 IR 事件流并列，决定2小时目标成败 |
| SA 分析并发 | 按依赖顺序（易被误解为串行） | **显式并行**（最大并发 5 事件，有依赖的等上游完成即启动） |
| Human-in-the-Loop | 未明确检查点数量 | **严格限定：全流程仅1个强制检查点（IR-0骨架确认）** |

**修正后的目标承诺**：

- 标准通用企业管理系统（≤15个业务事件，≤20张表）：**50-75 分钟**内完成首版部署
- 中等复杂度系统（≤25个业务事件，≤35张表，含3-5个工作流）：**75-110 分钟**内完成
- 复杂系统（>25个业务事件）：可能超过2小时，需要拆分为多期交付

生成的首版系统可在 JNPF 手工低代码平台中用**表单设计器、工作流设计器、列表设计器、权限管理**进行全量可视化定制，无需触碰 AI 生成的源代码。

---

*文档版本 v1.1 | 最后更新：2026-07-03（补充混合输出策略 + 2小时可行性分析）*

---

## 8. 六目标达成性核查与最终落地补全

> 本节是对全文计划的逐目标正式核查。基于现有 JNPF-AI 代码资产（`PipelineOrchestratorService`、`SandboxManager`、`SandboxModule`、Phase A/B 施工包）做实证比对，识别残余缺口并给出精确补全措施。

### 8.0 现有 JNPF-AI 代码资产盘点（实证基础）

通过代码审查确认的已有能力：

| 已有组件 | 路径 | 当前能力 | 计划中的角色 |
|---|---|---|---|
| `PipelineOrchestratorService` | `backend/application/…/AIDevelopment/` | 5阶段流水线调度（串行） | 改造为事件驱动 DAG Orchestrator |
| `SandboxManager` | `backend/application/…/AIDevelopment/Sandbox/` | Docker沙箱 CRUD，SemaphoreSlim(5) | 扩展为沙箱池 + 队列调度 |
| `SandboxModule` | `backend/application/JNPF.API.Entry/Modules/` | API 注册入口 | 扩展 AI 模块路由注册 |
| SSE 通道（`_sseChannels[pipelineId]`） | `PipelineOrchestratorService` 内 | 单项目进度推送 | 复用，扩展为多项目并发推送 |
| Phase A 施工包（已落地） | `docs/AI原生开发/2、施工包/` | 工作区隔离路径、沙箱绑定流水线、写文件 Hook | 作为多租户隔离基础设施 |
| Phase B 施工包（计划中） | 同上 | 前端预览（studio-preview）、沙箱队列、安全修正 | 继续执行，与本计划并行 |
| `BASE_VISUAL_DEV` 元数据表 | JNPF 手工平台数据库 | 表单/列表 JSON 存储 | UI 设计 Skill 的直接输出目标 |
| `FLOW_TEMPLATE_JSON` 元数据表 | JNPF 手工平台数据库 | 工作流 BPMN JSON 存储 | 工作流生成的直接输出目标 |

---

### 8.1 目标一核查：10个符合2026主流水准的 Skills

**核查结论：✅ 计划已覆盖，有一处细化补充**

原计划第3节已完整设计10个 Skill 的角色定义、信息需求、推理机制、产出物。

**补充：Skills 与现有 JNPF-AI 代码的接入点映射**

10个 Skill 不是从零建设，而是在 `PipelineOrchestratorService` 现有阶段基础上**重构和扩展**：

```
现有 5 阶段流水线          →  新 10 Skill 映射
─────────────────────────────────────────────
Stage 0: SA 门控           →  产品经理 Skill（IR-0）
Stage 1: 骨架预分析        →  产品经理 Skill 的 ToT 推理阶段
Stage 2: 事件精炼          →  系统需求分析师 Skill（IOI + SA九步）
Stage 3: SA 综合分析       →  系统需求分析师 Skill 驱动的 SA Orchestrator
Stage 4: 代码生成          →  架构/总体/数据库/UI 设计 + 开发 Skill
（现无） 测试阶段           →  新增：测试 Skill
（现无） 部署阶段           →  新增：部署 Skill（集成 JNPF VisualDev 注册）
（现无） Bug 修复          →  新增：Bug 修复 Skill
```

**IBaseSkill 注册到现有 PipelineOrchestratorService 的方式：**

```typescript
// 现有 PipelineOrchestratorService 中，按阶段调用 Skill
class PipelineOrchestratorService {
  private skillRegistry: SkillRegistry;  // 新增：Skill 注册表

  async executeStage(pipelineId: string, stage: PipelineStage): Promise<void> {
    const skill = this.skillRegistry.getSkillForStage(stage);
    
    // 从 ir_events 重建当前 IR 快照（无状态设计）
    const irSnapshot = await this.irProjectionEngine.rebuild(pipelineId);
    
    // 验证输入条件
    const validation = skill.validateInput(irSnapshot);
    if (!validation.passed) throw new PipelineError(validation.errors);
    
    // 执行 Skill（流式输出 IR 事件）
    for await (const irEvent of skill.reason({ irSnapshot, pipelineId, tenantId })) {
      await this.irEventStore.append(irEvent);       // 持久化事件
      await this.sseChannels.push(pipelineId, irEvent); // 实时推送前端
    }
  }
}
```

---

### 8.2 目标二核查：需求分析→架构→开发→测试→部署→Bug修复 完整链条

**核查结论：✅ 已覆盖，补充现有 PipelineOrchestratorService 的改造边界**

**现有串行流水线改造为事件驱动 DAG 的最小改动路径：**

```
改造前（5阶段串行）：
Stage0 → Stage1 → Stage2 → Stage3 → Stage4

改造后（事件驱动 DAG）：
IR事件流触发 → Skill激活（按信息需求声明判定）

具体改造：
- 不删除现有 Stage 枚举，将其改为 IR 事件类型的别名
- PipelineOrchestratorService 中的 switch(stage) 改为订阅 ir_events 表的新事件
- 每个 Skill 的 reason() 方法替代原来的阶段处理逻辑
- 新增阶段（测试/部署/Bug修复）只需新增 Skill 和对应的 IR 事件类型
```

**手工平台完全隔离的技术保证（对应"不丢失已有功能"要求）：**

| 隔离维度 | 手工平台 | AI 平台 | 隔离机制 |
|---|---|---|---|
| 数据库表 | `BASE_VISUAL_DEV` 等已有表 | `ai_ir_events`、`ai_projects` 等 `ai_` 前缀表 | 表名隔离 + 租户 ID 过滤 |
| 代码路径 | `backend/modularity/`（禁止 AI 修改） | `backend/modularity/JNPF.AI/`（AI 专属模块） | Phase A A3 Hook 白名单 |
| 前端路由 | `/jnpf/` 路由（手工功能） | `/jnpf/ai/` 路由（AI 功能） | Vue Router 命名空间 |
| API 路由 | `/api/{module}/` | `/api/ai/` | ASP.NET 路由前缀 |
| 文件路径 | `{SystemPath}/CodeGenerate/` | `{SystemPath}/StudioWorkspace/{tenantId}/{pipelineId}/` | Phase A A2 路径切换 |
| 功能开关 | 始终开启 | `Features:AIEnabled` Feature Flag 控制 | `appsettings.json` |

手工平台回归测试门控：每次 AI 模块变更后，必须执行以下验证：

```bash
# 传统 VisualDev 代码生成不受影响验证（在 CI 中执行）
1. 触发 VisualDev 生成请求 → 验证产物落盘到 CodeGenerate/ 而非 StudioWorkspace/
2. 打开 JNPF 手工表单设计器 → 验证 BASE_VISUAL_DEV 读写正常
3. 发起工作流 → 验证 FLOW_TEMPLATE_JSON 流转正常
4. 验证手工平台的所有已有 API 响应 200（接口冒烟测试）
```

---

### 8.3 目标三核查：SA九步完整实现 + 版本管理 + 增量更新 + Bug友好

**核查结论：⚠️ 原计划覆盖了静态描述，需补充"增量更新"的具体触发机制**

#### 8.3.1 SA九步完整执行保证

SA 步骤的完整性由以下机制双重保证：

**保证一：SAOrchestrator 的串行锁定**

```typescript
class SAOrchestrator {
  private readonly REQUIRED_STEPS: SAStepName[] = [
    'Scope', 'DFD', 'BPM', 'Dict', 'PSpec', 'DecisionTable', 'ER', 'StateMachine', 'UI'
  ];

  async executeForEvent(eventId: string, irSnapshot: IRSnapshot): Promise<void> {
    for (const step of this.REQUIRED_STEPS) {
      // 严格串行：每步必须 Completed 才能执行下一步
      const result = await this.runStep(step, eventId, irSnapshot);
      
      // 追加 SA_Step_Completed 事件（不可跳过）
      await this.irEventStore.append({
        type: 'SA_Step_Completed',
        fragmentId: `${eventId}:${step}`,
        payload: { stepName: step, outputRef: result.outputId }
      });
      
      // 更新 irSnapshot（下一步依赖上一步的产出）
      irSnapshot = await this.irProjectionEngine.rebuild(eventId);
    }
  }
}
```

**保证二：稳定性判定器的 9步计数门控**

```typescript
// 稳定性判定器中的门控逻辑
function isEventSpecStable(eventId: string, events: IREvent[]): boolean {
  const completedSteps = events
    .filter(e => e.type === 'SA_Step_Completed' && e.payload.eventId === eventId)
    .map(e => e.payload.stepName);
  
  const allStepsCompleted = REQUIRED_STEPS.every(step => completedSteps.includes(step));
  return allStepsCompleted;  // 必须9步全部完成，缺一不可
}
```

#### 8.3.2 版本管理机制（IR 版本号）

每个 IR 片段都有独立版本号。任何修改都以新事件追加，不覆盖历史：

```sql
-- ai_ir_fragment_snapshots 中的版本追踪
-- F_CurrentVersion 在每次 EventSpecRevised 事件后 +1
-- F_IrContent 始终是最新的折叠状态

-- 查询某片段的历史版本（时间旅行）：
SELECT e.F_Payload, e.F_CreatedAt, e.F_FragmentVersion
FROM ai_ir_events e
WHERE e.F_ProjectId = @projectId
  AND e.F_FragmentId = @fragmentId
  AND e.F_EventType IN ('EventSpecConfirmed', 'EventSpecRevised', 'SA_Step_Completed')
ORDER BY e.F_Sequence;
-- 从这个事件序列，可以精确还原任意时刻的事件规格内容
```

#### 8.3.3 增量更新机制（关键补充）

**触发场景**：用户修改了已稳定的 IR-1 事件规格（如发现"工单编号"字段规则描述错误）

**触发机制**（此机制在原计划中未充分展开，现补充）：

```typescript
// 系统需求分析师 Skill 收到 EventSpecRevised 事件时
async onEventSpecRevised(revisedEventId: string, changedFields: string[]): Promise<void> {
  
  // 1. 标记被修改的 EventSpec 片段状态退回 in-progress
  await this.stabilityGate.degrade(revisedEventId, 'in-progress');
  
  // 2. 分析哪些 SA 步骤受影响（不是全部重跑）
  const affectedSteps = this.analyzeAffectedSASteps(changedFields);
  // 例如：只修改了字段类型 → 只重跑 Dict + ER（不重跑 Scope/DFD/BPM）
  //      修改了流程规则 → 重跑 BPM + PSpec + DecisionTable
  //      修改了实体关系 → 重跑 ER + StateMachine
  
  // 3. 标记下游受影响的 IR-2 片段
  const downstreamFragments = await this.constraintEngine.findAffected(revisedEventId);
  for (const frag of downstreamFragments) {
    await this.stabilityGate.degrade(frag.fragmentId, 'in-progress');
    await this.irEventStore.append({
      type: 'FragmentInvalidated',
      fragmentId: frag.fragmentId,
      payload: { reason: 'upstream-spec-revised', sourceEventId: revisedEventId }
    });
  }
  
  // 4. 只重跑受影响的 SA 步骤（不重跑已确认的步骤）
  for (const step of affectedSteps) {
    await this.saOrchestrator.rerunStep(revisedEventId, step, currentSnapshot);
  }
  
  // 5. 所有步骤完成后，重新判定稳定性
  // StabilityGate 会自动检测 9步是否全部 Completed，达成后再次升级为 stable
}
```

**受影响 SA 步骤的精确判定表（确定性规则，不依赖 LLM）：**

| 修改类型 | 受影响 SA 步骤 | 不受影响步骤 |
|---|---|---|
| 仅修改字段名/描述 | Dict | 其余8步 |
| 修改字段类型/约束 | Dict, ER | 其余7步 |
| 修改状态机（如添加新状态） | StateMachine, DecisionTable | 其余7步 |
| 修改业务流程节点 | BPM, PSpec | 其余7步 |
| 修改实体关系（外键） | ER, StateMachine | 其余7步 |
| 修改 UI 要求 | UI | 其余8步 |
| 修改角色权限 | Scope, UI | 其余7步 |
| 增加新业务规则 | PSpec, DecisionTable | 其余7步 |

#### 8.3.4 Bug修复与二次开发的可追溯性

```sql
-- Bug 修复者可以执行以下查询，精确了解某个设计决策的来龙去脉：

-- 查询"工单总额字段"是在哪个 SA 步骤、基于哪个版本的输入确定的：
SELECT 
    e.F_EventType,
    e.F_SAStepName,
    e.F_FragmentVersion,
    e.F_CreatedAt,
    JSON_VALUE(e.F_Payload, '$.fieldName') as FieldName,
    JSON_VALUE(e.F_Payload, '$.decisionReason') as DecisionReason
FROM ai_ir_events e
WHERE e.F_ProjectId = @projectId
  AND e.F_Payload LIKE '%totalAmount%'
ORDER BY e.F_Sequence;
-- 结果：能看到"totalAmount 字段在 SA Step 3 Dict 步骤中被确认，
--       当时的输入来自 IR Version 2 的骨架草案，
--       用户在第二轮精炼对话中明确了'含税总额'的业务含义"
```

---

### 8.4 目标四核查：Prompt + 种子数据 + SA九步 + IR 有机融合

**核查结论：⚠️ 原计划概念完整，需补充各 Skill Prompt 的实际结构模板**

#### 8.4.1 Prompt 动态构建机制（四要素融合的核心）

每个 Skill 的 Prompt 由 `ContextBuilder` 动态组装，四要素各司其职：

```
┌────────────────────────────────────────────────────────────┐
│              Skill X 的 Prompt 动态组装结构                  │
├────────────────────────────────────────────────────────────┤
│ ① System Prompt（Skill 角色定义，静态）                      │
│   "你是系统需求分析师，负责将骨架蓝图转化为精确的字段级规格..."  │
├────────────────────────────────────────────────────────────┤
│ ② IR 上下文（来自 ir_events 投影，动态）                      │
│   "当前项目已确认的骨架（IR-0）：{ir_snapshot.skeleton}"       │
│   "当前业务事件 EVT-003 的依赖事件 EVT-001 已确认规格：{...}"   │
│   → 只注入"信息需求声明"中声明的 IR 片段类型，不全量注入        │
│   → 超出 Token 阈值时，自动压缩为字段摘要版本                  │
├────────────────────────────────────────────────────────────┤
│ ③ 业务领域种子数据（来自 seed-data-mcp，动态）                  │
│   "本行业（制造业）的标准工单模板：{seed.workorder_template}"   │
│   "本企业已有的数据字典：{seed.enterprise_dict}"               │
│   → 只注入与当前事件相关的种子项（关键词匹配，≤20条）           │
│   → 标注来源类型：user-stated / seed-data / inferred          │
├────────────────────────────────────────────────────────────┤
│ ④ SA 九步上下文（来自上游步骤产出，动态）                       │
│   "SA Step 1 DFD 已确定的数据流：{step1_output}"              │
│   "SA Step 2 BPM 已确定的业务流程：{step2_output}"            │
│   → 当前步骤能看到所有已完成步骤的产出（累积上下文）             │
│   → SA 步骤间信息严格单向流动（Step N 只能引用 Step 1~N-1）    │
├────────────────────────────────────────────────────────────┤
│ ⑤ IOI 约束声明（来自已确认 EventSpec，动态）                   │
│   "已确认的全局不变量：工单总额=∑工序成本，不得违反"            │
│   → 由 ioi-validator-mcp 在每次 LLM 调用后自动校验             │
└────────────────────────────────────────────────────────────┘
```

#### 8.4.2 系统需求分析阶段的"智能优势 + 完整性"平衡机制

> 目标四特别要求：既能发挥 LLM 智能优势，又确保业务逻辑和数据完整性、一致性。

**三重保障机制：**

**保障A：LLM 负责"理解和提案"，SA步骤负责"形式化和校验"**

```
LLM 的工作：
  "用户说的'报工'可能涉及哪些字段？候选：操作人、设备、工序、数量、时间、质量等级"
  → LLM 理解语义，给出候选清单（发挥智能优势）

SA Step 4（PSpec）的工作：
  "候选字段中，哪些有前置条件？operatorId 必须是已分配到该工单的员工"
  → 形式化为 PreCondition，可被 IOI Validator 机器验证（确保完整性）

SA Step 5（DecisionTable）的工作：
  "质量等级如何影响后续流程？合格→下一工序，不合格→返工，报废→工单终止"
  → 形式化为决策表，无歧义，开发可直接实现（确保一致性）
```

**保障B：用户确认的"锚定机制"**

LLM 提案的所有业务规则，都在 SA Step 4 结束时以结构化方式展示给用户确认：

```json
{
  "pendingRules": [
    {
      "ruleId": "R-003",
      "description": "报工数量不得超过当日计划量",
      "source": "inferred",           // ← 标注来源：是LLM推断的，非用户明确说的
      "confidence": 0.75,
      "preCondition": "quantity <= plannedQuantity",
      "action": "REJECT with message '超出计划量'"
    }
  ],
  "userAction": "confirm | modify | reject"
}
```

`source: "inferred"` 的规则，在用户确认之前，状态保持 `pending`，不写入 `stable` IR。

**保障C：IOI 不变量的全局一致性检测**

任何新确认的业务规则，在写入 IR 前，IOI Validator 自动执行：

```typescript
// IOI 校验示例：新确认"报工数量字段"时
function validateIOIInvariants(newField: FieldSpec, allInvariants: IOIInvariant[]): ValidationResult {
  for (const invariant of allInvariants) {
    // 检查：工单总成本 = 所有工序成本之和
    // 如果新字段涉及"工序成本"，需要验证其类型（decimal）与不变量中的求和操作兼容
    if (invariant.involvedFields.includes(newField.name)) {
      const compatible = checkTypeCompatibility(newField, invariant);
      if (!compatible) return { passed: false, violatedInvariant: invariant.id };
    }
  }
  return { passed: true };
}
```

---

### 8.5 目标五核查：多用户多任务并行

**核查结论：✅ 架构已完整，关键是映射到现有代码的演进路径**

**现有 `SandboxManager` 的演进路径（不是推倒重来）：**

```
现有状态：
  SandboxManager 用 SemaphoreSlim(5) 控制并发
  沙箱以 sandboxId = "pipeline-{pipelineId}" 命名
  无多租户隔离概念（当前 MVP 单租户）

演进目标：
  L1 租户路由：tenantId → 资源池分配
  L2 项目路由：{tenantId, pipelineId} → 具体沙箱实例
  Agent 无状态：每次激活从 ir_events 重建上下文

演进步骤（与 Phase B B3 协同）：
  Step 1（与 Phase B B3 同期）：
    - SandboxManager 的 SemaphoreSlim 改为 Channel<CreateRequest>（队列）
    - 新增 TenantResourcePool（每租户最多 3 个并发项目，可配置）
    - 队列溢出时 SSE 推送 sandbox_queued 事件

  Step 2（阶段一）：
    - 路由表初始用 ai_route_table（SQL Server，见第5节）
    - 软路由 TypeScript 服务查询此表决定沙箱分配
    - MVP 阶段不引入 etcd（降低工程复杂度）

  Step 3（阶段六生产加固）：
    - 将 ai_route_table 迁移到 etcd（3节点集群）
    - 启用 etcd Watch 机制实现实时路由变更通知
```

**全链路 MCP 上下文透传（保证多用户不串味的关键实现）：**

```typescript
// TenantSandbox 是所有 Skill 执行的隔离容器
class TenantSandbox {
  constructor(private tenantId: string, private pipelineId: string) {}

  async callMCP(toolName: string, args: object): Promise<any> {
    return mcpGateway.call(toolName, {
      ...args,
      _context: {          // 每个 MCP 调用都携带租户上下文
        tenantId: this.tenantId,
        pipelineId: this.pipelineId
      }
    });
  }

  async callLLM(prompt: string): Promise<string> {
    return llmGateway.complete({
      prompt,
      metadata: {          // LLM 调用也携带（用于计费隔离和审计）
        tenantId: this.tenantId,
        pipelineId: this.pipelineId
      }
    });
  }

  async appendIREvent(event: IREvent): Promise<void> {
    await irEventStore.append({
      ...event,
      tenantId: this.tenantId,   // 所有 IR 事件都携带 tenantId
      pipelineId: this.pipelineId
    });
  }
}
```

**"串味"的根本消除机制**：IR 投影引擎在每次重建时，强制加 `WHERE tenantId = ? AND pipelineId = ?` 过滤条件。每个 Skill 从 `TenantSandbox` 获取的 `irSnapshot` 物理上只包含当前项目的数据，不存在"可能看到别人数据"的代码路径。

---

### 8.6 目标六核查：架构层次识别 + Bug修复/二次开发中的迭代优化

**核查结论：⚠️ 原计划有概念，需补充"架构自愈的具体触发和执行机制"**

#### 8.6.1 架构层次和公共类的识别机制

开发 Skill 生成代码后，`arch-guard` 执行静态分析并提取架构元数据：

```typescript
// arch-guard 的架构层次提取逻辑
async function extractArchitectureMetadata(generatedDir: string): Promise<ArchMetadata> {
  const csFiles = glob.sync(`${generatedDir}/**/*.cs`);
  
  const layers: LayerInfo[] = [];
  const commonComponents: CommonComponent[] = [];
  
  for (const file of csFiles) {
    const ast = await parseCSharpAST(file);
    
    // 1. 从命名空间推断层次
    // JNPF.{Module}.Application.Services → Service 层
    // JNPF.{Module}.Domain.Entities → Domain/Entity 层
    // JNPF.{Module}.Infrastructure → Infrastructure 层
    const layer = inferLayerFromNamespace(ast.namespace);
    
    // 2. 从继承关系识别公共类使用
    const baseClasses = ast.classes.flatMap(c => c.baseClasses);
    const usedCommonComponents = baseClasses.filter(b => 
      JNPF_COMMON_CLASSES.includes(b) // 如 BaseService, SqlSugarRepository
    );
    
    layers.push({ className: ast.mainClass, layer, filePath: file });
    usedCommonComponents.forEach(comp => {
      commonComponents.push({ name: comp, usedBy: ast.mainClass });
    });
  }
  
  return { layers, commonComponents, dependencyRules: inferDependencyRules(layers) };
}
```

这份 `ArchMetadata` 写入 `ArchitectureMetadataRecorded` 事件，存入 `ai_ir_events` 表，是后续所有 Bug 修复和二次开发的查询基础。

#### 8.6.2 Bug修复中的分层依赖检测

```typescript
// bugfix-skill 在提出修复方案时，强制经过约束检测
async function validateBugfixAgainstArchitecture(
  fix: BugfixProposal,
  archMetadata: ArchMetadata
): Promise<ConstraintCheckResult> {
  
  // 检查1：修复方案涉及的类，层次是否正确
  const fixedClass = archMetadata.layers.find(l => l.className === fix.targetClass);
  const calledClasses = fix.newDependencies || [];
  
  for (const calledClass of calledClasses) {
    const calledLayer = archMetadata.layers.find(l => l.className === calledClass)?.layer;
    const isAllowed = archMetadata.dependencyRules.some(r => 
      r.from === fixedClass?.layer && r.to === calledLayer && r.allowed
    );
    if (!isAllowed) {
      return {
        passed: false,
        violation: `${fix.targetClass}(${fixedClass?.layer}) 不应依赖 ${calledClass}(${calledLayer})`
      };
    }
  }
  
  // 检查2：是否绕过了必须使用的公共类
  const mustUseComponents = MANDATORY_COMMON_COMPONENTS[fixedClass?.layer || ''] || [];
  for (const comp of mustUseComponents) {
    if (!fix.usedComponents.includes(comp)) {
      return {
        passed: false,
        violation: `${fix.targetClass} 必须继承/使用 ${comp}，不得直接实现`
      };
    }
  }
  
  return { passed: true };
}
```

#### 8.6.3 架构迭代优化（"自愈"机制的精确实现）

**触发规则**：当以下任一条件满足时，系统自动生成架构优化建议：

```typescript
// 架构异味检测规则（在 bugfix-skill 收到 BugFixed 事件后触发）
async function detectArchitectureSmells(projectId: string): Promise<ArchSmell[]> {
  const smells: ArchSmell[] = [];
  
  // 规则1：同一组件被修复 ≥ 3 次（职责边界不清晰）
  const fixCounts = await irEventStore.countByType('BugFixed', { groupBy: 'targetComponent' });
  for (const [component, count] of Object.entries(fixCounts)) {
    if (count >= 3) {
      smells.push({
        type: 'repeated-fix',
        component,
        fixCount: count,
        suggestion: `${component} 频繁出现缺陷，建议检查其职责是否过于复杂，考虑拆分`
      });
    }
  }
  
  // 规则2：某个 IR-1 EventSpec 修改次数 ≥ 2 次（需求理解有盲点）
  const specRevisions = await irEventStore.countByType('EventSpecRevised', { groupBy: 'fragmentId' });
  for (const [fragmentId, count] of Object.entries(specRevisions)) {
    if (count >= 2) {
      smells.push({
        type: 'spec-instability',
        fragmentId,
        revisionCount: count,
        suggestion: `业务事件 ${fragmentId} 的规格反复修改，建议在产品经理 Skill 阶段增加对该事件的深度追问`
      });
    }
  }
  
  // 规则3：约束违规 ≥ 2 次（架构约束声明本身可能有问题）
  const violations = await constraintViolations.groupByRule(projectId);
  for (const [rule, count] of Object.entries(violations)) {
    if (count >= 2) {
      smells.push({
        type: 'constraint-repeatedly-violated',
        rule,
        violationCount: count,
        suggestion: `规则 ${rule} 被多次违反，建议审查该规则是否合理或是否需要调整架构设计`
      });
    }
  }
  
  if (smells.length > 0) {
    await irEventStore.append({
      type: 'ArchitectureSmellDetected',
      projectId,
      payload: { smells, generatedAt: new Date().toISOString() }
    });
  }
  
  return smells;
}
```

**架构优化建议的展示与执行**：

```
ArchitectureSmellDetected 事件
    │
    ▼ 前端 SSE 推送给用户（架构健康度仪表盘）
    │   显示：发现 N 个架构异味，点击查看详情和建议
    │
    ▼ 用户确认接受某个建议
    │   例如：接受"拆分 WorkOrderService"建议
    │
    ▼ 追加 ArchRefactorApproved 事件
    │
    ▼ 激活 BugFix Skill 的"架构重构模式"
        输入：被拆分组件的 ArchMetadata + 建议拆分方案
        输出：拆分后的多个组件代码（增量 IR 事件）
        约束：拆分后的组件必须通过架构守卫重新验证
        结果：新的 ArchitectureMetadataRecorded 事件（更新架构元数据）
```

---

### 8.7 最终差距闭合：补充到各阶段的具体任务

基于上述核查，以下任务必须明确纳入实施计划（补充到第2节的各阶段）：

| 编号 | 补充任务 | 归属阶段 | 工期估算 | 解决目标 |
|---|---|---|---|---|
| P1 | `SAOrchestrator.ts` 改造：从独立运行改为被 `analyst-skill` 驱动，每步完成追加 `SA_Step_Completed` | 阶段二 | 2天 | 目标三 |
| P2 | 增量更新触发器：实现 `EventSpecRevised` 事件的受影响 SA 步骤精确判定表 | 阶段二 | 1天 | 目标三 |
| P3 | Prompt 模板库：为每个 Skill 建立标准化的四要素 Prompt 结构（含种子数据注入位、IR 上下文位、SA步骤位） | 阶段二-三 | 3天 | 目标四 |
| P4 | `bpm-to-jnpf-converter`：BPM 抽象图 → `FLOW_TEMPLATE_JSON` 原生 JSON 的确定性转换器 | 阶段三 | 2天 | 目标一/二 |
| P5 | `ir1-to-visualdev-mapper`：IR-1 字段 → `BASE_VISUAL_DEV.F_FORM_DATA` 的确定性映射器 | 阶段三 | 2天 | 目标一/二 |
| P6 | `arch-guard`：C# AST 解析 + 层次推断 + 公共类检测（扩展现有 `JNPF.Analyzers`） | 阶段四 | 3天 | 目标六 |
| P7 | 架构异味检测器：3条触发规则 + `ArchitectureSmellDetected` 事件 + 前端仪表盘展示 | 阶段五 | 2天 | 目标六 |
| P8 | `SandboxManager` 演进：`SemaphoreSlim` → `Channel<CreateRequest>` + 租户资源池（与 Phase B B3 合并） | 阶段一 | 与B3合并 | 目标五 |
| P9 | 手工平台回归测试门控：在 CI 中增加 VisualDev/工作流/API 冒烟测试，每次 AI 模块变更后自动执行 | 阶段一 | 1天 | 目标二 |
| P10 | 2小时基准测试：标准场景（HR+考勤+请假）全链路计时，验证关键路径优化效果 | 阶段五 | 1天 | 验收 |

---

### 8.8 最终目标达成矩阵

| 目标 | 原计划覆盖 | 本节补充 | 实施后达成度 | 关键风险 |
|---|---|---|---|---|
| **目标一**：10个2026水准Skills | ✅ 第3节完整设计 | P3 Prompt模板库 + P4/P5转换器 | ✅ 100% | ToT成本控制 |
| **目标二**：完整链条 | ✅ 第1-2节链路设计 | P9 手工平台回归测试门控 | ✅ 100% | SA Agent封装质量 |
| **目标三**：SA九步 + 版本管理 + 增量更新 | ✅ 事件类型定义 | P1 SAOrchestrator改造 + P2 增量更新触发器 | ✅ 100% | 受影响步骤判定精度 |
| **目标四**：四要素有机融合 | ✅ 概念层完整 | P3 Prompt模板库（具体结构） | ✅ 100% | LLM幻觉在规格阶段 |
| **目标五**：多用户多任务并行 | ✅ 两级路由 + etcd | P8 SandboxManager演进（MVP阶段用SQL） | ✅ 100% | 并发下的Token预算控制 |
| **目标六**：架构识别 + 迭代优化 | ✅ 概念层 | P6 arch-guard + P7 架构异味检测器 | ✅ 100% | AST解析对复杂C#的覆盖率 |

**最终结论：六大目标在当前计划 + 本节补充任务（P1-P10）的支撑下，均可达成。**

无需推倒重来，现有 JNPF-AI 代码资产（PipelineOrchestratorService、SandboxManager、Phase A施工包）是计划的有效基础，通过渐进式改造而非替换来实现所有目标。

---

*文档版本 v1.2 | 最后更新：2026-07-03（六目标核查 + 差距闭合 + 最终达成矩阵）*

---

## 9. 最终深度审核：从"能生成功能"到"生成完整系统"的最后闭环

> 本节是全计划的最终审核。审核视角：**假设 12 周后系统上线，用户输入一份需求，产出的东西到底是不是一个"完整的通用企业管理系统"？**
>
> 审核发现了一个此前所有版本（v1.0-v1.2）都未正面回答的问题：**"完整"从未被形式化定义**。第8节的六目标核查验证了"链条各环节都存在"，但没有验证"链条终点的产出物集合是否完备"。一条完整的流水线，如果每个 Skill 只产出自己视角的制品，最终拼出来的可能是"一堆能跑的表单"而非"一个企业管理系统"。本节补上这最后一块。

### 9.1 缺口诊断：现有计划的产出物盘点 vs 完整系统所需

**现有计划各 Skill 的产出物清单（v1.2 状态）：**

| Skill | 产出 |
|---|---|
| 数据库设计 | DDL（表+索引+约束） |
| UI 设计 | `BASE_VISUAL_DEV` 表单/列表 JSON + `FLOW_TEMPLATE_JSON` 工作流 |
| 开发 | C# Service + Entity（`.vm` 模板生成） |
| 测试 | 测试脚本 + 测试报告 |
| 部署 | VisualDev 注册 + 菜单创建 + 冒烟截图 |

**一个"完整的通用企业管理系统"实际需要的制品全集（以 JNPF 平台承载为准）：**

| # | 制品类别 | 现有计划覆盖状态 | 缺口严重度 |
|---|---|---|---|
| 1 | 数据库表 + 索引 + 约束 | ✅ 数据库设计 Skill | — |
| 2 | 表单/列表页面（VisualDev JSON） | ✅ UI 设计 Skill | — |
| 3 | 工作流定义（FlowTemplateJson） | ✅ P4 转换器 | — |
| 4 | C# Service（CRUD 部分） | ✅ 开发 Skill（.vm 模板） | — |
| 5 | **复杂业务逻辑代码**（PSpec/决策表→可执行逻辑） | ❌ **未设计**（模板只覆盖CRUD） | **致命** |
| 6 | **跨事件集成逻辑**（如报工→自动更新工单状态） | ❌ **未设计** | **致命** |
| 7 | **角色 + 权限初始化数据**（IR-0 roleMatrix → JNPF 权限记录） | ❌ 未设计 | 高 |
| 8 | **数据字典初始化**（SA Dict → `BASE_DICTIONARY_TYPE/DATA`） | ❌ 未设计 | 高 |
| 9 | 菜单 + 导航结构 | ⚠️ 部署 Skill 一笔带过，无结构化生成规则 | 中 |
| 10 | **演示数据**（每张表的种子记录，让系统开箱可看） | ❌ 未设计 | 中 |
| 11 | 首页/工作台（待办、统计卡片） | ❌ 未设计 | 中 |
| 12 | App 端配置（`appColumnData`） | ⚠️ 类型中有字段，无生成规则 | 低 |
| 13 | 消息通知配置（审批到达提醒） | ❌ 未设计 | 低 |
| 14 | 打印模板 / 导入导出配置 | ❌ 未设计 | 低（可后置） |

**审核结论：不补上第 5、6 项，生成的系统只是"CRUD 表单集合"，不是企业管理系统。第 7、8、10 项不补，系统部署后是"空壳"，用户打开即报错或一片空白。**

---

### 9.2 制品完整性契约（Completeness Contract）— 核心补充设计

**设计思想：把"完整"从主观判断变为机器可校验的契约。**

每个 IR-1 业务事件在进入部署阶段前，必须通过**制品完整性门控（Completeness Gate）**——一个确定性检查器，验证该事件的全部必需制品都已存在于 IR-3 中：

```typescript
// 制品完整性契约：每个业务事件必须产出的制品清单（按事件特征动态确定）
interface ArtifactCompletenessContract {
  eventId: string;
  requiredArtifacts: ArtifactRequirement[];
}

function deriveRequiredArtifacts(eventSpec: IR1_EventSpec): ArtifactRequirement[] {
  const required: ArtifactRequirement[] = [
    // 所有事件的基线制品
    { type: 'ddl-table',        source: 'db-design-skill' },
    { type: 'visualdev-form',   source: 'ui-design-skill' },
    { type: 'visualdev-list',   source: 'ui-design-skill' },
    { type: 'csharp-service',   source: 'developer-skill' },
    { type: 'menu-entry',       source: 'deploy-skill' },
    { type: 'role-permission',  source: 'deploy-skill' },     // 来自 roleMatrix
    { type: 'test-suite',       source: 'tester-skill' },
    { type: 'demo-data',        source: 'developer-skill' },  // ≥3条演示记录
  ];

  // 按事件特征追加条件制品
  if (eventSpec.hasWorkflow)        required.push({ type: 'flow-template-json', source: 'ui-design-skill' });
  if (eventSpec.hasDecisionTable)   required.push({ type: 'business-rule-code', source: 'developer-skill' });
  if (eventSpec.hasPSpecLogic)      required.push({ type: 'custom-service-method', source: 'developer-skill' });
  if (eventSpec.crossEventEffects.length > 0)
                                    required.push({ type: 'integration-handler', source: 'developer-skill' });
  if (eventSpec.usesDictionary)     required.push({ type: 'dictionary-seed', source: 'deploy-skill' });
  if (eventSpec.hasStateMachine)    required.push({ type: 'state-transition-code', source: 'developer-skill' });

  return required;
}

// 完整性门控：部署 Skill 激活前的强制检查
async function completenessGate(projectId: string): Promise<GateResult> {
  const allEvents = await irProjection.getFragments(projectId, 'IR1_EventSpec');
  const missing: MissingArtifact[] = [];

  for (const event of allEvents) {
    const contract = deriveRequiredArtifacts(event);
    for (const req of contract) {
      const artifact = await irProjection.findArtifact(projectId, event.eventId, req.type);
      if (!artifact || artifact.stability !== 'stable') {
        missing.push({ eventId: event.eventId, artifactType: req.type, responsibleSkill: req.source });
      }
    }
  }

  if (missing.length > 0) {
    // 追加 CompletenessGateFailed 事件，自动重新激活责任 Skill 补齐缺失制品
    await irEventStore.append({ type: 'CompletenessGateFailed', payload: { missing } });
    return { passed: false, missing };
  }
  await irEventStore.append({ type: 'CompletenessGatePassed', payload: { eventCount: allEvents.length } });
  return { passed: true };
}
```

**门控位置**：插入在"测试 Skill 通过"与"部署 Skill 激活"之间。`test-passed` 事件不再直接触发部署，改为触发完整性门控，门控通过才发布 `ready-to-deploy` 事件。

---

### 9.3 致命缺口一的解决：复杂业务逻辑代码生成（PSpec/决策表 → C#）

**问题本质**：`.vm` 模板只能生成 CRUD 骨架。SA Step 4（PSpec）产出的加工逻辑（如"报工时校验数量≤计划量，超出则拒绝"）和 Step 5（DecisionTable）产出的决策表（如"质量等级→流程分支"），必须变成**可执行的 C# 代码**，否则规格只是文档。

**解决方案：三通道代码生成策略（按逻辑复杂度分流）**

```
IR-1 中的业务逻辑
    │
    ├── 通道A：简单校验规则（约60%的规则）
    │   如"必填""数值范围""正则格式""外键存在性"
    │   → 确定性映射为 FluentValidation 规则代码（零LLM）
    │   → 生成 {Entity}Validator.cs，模板固化，参数填充
    │
    ├── 通道B：决策表逻辑（约25%的规则）
    │   SA Step 5 的决策表本身就是结构化数据（条件→动作矩阵）
    │   → 确定性转换为 C# switch/规则链代码（零LLM）
    │   → 生成 {Event}DecisionHandler.cs
    │   → 决策表数据同时存入 ai_decision_tables 表，运行时可查可改
    │
    └── 通道C：复杂过程逻辑（约15%的规则）
        PSpec 中的多步骤过程描述（如"提交报工→锁定工单→计算成本→若超预算则挂起"）
        → LLM 生成方法体（唯一需要 LLM 的通道）
        → 生成位置：{Entity}Service.cs 中的 partial 扩展方法
        → 强制约束：
          a) LLM 只生成方法体，方法签名由 PSpec 的输入输出规格确定性生成
          b) 生成代码必须通过 arch-guard 检查（分层/公共类）
          c) 测试 Skill 从 PSpec 的前置/后置条件自动推导该方法的单元测试
          d) 单元测试不通过 → 最多重试3次 → 仍失败则标记 HumanReviewRequired
```

**通道C 的 LLM Prompt 结构（防幻觉设计）：**

```
① 方法签名（确定性生成，LLM 不可修改）:
   public async Task<WorkReportResult> SubmitWorkReport(WorkReportInput input)
② PSpec 形式化规格（LLM 的唯一逻辑依据）:
   前置条件: input.Quantity <= workOrder.PlannedQuantity - workOrder.ReportedQuantity
   处理步骤: 1.锁定工单行 2.累加已报数量 3.按工序单价计算成本 4.成本超预算→状态挂起
   后置条件: workOrder.ReportedQuantity == old + input.Quantity
   异常路径: 超量→Oops.Bah("超出计划量"); 工单已关闭→Oops.Bah("工单已关闭")
③ 可用的公共类清单（白名单，禁止使用清单外的类）:
   _repository(SqlSugarRepository), Oops.Bah/Oh, UserManager, ...
④ 生成约束: 禁止 raw SQL 拼接；禁止 try-catch 吞异常；必须用 Oops 抛业务异常
```

**partial class 机制保证模板与手写代码互不覆盖（R3 红线兼容）：**

```
{Entity}Service.cs           ← .vm 模板生成（CRUD），重新生成时覆盖
{Entity}Service.Custom.cs    ← 通道B/C 生成（业务逻辑），partial class，
                               独立文件，模板重生成不触碰
```

这样二次开发时，用户改需求重新生成 CRUD 不会丢失业务逻辑代码；业务逻辑变更只重新生成 `.Custom.cs`。

---

### 9.4 致命缺口二的解决：跨事件集成逻辑生成

**问题本质**：企业管理系统的灵魂在于事件联动（报工→工单状态更新→库存扣减→成本归集）。单事件视角的 Skill 各自生成制品，没人负责"事件之间的因果链"。

**解决方案：集成点作为一等 IR 公民**

1. **来源**：IR-0 的 `businessEvents[].dependsOn` 已声明事件依赖；SA Step 1（DFD）的数据流已声明数据在事件间的流动。总体设计 Skill（Skill 4）新增职责：**将 DFD 数据流中的跨事件流动，形式化为集成点声明（IntegrationPoint）**，写入 IR-2：

```typescript
interface IR2_IntegrationPoint {
  integrationId: string;              // INT-001
  sourceEvent: string;                // EVT-005 报工提交
  targetEntities: string[];           // WORK_ORDER, INVENTORY
  trigger: 'after-commit';            // 触发时机（事务后）
  effects: Array<{
    entity: string;
    operation: 'update' | 'insert';
    logic: string;                    // 形式化描述："WORK_ORDER.ReportedQty += input.Qty"
    consistency: 'same-transaction' | 'eventual';  // 同事务 or 最终一致
  }>;
}
```

2. **代码生成**：开发 Skill 对每个 IntegrationPoint 生成集成处理器：
   - `same-transaction` 效果 → 生成在 Service 方法体内（同一 SqlSugar 事务）
   - `eventual` 效果 → 生成 JNPF EventBus（Channel）事件处理器 `{Event}IntegrationHandler.cs`

3. **测试**：测试 Skill 对每个 IntegrationPoint 自动生成集成测试：执行源事件 → 断言目标实体状态变化符合 `effects` 声明。

4. **完整性门控检查项**：`crossEventEffects.length > 0` 的事件必须有对应 `integration-handler` 制品（见9.2）。

---

### 9.5 高优缺口的解决：权限/字典/菜单/演示数据的自动初始化

这四项让系统从"部署成功"变为"开箱可用"，全部为确定性生成（零 LLM）：

**① 角色与权限初始化（roleMatrix → JNPF 权限体系）**

```
IR-0 roleMatrix                     JNPF 目标表
─────────────────────────────────────────────────────
roleName: "车间主管"          →     BASE_ROLE 插入角色记录
responsibilities: [...]       →     BASE_AUTHORIZE 授权记录：
involvedRoles ∈ businessEvent →       该角色 × 该事件对应菜单/按钮/列表/表单权限
permissionBoundary: "本部门"   →     数据权限方案（BASE_AUTHORIZE 的数据范围配置）
```

部署 Skill 生成一份**权限初始化 SQL 脚本**（幂等，可重复执行），作为 IR-3 制品存档。

**② 数据字典初始化（SA Dict → JNPF 字典表）**

SA Step 3 产出的枚举类字典项（如请假类型、审批状态）：

```
dict.leaveType: ["年假","事假","病假"]  →  BASE_DICTIONARY_TYPE (类型: AI_GEN_{project}_leaveType)
                                        →  BASE_DICTIONARY_DATA (3条数据项)
UI 设计 Skill 的 el-select 组件         →  dictionaryType 引用该字典编码（闭环）
```

**③ 菜单结构生成规则（不再是部署 Skill 的模糊步骤）**

```
菜单树推导规则（确定性）：
  一级菜单 ← IR-0 的业务子域（如"生产管理""质量管理"，由 PM Skill 的事件分组产生）
  二级菜单 ← 每个 VisualDev 功能（列表页入口）
  按钮权限 ← 表单操作（新增/编辑/删除/审批/导出），从 EventSpec 的角色-操作矩阵推导
```

**④ 演示数据生成**

开发 Skill 对每张表生成 3-5 条演示记录（INSERT 脚本）：
- 字段值来源：种子库的行业示例值（如工单号 "WO-2026-0001"）+ 字典项随机取值 + 外键引用已生成的上游演示记录（保证引用完整性，按 ER 图拓扑排序生成）
- 用途：部署后冒烟测试直接可见带数据的列表页（E1 截图证据有实际内容）；用户验收时看到的不是空系统

---

### 9.6 "完整系统"的最终验收定义（Definition of Done）

**12周后的终极验收场景（替代此前分散的验收标准）：**

> 输入：一份 1200 字的"生产车间管理系统"需求（含工单、报工、质检、设备点检 4 个子域，14 个业务事件，2 个审批流）。
>
> 产出必须满足以下全部条款，缺一项即判定"未达成完整系统目标"：

| # | 验收条款 | 验证方式 |
|---|---|---|
| D1 | 14 个业务事件全部通过完整性门控（9.2 契约） | 查询 `CompletenessGatePassed` 事件 |
| D2 | 部署后用演示账号登录，菜单树完整呈现 4 个子域 | Playwright 截图 |
| D3 | 每个列表页打开即有演示数据（非空白） | Playwright 遍历截图 |
| D4 | 新增一条报工记录，工单的已报数量自动更新（跨事件集成生效） | E2E 断言 |
| D5 | 提交超出计划量的报工，系统拒绝并提示（PSpec 逻辑生效） | E2E 断言 |
| D6 | 用"车间主管"角色登录，只能看到权限内的菜单和本部门数据 | E2E 双账号对比 |
| D7 | 发起质检审批流，按 FlowTemplateJson 定义流转到正确审批人 | E2E 断言 |
| D8 | 用 JNPF 表单设计器打开任一 AI 生成的表单，修改后保存成功 | 手工验证 + 截图 |
| D9 | 用 JNPF 工作流设计器打开审批流，增加一个节点后保存生效 | 手工验证 + 截图 |
| D10 | 全程耗时 ≤ 2 小时（从需求提交到 D2 可登录） | 事件流时间戳统计 |
| D11 | 手工平台全部回归测试通过（生成过程零影响） | CI 报告 |
| D12 | 注入一个字段级 Bug 并修复，只重算受影响制品（增量验证） | 事件流审计 |

---

### 9.7 补充任务 P11-P16（并入实施路线图）

| 编号 | 任务 | 归属阶段 | 工期 | 解决缺口 |
|---|---|---|---|---|
| P11 | **完整性门控引擎**：制品契约推导 + 门控检查器 + `CompletenessGateFailed` 自动补齐重激活 | 阶段四 | 2天 | 9.2（核心） |
| P12 | **三通道业务逻辑代码生成**：通道A FluentValidation 映射器 + 通道B 决策表转换器 + 通道C LLM方法体生成（含 partial class 机制） | 阶段四 | 4天 | 9.3（致命缺口一） |
| P13 | **集成点形式化 + 集成处理器生成**：总体设计 Skill 新增 IntegrationPoint 提取 + 开发 Skill 生成事务内/EventBus 处理器 + 集成测试推导 | 阶段三-四 | 3天 | 9.4（致命缺口二） |
| P14 | **权限/字典/菜单初始化生成器**：roleMatrix→BASE_ROLE/AUTHORIZE 脚本 + Dict→BASE_DICTIONARY + 菜单树推导规则 | 阶段五 | 2天 | 9.5 |
| P15 | **演示数据生成器**：按 ER 拓扑排序生成引用完整的 INSERT 脚本（种子库示例值 + 字典随机） | 阶段五 | 1天 | 9.5 |
| P16 | **DoD 终极验收套件**：D1-D12 的自动化验证脚本（Playwright + 事件流断言 + CI 集成） | 阶段五-六 | 2天 | 9.6 |

**工期影响评估**：P11-P16 合计新增约 14 人日。阶段四、五原有排期各预留了缓冲，P12/P13 为阶段四的关键路径（建议阶段四扩展为 2.5 周），其余可并行消化。总体 12 周框架不变，阶段四/五边界微调。

---

### 9.8 最终审核结论

**审核前状态（v1.2）**：六目标的"链条环节"全部就绪，但"链条终点产出完整系统"缺乏形式化保障——能生成的是"带流程的 CRUD 表单集合"，与"完整的通用企业管理系统"之间存在 5 项制品缺口，其中 2 项致命（复杂业务逻辑代码、跨事件集成逻辑）。

**审核后状态（v1.3，本节补充后）**：

| 维度 | 保障机制 |
|---|---|
| 完整性可定义 | 制品完整性契约（按事件特征动态推导必需制品清单） |
| 完整性可校验 | 完整性门控（部署前强制机器检查，缺失自动重激活责任 Skill） |
| 业务逻辑可执行 | 三通道代码生成（60%规则零LLM确定性映射 + 25%决策表转换 + 15%LLM受控生成） |
| 事件联动可实现 | IntegrationPoint 一等 IR 公民 + 事务内/EventBus 双模式处理器生成 |
| 开箱可用 | 权限/字典/菜单/演示数据四项自动初始化 |
| 完整性可验收 | DoD 十二条款终极验收（D1-D12，全部自动化或截图留证） |

**最终结论：在 P1-P16 全部纳入实施的前提下，本计划可以支撑"输入自然语言需求，2小时内生成一个可登录、有数据、有权限、有流程、业务逻辑可执行、且可被 JNPF 手工平台工具全量定制的完整通用企业管理系统"这一终极目标。**

计划至此收敛，不再有已知的结构性缺口。剩余风险均为执行层面（LLM 生成质量、种子库建设进度、团队工程能力），已在第6节和8.8节风险矩阵中覆盖。

---

*文档版本 v1.3 | 最后更新：2026-07-03（最终深度审核：制品完整性契约 + 三通道业务逻辑生成 + 集成点机制 + DoD 十二条款）*
*下一步：将 P1-P16 并入第2节各阶段任务清单，拆解为 Engineering Tickets，启动 W1 实施*
