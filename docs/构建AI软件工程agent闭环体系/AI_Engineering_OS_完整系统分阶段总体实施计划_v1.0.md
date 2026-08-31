# AI Engineering OS 完整系统分阶段总体实施计划 v1.0

## 唯一施工纲领 / Single Source of Truth

> **文档状态：AUTHORITATIVE / EXECUTION MASTER PLAN**
>
> 本文件用于把《AI_Engineering_OS_最终权威执行版_v2.1_FDE.md》转化为可连续执行的总体施工路线。
>
> 本文件不替代上位权威规格；它负责定义：阶段顺序、阶段目标、依赖关系、交付物、验证方式、Gate、上下文防漂移机制、当前站位和后续施工边界。
>
> 后续任何 Design Specification、Implementation Plan、Task Bundle、Test Plan、Gate Report、Completion Report 均必须能够回溯到本文件的 Phase / Workstream / Gate。

---

# 0. 文档使命

本计划解决两个核心问题：

1. 防止 AI 工程师经过多轮任务后遗忘整体目标、错误缩小范围、跳阶段、提前做后续 Intelligence/FDE、把局部完成误报为系统完成。
2. 建立项目唯一施工真相源，使所有后续设计规格和实施计划都只能是本计划的下位展开，而不是另起炉灶。

上位 v2.1 的核心要求是：系统必须把 Governance、AgentOS、Product/System Design Intelligence、UI/UX/Frontend、Engineering Intelligence、Verification、Multi-Agent/FDE Team、Evidence 和 Real Engineering 统一到同一执行闭环中；其最终完成必须由状态、证据和 Gate 共同决定，而不能由 Agent 自我宣布完成。fileciteturn4file0L19-L42

---

# 1. 权威体系

权威层级固定为：

```text
Human / Chief Architect
        ↓
AI Engineering OS Authoritative Specification v2.1
        ↓
AI Engineering OS Master Implementation Plan v1.0（本文件）
        ↓
Active Phase Contract
        ↓
Approved Design Specification
        ↓
Approved Implementation Plan
        ↓
Task Bundle
        ↓
Code / Tests / Evidence
        ↓
Gate Evaluation
```

任何下级文档不得静默覆盖上级约束。

## 1.1 本文件的权限

本计划可以：

- 把上位 Phase 拆成可施工的子阶段；
- 指定当前工作顺序和依赖；
- 指定阶段级制品、验证和 Gate；
- 对历史编号进行映射，避免工程执行混淆。

本计划不得：

- 删除 v2.1 的能力；
- 把暂未验证的实现策略升级成架构铁律；
- 将真实验证替换成 Mock / Narrative / Planned PASS；
- 绕过上位 Gate 提前进入下一能力域。

---

# 2. v2.1 阶段编号统一规则

v2.1 当前同时存在两套历史编号：正文的 0–7 主阶段，以及 FDE Amendment 的 0–9 产品能力路线。为了避免工程师在多轮施工后发生编号漂移，本实施计划采用以下统一映射：

```text
ARCHITECTURE / PRODUCT PHASE（权威）

P0  Baseline & Governance Foundation
P1  Control Plane Governance
P2  AgentOS Governance & Runtime
P3  Engineering Cognition
P4  Intelligent Verification
P5  Multi-Agent Team Runtime
P6  Enterprise Product / System Design Intelligence
P7  UI / UX / Frontend Engineering Intelligence
P8  FDE Real Engineering Vertical Slice
P9  Production Hardening / Benchmark
```

说明：

- 原 v2.0 的“Phase 6 Expert Integration & Real Engineering”在 v2.1 FDE Amendment 中被重新展开为 P8 FDE Vertical Slice；其真实 JNPF/类级重构 Pilot 能力不丢失，而是成为 P8 的一部分。
- 当前工程报告中的“Phase 1 Runtime Kernel”属于 **P2 AgentOS Runtime 的内部施工子阶段 Runtime Phase A**，不应被解释为重新开始 P1。当前治理前置和 P1 Policy Vertical Slice 已有实测完成项。
- 从此以后，报告必须同时写“架构 Phase + Runtime Subphase”，例如：`P2 / Runtime-A`。

---

# 3. 当前真实站位

截至当前事实报告：

```text
P0 Harness / Pre-AgentOS Governance              ✅ PASS
P1 Policy Vertical Slice                         ✅ PASS（已有黑盒验证）
P2 AgentOS Runtime Architecture                  ✅ FROZEN
P2 / Runtime-A Kernel Implementation             🟡 NEXT
P2 / Runtime-B~H                                 🟡 DESIGNED
P3 Cognition                                    ❌ NOT STARTED
P4 Verification                                  ❌ NOT STARTED
P5 Team Runtime                                  ❌ NOT STARTED
P6 System Design Intelligence                    ❌ NOT STARTED
P7 UI / UX / Frontend Intelligence               ❌ NOT STARTED
P8 FDE Vertical Slice                             ❌ NOT STARTED
P9 Hardening / Benchmark                         ❌ NOT STARTED
```

已经完成的重点事实：

- Harness Inventory / Authority / Boundary / Quarantine / Registry / Memory Contract / Resolver / Adversarial Verification 已形成治理前置闭环。
- PRE-AGENTOS-GATE 已 PASS。
- 类级重构专家 Skill v6.0 已可用。
- Universal Agent v2.1 设计冻结，但实现代码为 0。
- AgentOS Runtime 总体架构和 Phase A-H 设计完成，但 `backend/modules/mod-runtime/` 尚无正式实现代码。
- v5 类级重构实施资产已准备，但最终真实 `dotnet test` 未取得完整执行结果；因此不得把该轮描述为最终闭环 PASS。

**当前唯一主施工线：P2 / Runtime-A。**

---

# 4. 全系统最终能力地图

AI Engineering OS 最终必须同时具备六个价值维度：

```text
1. Weak-Agent Cognitive Amplification
2. Enterprise Product / System Analysis & Design
3. UI / UX & Frontend Design-to-Implementation
4. Engineering Execution & Verification
5. Multi-Agent / FDE Team Delivery
6. External Skill / MCP / Provider Compatibility
```

并由统一基础设施承载：

```text
Governance
AgentOS Runtime
Policy
Gate
Hook
State
Capability
Memory
Evidence
Recovery
Supply Chain
Observability
```

两种入口统一：

```text
Greenfield
Enterprise Intent
 → Business / Product
 → Domain / Process
 → Functional
 → Architecture / Data
 → UX / UI
 → Frontend + Backend
 → Engineering Verification

Brownfield
Existing System
 → Discovery
 → Contract
 → Architecture / Business / Risk
 → Table / Class Engineering
 → Build / Test / Regression
 → Review / Evidence
```

两者最终进入同一 Completion Contract。

---

# 5. 总体阶段门路线

```text
P0 Governance Foundation
       │ G0
       ▼
P1 Executable Control Plane
       │ G1
       ▼
P2 AgentOS Runtime
       │ G2
       ▼
P3 Engineering Cognition
       │ G3
       ▼
P4 Intelligent Verification
       │ G4
       ▼
P5 Multi-Agent Team Runtime
       │ G5
       ▼
P6 Enterprise Product / System Design
       │ G6
       ▼
P7 UI / UX / Frontend
       │ G7
       ▼
P8 FDE Vertical Slice
       │ G8
       ▼
P9 Production Hardening / Benchmark
       │ G9
       ▼
AI ENGINEERING OS PRODUCTION-READY
```

严格规则：

> **Gate 未通过，不得把下一 Phase 宣布为完成；允许准备下一 Phase 的设计材料，但禁止以实现下一 Phase 的方式规避当前 Gate。**

---

# 6. 每个 Phase 的固定施工生命周期

所有 Phase 必须统一使用：

```text
PHASE START
    ↓
Baseline Verification
    ↓
Read Phase Context
    ↓
Read Active Phase Contract
    ↓
Read Previous Gate
    ↓
Task Bundle
    ↓
Implementation / Execution
    ↓
Real Build
    ↓
Real Test / Validation
    ↓
Self Evaluation
    ↓
Self Repair
    ↓
Independent Review
    ↓
Evidence Collection
    ↓
Gate Evaluation
    ↓
Phase Closure
```

这是上位 v2.1 的执行纪律落地；它明确要求每个 Phase 持续回读 Current Phase、Current Gate 和 Outstanding Items，以防上下文漂移。fileciteturn5file1L1051-L1077

---

# 7. P0 — Baseline & Governance Foundation

## 7.1 目标

建立整个 AI Engineering OS 的统一基线、权威边界和可执行治理入口。

## 7.2 当前状态

**已完成。**

包括：

```text
Harness Inventory
Authority Map
Boundary Map
Quarantine
Capability Registry
Memory Contract
Resolver
Adversarial Tests
PRE-AGENTOS-GATE
```

## 7.3 必须保持的能力

不能因为进入 P2 Runtime 而回退或覆盖既有 Harness Governance。

## 7.4 Exit Gate G0

必须保持：

```text
0 Architecture Boundary Violation
0 Governance Authority Duplication
0 Unknown Critical Component
PRE-AGENTOS-GATE = ACCEPTED
```

**注意：G0 通过只表示前置治理成立，不表示 AgentOS 已建成。**

---

# 8. P1 — Executable Control Plane

## 8.1 目标

把规则转化为 Machine Policy、Gate、Hook、Completion Enforcement。

## 8.2 核心工作流

```text
Rule
 ↓
Policy
 ↓
Evaluation
 ↓
Hook / Gate
 ↓
Evidence
```

## 8.3 核心能力

```text
Policy
PolicyRule
PolicyScope
PolicySeverity
PolicyDecision
PolicyEvidenceRequirement

Gate
GateCondition
GateResult
GateFailure
GateEvidence

PreAction Hook
PostAction Hook
PreMutation Hook
PreBuild Hook
PreTest Hook
PreComplete Hook
```

## 8.4 强制治理场景

至少阻止：

```text
Skip Build
Fake Pass
Skip Test
Delete Test
Weaken Assertion
Complete Without Evidence
Direct Tool Bypass
```

## 8.5 当前完成情况

项目报告显示已有 `P001-P005 Policy Vertical Slice` 和黑盒 54 PASS，因此 P1 已有实测闭环资产。

## 8.6 Exit Gate G1

必须证明：

```text
高风险工程规则都有机器 Enforcement
违规 Agent 无法进入 Completed
Policy Decision / Gate Decision 可追踪
```

---

# 9. P2 — AgentOS Governance & Runtime

> **当前主施工阶段。**

## 9.1 目标

真正建立 AgentOS Runtime，使 Expert 不再只是 Skill/Prompt，而成为受状态、上下文、能力、策略、证据和生命周期约束的真实 Agent。

Expert 必须具备：

```text
Identity
State
Lifecycle
Task
Skills
Context
Capability
Evidence
```

这一点直接承接 v2.1 对 Expert-as-Real-Agent 的要求。fileciteturn4file0L118-L133

## 9.2 P2 必须完成的能力域

### P2-A Runtime Kernel

优先级最高：

```text
Agent Identity
Task Identity
Lifecycle Kernel
Execution Kernel
State Authority
Core Runtime Loop
```

当前对应你们现有 Runtime Phase A。

### P2-B State & Context

```text
Task State
Execution Stage
Operation State
Context
Agent State
```

采用三层状态模型：

```text
Task State
Execution Stage
Operation State
```

避免把所有生命周期混成一个超级状态枚举。v2.1 已明确要求状态正式成为运行时契约。fileciteturn2file1L1162-L1254

### P2-C Capability Boundary

建立：

```text
Capability Registry
Authorization
Capability Policy
Capability Provider
```

至少覆盖：

```text
Search
File
Build
Test
Git
Memory
```

### P2-D External Provider / MCP Adapter

所有外部能力遵守：

```text
Agent
 ↓
Runtime
 ↓
Authorization
 ↓
Policy
 ↓
Adapter
 ↓
External Provider
 ↓
Evidence
```

外部工具可以提供 Capability，但不能获得 Governance Authority。fileciteturn4file0L260-L338

### P2-E Memory Boundary

统一：

```text
Memory Contract
 ↓
Memory Provider
```

区分：

```text
Working Memory
Task Memory
Agent Memory
Project Knowledge
Long-Term Memory
```

### P2-F Evidence Runtime

把工程动作绑定到：

```text
Task
Agent
Team
Phase
Action
Capability
Input
Output
Before
After
Tool
Timestamp
Policy
Result
Evidence
```

### P2-G Resilience & Recovery

必须考虑：

```text
Agent Crash
Runtime Restart
Machine Restart
Evidence Write Failure
Tool Timeout
Partial Mutation
```

恢复策略不是默认 Git Reset，而必须绑定 Work Unit 的：

```text
Before Snapshot
Mutation Boundary
After Snapshot
Recovery Strategy
```

可选：

```text
Rollback
Compensating Change
Forward Repair
Manual Resolution
```

这一恢复模型来自 v2.1 的 Resilience/Recovery 补强。fileciteturn2file1L1308-L1396

### P2-H Governance Integration / Completion

最终打通：

```text
State
+
Policy
+
Capability
+
Evidence
+
Gate
+
Completion
```

## 9.3 P2 当前施工顺序

严格：

```text
Runtime-A Kernel
 ↓ Gate-A
Runtime-B State/Context
 ↓ Gate-B
Runtime-C Execution
 ↓ Gate-C
Runtime-D Evidence
 ↓ Gate-D
Runtime-E Persistence
 ↓ Gate-E
Runtime-F Governance
 ↓ Gate-F
Runtime-G Extension
 ↓ Gate-G
Runtime-H Integration
 ↓ Gate-H
```

不得在 A-H 未闭环时开始 P3 Cognition。

## 9.4 P2 Exit Gate G2

必须证明：

```text
Expert cannot bypass Runtime
Runtime has no domain-specific Expert dependency
MCP cannot bypass Policy
Memory Provider cannot change Governance
Unauthorized capability is blocked
State transitions are atomic/linearizable
Restart does not corrupt authoritative state
Evidence is trustworthy and traceable
Completion cannot be self-declared
```

并完成真实 `dotnet build` / `dotnet test` 验证。

---

# 10. P3 — Engineering Cognition Engine

## 10.1 目标

解决第一个核心产品痛点：**弱 Agent 不会完整思考，强 Agent 又不应被压制。**

系统通过 Risk-Weighted Cognition 强制产生与任务风险匹配的认知，而不是用僵硬数字决定分析深度。

## 10.2 核心能力

```text
Task Classifier
Risk Classifier
Cognitive Requirement Resolver
Discovery Engine
Dependency Graph
Boundary Analysis
Contract Model
Architecture Assessment
Behavior Analysis
Risk Analysis
Performance Cognition
Implementation Plan
```

P3 必须继承现有类级重构专家 Skill 中已经证明有效的高维工程分析能力，而不是另造一套完全不同的认知体系。

## 10.3 Cognitive Pipeline

```text
Observe
 ↓
Expand Context
 ↓
Discover Dependencies
 ↓
Understand Structure
 ↓
Identify Boundary
 ↓
Extract Contract
 ↓
Analyze Architecture
 ↓
Analyze Behavior
 ↓
Analyze Risk
 ↓
Plan
 ↓
Implement
```

## 10.4 Risk-Weighted Cognition

至少考虑：

```text
Change Type
Criticality
Call Graph
Cross-Cutting Concerns
Architectural Layer
Behavioral Risk
```

不得使用：

```text
>5 callers
90% coverage
```

作为统一硬规则。v2.1 明确反对这种机械阈值。fileciteturn2file1L1596-L1700

## 10.5 Exit Gate G3

真实随机任务验证：

```text
Bug Fix
Feature
Refactor
Runtime Change
```

证明：

```text
Required Cognition 未完成
        ↓
Implementation 被阻止
```

同时证明强 Agent 可以主动扩展分析但不会突破 Scope / Policy / Ownership / Evidence / Gate。

---

# 11. P4 — Intelligent Verification Engine

## 11.1 目标

把“运行测试”升级成“构建足以证明真实完成的验证策略”。

## 11.2 五层验证模型

```text
Structural
Behavioral
Workflow
Integration
Regression
```

## 11.3 核心能力

```text
Verification Planner
Test Selector
Critical Path Analyzer
Behavior Observer
Mock Boundary Policy
Fake Green Detector
Verification Evidence Model
```

## 11.4 Minimum Sufficient Verification

不是：

```text
尽可能多的测试
```

也不是：

```text
最低数量测试
```

而是：

> **与变更、依赖、风险和关键流程相关，并足以证明核心能力真实成立的最小充分集合。**

必须坚持 Relevant Verification Coverage，而不是简单限制测试数量。v2.1 对此已有明确修正。fileciteturn2file1L1704-L1799

## 11.5 Realism Policy

支持：

```text
REAL_REQUIRED
REAL_PREFERRED
MOCK_ALLOWED
MOCK_REQUIRED
```

关键业务链至少必须有真实执行：

```text
API
 → Application
 → Domain
 → Persistence
 → Event
 → Handler
 → Side Effect
```

## 11.6 Fake Green 防御

必须检测：

```text
Assertion Weakened
Test Deleted
Test Skipped
Mock Replacing Real
Hard-coded Result
Fake API
Fake Event
Empty Implementation
```

## 11.7 Exit Gate G4

必须完成真实：

```text
Business API
Database Operation
Event Flow
Workflow
Regression
Intentional Failure
```

并证明：

```text
Failure
 ↓
Detection
 ↓
Diagnosis
```

---

# 12. P5 — Multi-Agent Team Runtime

## 12.1 目标

把单 Expert 升级成真正的受治理工程团队，而不是群聊。

## 12.2 核心结构

```text
Lead Agent
 ├─ Discovery Specialist
 ├─ Architecture Specialist
 ├─ Implementation Specialist
 ├─ Verification Specialist
 └─ Reviewer Specialist
```

具体团队人数动态决定，不把某一固定数量写成架构铁律。

## 12.3 核心能力

```text
Agent Registry
Team Registry
Delegation Engine
Scheduler
DAG Executor
Context Manager
Artifact Ownership
Conflict Resolver
Team Evidence
```

## 12.4 Communication

默认 Specialist-to-Specialist 受限，但由 Policy/Capability 控制必要的直接通信；不能永久锁死唯一拓扑。v2.1 明确采用 Governed Communication Graph。fileciteturn2file1L1803-L1879

## 12.5 Ownership

所有 Mutation 必须绑定：

```text
Task
Agent
Workspace / Mutation Boundary
Artifact
```

必须回答：

```text
Who owns?
Who may modify?
Who may approve?
Who may revert?
Who may review?
```

## 12.6 Exit Gate G5

必须真实证明：

```text
Lead
+
Multiple Specialists
```

能够：

```text
Parallel Analysis
→ Aggregation
→ Decision
→ Implementation
→ Verification
```

同时：

```text
No File Conflict
No Permission Violation
No Gate Bypass
No Evidence Loss
```

---

# 13. P6 — Enterprise Product / System Design Intelligence

> **这是 v2.1 FDE Amendment 新增的核心一级能力。**

## 13.1 目标

解决原 AI Engineering OS 最大遗漏：AI 从企业业务意图到完整系统设计的能力。

## 13.2 核心设计链

```text
Enterprise Intent
 ↓
Business Context
 ↓
Product Definition
 ↓
Domain Model
 ↓
Business Process
 ↓
Functional Architecture
 ↓
Data / Table Model
 ↓
Technical Architecture
```

## 13.3 核心 Agents / Capabilities

```text
Business / System Analysis Agent
Product Manager Agent
Domain / Process Design Agent
Architecture Design Agent
Data / Table Design Agent
```

## 13.4 强制设计产物

至少包括：

```text
Business Context
Role Model
Problem Statement
User Stories / Requirements
Business Rules
Domain Model
Bounded Contexts
Process Model
State Model
Functional Module Tree
Capability Matrix
Data Model
Table Design
Architecture Model
NFR / Security / Integration Concerns
```

## 13.5 Functional Decomposition

必须形成：

```text
L1 Business Domain
   ↓
L2 Functional Module
   ↓
L3 Capability
```

不能把“菜单 = 模块 = CRUD”作为默认设计。

每个模块必须回答：

```text
为什么存在？
服务谁？
解决什么问题？
核心流程是什么？
状态是什么？
异常是什么？
```

## 13.6 Design-to-Engineering Traceability

任何实现必须能够反查：

```text
Business Intent
 ↓
Product Requirement
 ↓
Capability
 ↓
Design Artifact
 ↓
Data / API / UI
 ↓
Code
 ↓
Verification Evidence
```

## 13.7 Exit Gate G6

用真实复杂企业业务场景证明：

```text
业务理解正确
模块划分可解释
流程完整
领域边界清晰
数据模型可追踪
架构设计可实施
设计产物结构化
设计可以进入工程实现
```

---

# 14. P7 — UI / UX / Frontend Engineering Intelligence

## 14.1 目标

解决第二个核心上游痛点：AI 不应该只是“生成 CRUD 页面”，而必须从用户任务出发完成 UI/UX 设计并真实实现。

## 14.2 核心链

```text
User Role
 ↓
User Goal
 ↓
Business Task
 ↓
Information Need
 ↓
Interaction Model
 ↓
Page Specification
 ↓
UI / UX Design
 ↓
Frontend Implementation
 ↓
Visual / Functional Verification
```

## 14.3 核心 Agents / Capabilities

```text
UX / Interaction Agent
UI Design Agent
Design System Agent
Frontend Engineering Agent
UI Verification Agent
```

## 14.4 强制页面模型

所有关键页面至少形成：

```text
Page Purpose
User Role
User Goal
Information Architecture
Layout
Components
Fields
Actions
States
Validation
Loading
Error
Empty State
Permission
Responsive Behavior
```

## 14.5 Design-to-Code Traceability

必须可回答：

```text
为什么这个页面存在？
这个组件服务哪个用户任务？
这个字段来源什么数据？
这个 Action 对应什么业务能力？
这个 UI 状态对应什么业务状态？
```

## 14.6 Exit Gate G7

真实验证：

```text
Page Specification
 → Frontend Implementation
 → Real API / Real Data
 → User Task Execution
 → UI Verification
```

不得使用：

```text
截图假装完成
静态 Mock 数据假装完成
页面代码存在假装完成
```

---

# 15. P8 — FDE Real Engineering Vertical Slice

## 15.1 目标

第一次证明完整 AI Engineering OS 可以像一支真正的企业软件 FDE Team 一样，从业务意图一路走到真实软件交付。

FDE 是 Delivery Operating Model，不是第二个 AgentOS。

## 15.2 标准 FDE Team

```text
FDE Lead
 ├─ Business / Product
 ├─ System / Domain Architect
 ├─ Data / Table
 ├─ UX / UI
 ├─ Frontend
 ├─ Backend
 ├─ Table Refactor
 ├─ Class Refactor Expert
 ├─ Verification
 └─ Reviewer
```

所有成员必须使用统一 AgentOS Runtime / Policy / Capability / Evidence / Gate。

## 15.3 P8 第一条 Vertical Slice

必须选择一个真实企业复杂业务。

对于当前项目，已有 JNPF / FlowCommentService 作为 Brownfield 工程验证资产；P8 应在此前成熟的 Brownfield 证明之上，增加 Greenfield System Design + UI/Frontend，从而完成真正的 FDE 纵向闭环。

## 15.4 强制完整链

```text
1. Real Enterprise Requirement
2. Business Context Discovery
3. Product / Functional Analysis
4. Domain / Process Analysis
5. Architecture Design
6. Data / Table Design
7. UX / UI Design
8. Page Specification
9. Frontend Implementation
10. Backend Implementation
11. Table-Level Engineering / Refactoring
12. Class-Level Engineering / Refactoring
13. Real Diff
14. Real Build
15. Real Test
16. Critical Workflow Verification
17. Failure Injection
18. Diagnosis
19. Self Repair
20. Rebuild
21. Retest
22. Regression
23. Independent Review
24. Evidence Integrity Verification
25. Completion Gate
```

v2.1 已明确：任何“原型假装完成、Mock 假装完成、代码存在假装完成、截图假装完成”都不能让 FDE Vertical Slice 成立。

## 15.5 P8 Exit Gate G8

必须证明：

```text
Business → Product → System → Data → UI → Code → Verification
```

可以形成完整可追踪闭环；同时 Brownfield 工程能力：

```text
Table Refactor
Class Refactor
Real Build
Real Test
Self Repair
Regression
Review
```

能够真正接入。

---

# 16. P9 — Production Hardening / Benchmark

## 16.1 目标

把“第一条 Vertical Slice 成立”提升为：

```text
可持续
可量化
可比较
可审计
可回放
可恢复
可扩展
可替换 Provider
可替换 Agent
```

## 16.2 Benchmark Task Matrix

至少：

```text
Bug Fix
Feature
Refactor
Architecture Change
Performance Fix
Integration Feature
Runtime Change
System Design
UI / Frontend
FDE End-to-End
```

## 16.3 核心质量指标

```text
Feature Completeness
Requirement Coverage
Dependency Discovery Rate
Architecture Discovery Rate
Regression Detection Rate
False Green Rate
Mock Dependency Rate
Build Success Rate
Workflow Success Rate
Self Repair Success Rate
Evidence Completeness
Average Completion Time
Token Consumption
```

这些指标只用于测量、诊断和比较，不得变成通过删能力、删测试或降验证来获取绿色的硬性目标。

## 16.4 Exit Gate G9

达到：

```text
Auditability
Replayability
Comparability
Recoverability
Extensibility
Provider Replaceability
Agent Replaceability
```

并能运行统一 Benchmark。

---

# 17. 横向基础设施：所有 Phase 共用，不属于单独能力阶段

以下能力贯穿 P0–P9，不能被视为“某一期做完就消失”。

## 17.1 Authority Model

```text
Human / Chief Architect
 > Control Plane
 > AgentOS
 > Lead
 > Expert
 > Specialist
 > Tool / MCP / Provider
```

谁拥有哪一种权威必须结构化表达。v2.1 已把 Authority 视为 Multi-Agent 的生命线。fileciteturn5file1L1171-L1222

## 17.2 Artifact Ownership & Mutation Boundary

每次 mutation 必须可以追溯：

```text
Task
Agent
Mutation Boundary
Artifact
Before
After
Approval
Evidence
```

## 17.3 Evidence Integrity

推荐：

```text
Evidence ID
Sequence
Hash
Previous Hash
Payload
Timestamp
Actor / Signer
```

核心要求是 Append-only + Integrity Verification，而不是“区块链化”。fileciteturn2file1L1400-L1445

## 17.4 Blocking / Escalation

阻塞判据不是单纯“失败 3 次”，而是：

```text
Same Failure Signature
+
No State Progress
→
Escalation
```

## 17.5 Change Management

任何涉及上位 Contract / Policy / Gate / Architecture 的修改，都必须通过正式 Change Record。

## 17.6 Supply Chain Governance

统一治理：

```text
Artifact
Version
Source
Integrity
Compatibility
Approval
```

适用：

```text
NuGet
Model
MCP Server
Prompt Pack
Skill Package
Plugin
Container
CLI
Script
```

v2.1 已把这从单纯“依赖版本记录”提升为 AgentOS Supply Chain。fileciteturn2file1L1927-L1968

## 17.7 OS Self-Verification

Anti-Pattern 必须逐步变成：

```text
Anti-Pattern
 ↓
Example
 ↓
Adversarial Fixture
 ↓
Automated Test
```

Fake Green 等问题必须成为自动化防御资产，而不是 README。fileciteturn5file1L1125-L1167

---

# 18. 当前阶段 P2 / Runtime-A 的执行规则

从本文件发布开始，所有工程师任务的默认入口固定为：

```text
P2 / Runtime-A
```

## 开工前必须确认

```text
[ ] v2.1 Authoritative Spec
[ ] 本 Master Plan
[ ] P2 Context
[ ] P2 Checklist
[ ] P2 Gate
[ ] Runtime Architecture Spec
[ ] Runtime Design / Implementation Proposal
[ ] D1-D8 Decision Record
```

## Runtime-A 不得做

```text
❌ Cognition Engine
❌ Verification Engine
❌ Multi-Agent Team Runtime
❌ System Design Agent
❌ UI Agent
❌ FDE Team
```

可以做：

```text
✅ Runtime Kernel
✅ Agent Identity
✅ Task Identity
✅ Lifecycle Kernel
✅ Authoritative State Boundary
✅ Core Execution Loop
✅ Gate-A
✅ Runtime tests / evidence
```

## Runtime-A Exit

只有在 Gate-A 所需真实测试、真实 Build、状态验证、边界验证全部完成后，才能进入 Runtime-B。

---

# 19. 每个 Runtime / Phase 子阶段的固定产出

每个子阶段至少提交：

```text
1. Phase Context
2. Phase Checklist
3. Phase Gate Contract
4. Design Specification
5. Implementation Plan
6. Code / Configuration
7. Tests
8. Actual Execution Results
9. Evidence Bundle
10. Independent Review
11. Gate Result
12. Phase Completion Report
```

缺一项：

```text
Phase = OPEN
```

---

# 20. AI 工程师上下文防漂移协议

这是本计划最重要的执行机制之一。

## 每轮开始必须回读

```text
CURRENT PROGRAM PHASE
CURRENT SUBPHASE
CURRENT GATE
CURRENT OBJECTIVE
OUTSTANDING ITEMS
FORBIDDEN ITEMS
LAST VERIFIED BASELINE
```

## 每轮结束必须更新

```text
Completed
Not Completed
Evidence
Failures
Repairs
Open Items
Next Authorized Step
```

## 任何工程师报告必须包含

```text
1. Current Phase
2. Current Gate
3. Scope This Round
4. Completed
5. Verified
6. Unverified
7. Deferred
8. Evidence Paths
9. Gate Status
10. Next Authorized Action
```

禁止仅提供自然语言“本轮完成总结”。

---

# 21. “Completed / Verified / Unverified / Deferred” 四态纪律

以后所有制品和能力统一使用：

```text
PLANNED
IMPLEMENTED
VERIFIED
COMPLETED
DEFERRED
BLOCKED
```

核心规则：

```text
Artifact exists
≠ Implemented

Implemented
≠ Verified

Verified
≠ Phase Completed

Evidence exists
≠ Evidence proves execution
```

必须最终满足：

```text
Implementation
+
Actual Execution
+
Observed Result
+
Evidence
+
Review
+
Gate
=
Completed
```

---

# 22. Real Build / Real Test / Real Mutation 铁律

任何关键能力必须尽可能由真实工程证明。

不得用以下替代：

```text
Plan
Mock
Synthetic Result
Narrative Success
Screenshot
Existing Test File
Expected PASS
```

真实工程必须至少回答：

```text
What changed?
Why changed?
Who changed?
What actually executed?
What failed?
How was it diagnosed?
How was it repaired?
What regressed?
Who reviewed?
What evidence proves it?
```

v2.1 将 Real Mutation、Real Diff、Real Build、Real Test、Real Workflow、Real Side Effect、Real Review 作为真实能力证明基础。fileciteturn4file0L342-L356

---

# 23. Scope Control：防止强 Agent 无限扩张

强 Agent 可以主动发现和修复：

```text
Direct Safety Issue
Regression Issue
Architecture Violation
Clearly Related Defect
```

但：

```text
Optional Improvement
Large Scope Refactor
Unrelated Cleanup
```

默认进入：

```text
Deferred Finding
```

不得用“顺便优化”无限扩张当前 Phase。

这承接现有类级重构专家的 Controlled Initiative 规则。fileciteturn3file0L931-L956

---

# 24. 当前类级重构专家能力在总体计划中的位置

现有 `generic-class-refactor-expert` v6.0 是一个已经可用的 Specialist Capability，不应等待整个 AI Engineering OS 完成后才使用。

但必须区分：

```text
Skill Capability
    ✅ 当前可用

Universal Agent Wrapper
    ❌ 尚未发布

AgentOS Runtime Integration
    ❌ 尚未完成

FDE Team Specialist
    ❌ 尚未完成
```

因此当前最合理策略：

```text
继续保留 Skill 作为现有能力资产
        ↓
完成 AgentOS Runtime
        ↓
完成 Universal Agent
        ↓
通过统一 Runtime 接入 Class Refactor Expert
```

这避免重新开发已有能力，也避免把 Skill 的现有可用状态误报为 AgentOS 已完成。

---

# 25. Design Spec 与 Implementation Plan 制作顺序

以后每一个 Phase 必须按照：

```text
Master Plan
 ↓
Phase Context
 ↓
Phase Design Specification
 ↓
Phase Gate Specification
 ↓
Implementation Plan
 ↓
Task Bundle
 ↓
Execution
 ↓
Evidence
 ↓
Review
 ↓
Gate
```

不得从“先写 C#”开始。

也不得一次性提前冻结所有未来 API；v2.1 明确采用“先证明架构，再冻结接口”的路线。fileciteturn5file1L1309-L1349

---

# 26. Phase Gate 的统一判定语义

Gate 结果只允许：

```text
PASS
FAIL
BLOCKED
DEFERRED
```

其中：

### PASS
全部强制条件、真实验证、Evidence、Review 均满足。

### FAIL
至少一个强制条件未满足，或存在违反 Iron Law 的行为。

### BLOCKED
当前无法继续，需要满足明确前置条件；不能冒充 PASS。

### DEFERRED
经过正式 Change / Decision Record 被批准推迟；不得计入已完成能力。

---

# 27. 不可跨越的系统红线

任何 Phase 都不得出现：

```text
1. Skip 关键验证换绿
2. 用 Mock 替代必须真实执行的链路
3. 用代码存在证明功能成立
4. 用测试文件存在证明测试通过
5. 用 Evidence 文件存在证明真实执行
6. 用 Agent 自我总结宣布完成
7. 通过删除能力缩小问题获得 Green
8. 修改测试适配错误实现
9. 直接调用受治理之外的工具
10. 绕过 Runtime
11. 让外部 Skill / MCP / Memory 获得治理权
12. 用固定数字代替工程判断
13. 不经 Change Record 修改 Frozen Contract
14. 在当前 Gate 未通过时偷偷施工下一主 Phase
15. 把 Brownfield 重构能力误称为完整 FDE 能力
16. 把原型 / 截图 / Mock UI 当成真实 UI Engineering 完成
```

其中前 1–10 项直接对应 v2.1 已冻结的工程治理边界。fileciteturn3file1L1498-L1524

---

# 28. 最终交付矩阵

只有以下九个主 Phase 全部通过，产品才可以宣布 Production Ready：

| Phase | 能力成立标准 |
|---|---|
| P0 | 治理、权威、边界和 Baseline 成立 |
| P1 | Rule → Policy → Gate → Hook → Evidence 真正可执行 |
| P2 | AgentOS Runtime 真实成立，Agent/Capability/State/Context/Evidence 可运行 |
| P3 | Agent 能按风险获得并完成必要工程认知 |
| P4 | 系统能设计并执行最小充分、真实的验证闭环 |
| P5 | Lead + Specialists 能受治理地并行协作完成工程任务 |
| P6 | AI 能从企业业务意图正确生成结构化系统设计 |
| P7 | AI 能从用户任务完成 UI/UX/Frontend 设计并真实实现 |
| P8 | FDE Team 能把 Business → System → UI → Code → Verification 串成真实交付链 |
| P9 | 系统可审计、可回放、可恢复、可比较、可替换并具备 Benchmark |

---

# 29. 当前唯一授权下一步

```text
CURRENT AUTHORIZED WORK

P2 / Runtime-A
        ↓
D1-D8 Decision Closure
        ↓
Runtime Kernel Implementation
        ↓
Gate-A
```

当前禁止跳转：

```text
P3 Cognition
P4 Verification
P5 Team
P6 System Design
P7 UI
P8 FDE
P9 Hardening
```

可以为这些 Phase 保留高层设计资料和架构思考，但不能以“提前开发”为方式消耗当前施工主线。

---

# 30. 最终 North Star

AI Engineering OS 的最终产品承诺不是：

```text
Prompt → Code
```

而是：

```text
Enterprise Intent / Existing System
        ↓
Business / Product Understanding
        ↓
Domain / Process / Functional Design
        ↓
Architecture / Data / Table Design
        ↓
UX / UI / Interaction Design
        ↓
Frontend + Backend Engineering
        ↓
Table / Class Refactoring
        ↓
Real Build
        ↓
Real Verification
        ↓
Failure Diagnosis / Self Repair
        ↓
Regression
        ↓
Review
        ↓
Evidence
        ↓
Gate
        ↓
Completion
```

整个过程必须由：

```text
Governance
+
AgentOS Runtime
+
Cognition
+
Verification
+
Multi-Agent
+
FDE Team
+
Evidence
```

统一托管。

---

# 31. Chief Architect Final Rule

> **任何 AI 工程师无论连续执行 1 轮、8 轮还是 80 轮任务，都不得根据自己的局部上下文重新解释项目方向。项目方向以本文件和上位 v2.1 为准；当前阶段以 Active Phase Contract 为准；当前能否进入下一阶段以 Gate 为准；某项能力是否真正成立以真实执行 + Evidence + Review 为准。**

> **当未来需求、技术方案、Agent 自主判断与本文件发生冲突时，先停在当前边界内记录冲突，不得静默改线。需要改变总体路线时必须通过正式 Change Record，并重新建立受影响 Phase 的 Gate。**

这份总计划的目的不是让 AI “记住更多文字”，而是让 AI **即使忘记上一轮上下文，也无法忘记自己当前属于哪个 Phase、为什么做、做完什么才算完成、什么绝对不能做，以及下一步被授权做什么。**

---

# 32. Version / Change Log

## v1.0

- 建立 AI Engineering OS 0–9 统一实施 Phase 映射。
- 将原 v2.1 历史 0–7 与 FDE 0–9 编号差异统一为架构 Phase + Subphase 模型。
- 将当前真实站位固定为 P2 / Runtime-A。
- 固化 P0–P9 的总体目标、能力、任务、产物、Exit Gate。
- 将 System Design、UI/UX/Frontend、FDE 从“未来愿景”提升为正式施工阶段 P6/P7/P8。
- 固化 Context Drift Protocol、四态完成纪律、Real Engineering 验证纪律、Change Management 与跨 Phase 红线。
- 明确现有 Class Refactor Skill v6.0 是可用 Specialist Capability，但不等同于 Universal Agent / AgentOS 已完成。
