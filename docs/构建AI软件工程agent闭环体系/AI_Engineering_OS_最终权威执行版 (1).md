# AI Engineering Operating System
## Final Authoritative Execution Specification v2.0
### AI Engineering OS — Governance × AgentOS × Engineering Intelligence × Real Engineering

> **文档状态：AUTHORITATIVE / EXECUTION BASELINE**
>
> 本文档由《构建智能体操作系统1.0》与《构建智能体操作系统1.1》的完整内容归并形成。
>
> 本版本是后续 Design Specification、Implementation Plan、Phase Plan、Test Plan、Gate 与 Completion Report 的**唯一上位执行依据**。
>
> **核心目标：从 Specification of Intent 升级为 Specification of Enforceable Behavior。**

---

# 0. 文档定位与权威等级

## 0.1 文档使命

AI Engineering OS 的第一目标不是描述“我们希望 AI 怎么工作”，而是建立一种可执行的工程操作系统，使不同能力水平的 AI Agent 都必须在统一的治理、认知、执行、验证、证据与恢复体系中工作，并对真实工程结果负责。

系统必须防止以下退化：

- 做到前几步后遗忘整体目标；
- 错误缩小问题范围；
- 为了测试绿色而删除或弱化核心能力；
- 以 Mock、模拟结果或叙述替代真实工程证据；
- 只完成局部任务却宣布 Phase 完成；
- 强 Agent 自主性被无必要压制；
- 弱 Agent 因遗漏高级工程思考而直接导致工程失控。

1.0 明确了这一使命及其治理、AgentOS、工程智能、真实工程四大骨架；1.1 进一步补齐了正式状态、恢复、权限、证据完整性、阻塞升级、术语和变更治理等运行时维度。

## 0.2 权威等级

系统中的权威由以下顺序决定：

```text
Human / Chief Architect Authority
        ↓
AI Engineering Control Plane
        ↓
AI Engineering OS Authoritative Specification
        ↓
Active Phase Contract / Approved Design Specification
        ↓
Implementation Plan
        ↓
Agent / Team Decisions
        ↓
External Capability / Tool / Provider
```

任何下级材料不得静默覆盖上级约束。

## 0.3 规范语言

本文档使用以下规范：

- **MUST / 必须**：不可违反；违反即 Gate Failure。
- **MUST NOT / 不得**：不可执行；违反即 Gate Failure。
- **SHOULD / 应**：默认要求；偏离必须有明确理由和证据。
- **MAY / 可以**：允许实现自由裁量。
- **RECOMMENDED / 推荐**：实现建议，不是架构合同。

## 0.4 规格与实现的边界

本文档冻结的是：

```text
Architecture
Authority
Behavior
Contracts
States
Policies
Evidence
Gates
Completion Semantics
Recovery Semantics
Phase Order
```

本文档原则上不冻结：

```text
具体线程模型
具体同步原语
具体数据库产品
具体消息通道类型
具体工作区实现
具体测试数量上限
具体毫秒级超时
具体 Git 策略
具体依赖库 minor version
```

除非未来经 Real Pilot + Evidence + Change Record 证明某一实现选择确实应升级为架构合同。

---

# 1. 产品定义

AI Engineering OS 不是：

```text
Prompt Library
Skill Collection
MCP Collection
Agent Collection
Test Runner
Coding Assistant
```

它是：

```text
Governance
+
Agent Runtime
+
Engineering Cognition
+
Verification Intelligence
+
Team Coordination
+
Evidence
+
Real Engineering Execution
```

最终用户只需要提出工程任务；系统内部负责理解、分析、规划、实施、验证、修复、复核、举证与完成裁决。

---

# 2. 系统最终目标

目标状态：

```text
                          AI ENGINEERING OS
                                 │
           ┌─────────────────────┼─────────────────────┐
           │                     │                     │
           ▼                     ▼                     ▼
      GOVERNANCE             AGENTOS             ENGINEERING
        CONTROL                                      INTELLIGENCE
           │                     │                     │
           │              ┌──────┴──────┐              │
           │              │             │              │
           │           SINGLE       AGENT TEAM          │
           │            AGENT                           │
           │              │             │              │
           │              └──────┬──────┘              │
           │                     │                     │
           └─────────────────────┼─────────────────────┘
                                 ▼
                        REAL ENGINEERING
                                 │
                 ┌───────────────┼───────────────┐
                 ▼               ▼               ▼
               THINK           BUILD           VERIFY
                 ↓               ↓               ↓
              REVIEW           REPAIR          EVIDENCE
```

成功不是“代码生成完成”，而是：

> 一个真实 Agent Team 能在 Governance、AgentOS、Cognition、Verification、Evidence 的共同约束下，对真实企业级代码完成真实工程修改，并通过真实 Build、真实 Test、真实 Failure Recovery、真实 Review，最终交付无功能缩水且有完整证据的结果。

---

# 3. 十条工程铁律（IRON LAWS）

## IRON-01 — Expert Is a Real Agent

Expert 必须是有身份、有状态、有生命周期、有任务、有技能、有上下文、有能力、有证据责任的真实 Agent，而不是一个 Prompt。

最低语义：

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

## IRON-02 — Runtime Must Not Own Domain Intelligence

AgentOS Runtime 不得内置具体项目业务规则、具体 JNPF 逻辑、具体类级重构策略或领域知识。

Runtime 负责：

```text
Lifecycle
State
Execution
Capability
Policy
Security
Evidence
Coordination
Recovery
```

领域智能属于 Expert / Engineering Intelligence。

## IRON-03 — Expert Must Not Bypass Runtime

所有工程能力必须经过 Runtime：

```text
Expert
 ↓
AgentOS Runtime
 ↓
Capability Authorization
 ↓
Capability
 ↓
Tool / MCP / Provider
```

不得直接绕过 Runtime 调用 File、Process、Shell、Build、Test、Git 或 External Service。

## IRON-04 — No Function Loss for Green

禁止为了绿色而：

```text
删除功能
删除业务逻辑
删除事件
删除 API
弱化 Assertion
Skip Test
取消真实 Build
全部 Mock
硬编码结果
空实现
TODO 替代实现
注释代替实现
缩小验证范围
修改测试适配错误代码
```

任何此类行为均属于治理违规，而非正常工程优化。

## IRON-05 — Governance Must Be Executable

关键治理规则不能只存在于 Markdown、Prompt、Skill 或 Memory 中。

必须最终进入可执行层：

```text
Rule
 ↓
Policy
 ↓
Evaluation
 ↓
Hook / Gate / State Constraint
```

## IRON-06 — Completion Must Be Evidence-Backed

Agent 无权直接设置 Completed。

Completion 必须同时满足：

```text
Runtime State
+
Required Evidence
+
Gate Evaluation
+
Required Validation
+
Required Review
```

## IRON-07 — Governance Sovereignty

AI Engineering Control Plane 是工程治理唯一权威。

第三方 Skill、MCP、Memory、Plugin、Prompt、Framework、Agent 都不得覆盖：

```text
Rules
Policies
Contracts
Gates
Completion Criteria
```

## IRON-08 — External Capability Isolation

外部工具只能提供能力，不能拥有治理权威。

例如 Serena 可以执行 Symbol Search、Reference Search、Code Navigation，但不能关闭 Gate、修改 Policy、删除 Evidence Requirement 或宣布 Phase Complete。

## IRON-09 — Single Authoritative Skill Registry

项目只能有一个权威 Skill Registry。

外部 Skill 可以被吸收、参考、迁移、改写，但不能形成第二套平级治理系统。

## IRON-10 — Real Engineering Over Synthetic Success

关键能力必须通过真实证据证明：

```text
Real Mutation
Real Diff
Real Build
Real Test
Real Workflow
Real Side Effect
Real Review
Real Recovery
Real Evidence
```

模拟结果只能辅助，不能替代关键工程证据。

---

# 4. 外部能力治理

## 4.1 Superpowers

定位为工程方法来源，而非第二治理系统：

```text
Superpowers
 ↓
抽取优秀工程方法
 ↓
进入 AI Engineering OS
 ↓
成为 Rules / Workflow / Skills / Verification 方法
```

重点吸收：

```text
Planning
Verification Before Completion
Self Review
Structured Execution
Problem Decomposition
```

## 4.2 Serena MCP

定位：External Engineering Capability Provider。

典型能力：

```text
Symbol Search
Reference Search
Code Navigation
Semantic Discovery
```

调用必须经过：

```text
Expert
 ↓
Runtime
 ↓
Authorization
 ↓
MCP Adapter
 ↓
Serena
```

## 4.3 Memory Provider

Memory 必须通过 AgentOS Memory Contract 接入：

```text
AgentOS Memory Contract
        ↓
Memory Provider
        ↓
Implementation Provider
```

Provider 可替换。Agent 不得依赖具体 Memory 实现。

## 4.4 统一 External Capability Boundary

以下均属于 External Capability：

```text
External Skill
External MCP
Memory Provider
Plugin
CLI
Service
Model Provider
Script
Container
```

全部必须进入：

```text
Capability Registry
Authorization
Policy
Evidence
```

---

# 5. 总体架构边界

```text
┌──────────────────────────────────────────────────────────────┐
│                 AI ENGINEERING CONTROL PLANE                │
│ Governance │ Rules │ Policies │ Workflow │ Skill Routing    │
│ Gates │ Templates │ Evidence │ Project Constitution         │
└───────────────────────────┬──────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────┐
│                         AGENTOS                              │
│ Identity │ State │ Context │ Lifecycle │ Capability          │
│ Policy │ Hooks │ Evidence │ Memory │ Team Runtime            │
│ Recovery │ Authorization │ Execution                         │
└───────────────────────────┬──────────────────────────────────┘
                            │
              ┌─────────────┼─────────────┐
              ▼             ▼             ▼
           Expert         Lead         Specialist
              │             │             │
              └─────────────┼─────────────┘
                            ▼
                 ENGINEERING INTELLIGENCE
                            │
       ┌────────────────────┼────────────────────┐
       ▼                    ▼                    ▼
 Cognitive Engine    Verification Engine    Review Engine
       │                    │                    │
       └────────────────────┼────────────────────┘
                            ▼
                       REAL TOOL LAYER
                            │
       ┌────────────────────┼─────────────────────┐
       ▼                    ▼                     ▼
      File                 Build                 Test
      Git                  DB                    MCP
      CLI                  Browser               External
                            │
                            ▼
                      EVIDENCE LEDGER
                            │
                            ▼
                           GATES
                            │
                    ┌───────┴───────┐
                    ▼               ▼
                 Continue        Human Gate
```

核心方向只能向下：

```text
Control Plane → AgentOS → Expert → Capability → Tool/Provider
```

不得形成 Runtime Core 对具体 Expert、JNPF 或具体业务重构逻辑的反向依赖。

---

# 6. Authority Model

必须明确“谁对什么拥有最终权威”：

| Authority | 权威范围 |
|---|---|
| Human / Chief Architect | 最终治理决策、重大架构裁决、不可逆风险裁决 |
| Control Plane | Rule、Policy、Gate、Completion Criteria |
| AgentOS | Runtime State、Lifecycle、Capability Authorization、Execution Enforcement |
| Lead Agent | Team Decision、Delegation、Conflict Resolution |
| Expert | 领域工程推理与工程策略 |
| Specialist | 被委派范围内的专业任务 |
| Tool / MCP | 执行被授权的 Capability |
| Memory Provider | 存储与检索 |

任何低层组件不得伪装成高层权威。

特别注意：

```text
Tool ≠ Authority
Memory ≠ Governance
Skill ≠ Capability
Agent ≠ Gate Authority
Lead ≠ Policy Authority
```

---

# 7. AgentOS 核心运行时合同

## 7.1 Agent Contract

架构级合同至少覆盖：

```text
Agent Identity
Agent State
Agent Lifecycle
Agent Task
Agent Context
Agent Skills
Agent Capability Set
Agent Evidence Responsibility
```

## 7.2 Capability Contract

至少覆盖：

```text
Capability
Capability Provider
Capability Policy
Capability Authorization
Capability Evidence
```

代表性 Capability：

```text
Search
File
Build
Test
Git
DB
Memory
MCP
External Service
```

## 7.3 Memory Boundary

至少区分：

```text
Working Memory
Task Memory
Agent Memory
Project Knowledge
Long-Term Memory
```

Memory 不得成为治理绕过通道。

---

# 8. Execution State Model

这是 1.1 对 1.0 最重要的结构升级之一。

不得把“业务阶段”“Task 生命周期”“单次操作执行状态”揉成一个超级枚举。

## 8.1 Task State

```text
Created
Active
Blocked
Completed
Failed
Escalated
Cancelled
```

## 8.2 Execution Stage

```text
Classification
Discovery
Contract
Cognition
Planning
Implementation
Build
Verification
Regression
Review
Evidence
Gate
```

## 8.3 Operation State

```text
Pending
Running
Succeeded
Failed
TimedOut
Cancelled
Recovered
```

核心关系：

```text
Task
 ├── State
 ├── CurrentStage
 └── Operations[]
```

因此可以合法表达：

```text
Task = Active
Stage = Verification

Agent A Operation = Succeeded
Agent B Operation = Running
Agent C Operation = Failed
```

## 8.4 State Transition Rule

所有状态转换必须从 AgentOS State Authority 的视角具备原子性 / 线性化语义。

实现方式不冻结。可以由 lock、CAS、事务、actor、event loop 等实现，但具体实现不能改变合同语义。

非法状态转换必须由 Runtime 拒绝。

---

# 9. Policy Semantics

## 9.1 Policy Decision

采用：

```text
ALLOW
ALLOW_WITH_EVIDENCE
BLOCK
REQUIRE_HUMAN
DENY
```

语义：

- **ALLOW**：允许执行。
- **ALLOW_WITH_EVIDENCE**：允许执行，但必须产生规定证据。
- **BLOCK**：当前条件不足，补齐条件后可以继续。
- **REQUIRE_HUMAN**：需要人工决策。
- **DENY**：明确禁止，不能通过补证据绕过。

不以简单的数字优先级取代语义。

## 9.2 Policy Scope

典型作用域：

```text
Global
Project
Module
Task
Operation
```

更具体作用域可以提供更细粒度约束，但不得覆盖更高权威级别不可变法律。

## 9.3 Policy Authority

真正决定覆盖关系的是 Authority Hierarchy，而不是单纯“越具体优先级越高”。

必须保护：

```text
L0 Immutable Engineering Law
```

任何 Task Policy 不得覆盖。

## 9.4 Policy Budget

同步授权必须具有有界执行预算，并在预算耗尽时 fail closed。

具体数字不是架构铁律；应通过 Benchmark 与 Implementation Phase 确定。

---

# 10. Hook 与 Gate

## 10.1 Hook

典型 Hook：

```text
PreAction
PostAction
PreMutation
PreBuild
PreTest
PreComplete
```

Hook 的职责是 Enforcement，不承担领域智能。

## 10.2 Gate

统一模型：

```text
Gate
GateCondition
GateResult
GateFailure
GateEvidence
```

典型 Gate：

```text
Implementation Gate
Build Gate
Test Gate
Repair Gate
Review Gate
Completion Gate
Phase Gate
```

## 10.3 Hook 与 Gate 的区别

```text
Hook = 执行动作边界上的实时 Enforcement
Gate = 是否允许进入下一阶段 / Completion 的裁决
```

---

# 11. Engineering Cognition Engine

## 11.1 目标

让 Agent 不容易遗漏高级工程师本应思考的问题，但不替 Agent 做业务决策。

核心原则：

> **强制 Agent 进行与任务风险匹配的工程思考。**

## 11.2 Minimum Sufficient Thought

```text
Task
 ↓
Task Classification
 ↓
Risk Classification
 ↓
Required Cognition
```

不同任务产生不同认知要求。

### 低风险修改

最低可能包含：

```text
Local Context
Contract
Test
```

### Repository 修改

应至少考虑：

```text
Callers
Dependencies
Tenant
Transaction
Query Semantics
Performance
Tests
```

### 类级重构

应至少考虑：

```text
Structure
Dependency Graph
Public Contract
Business Boundary
Cross-Cutting Concerns
Architecture
Behavior
Performance
Regression
```

### Runtime Core 修改

应至少考虑：

```text
Architecture
Lifecycle
Concurrency
Security
Compatibility
Capability Boundary
Regression
```

## 11.3 Cognitive Pipeline

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

## 11.4 Cognitive Evidence

必须产生结构化结果：

```text
DiscoveryResult
DependencyGraph
BoundaryAnalysis
ContractModel
ArchitectureAssessment
RiskAssessment
ImplementationPlan
```

仅有自然语言陈述不足以证明完成认知要求。

## 11.5 Risk-Weighted Cognition

不得使用机械数字替代工程判断，例如“超过 5 个调用者才建立完整调用图”不得作为统一规则。

认知深度应由至少以下因素共同决定：

```text
Call Graph Size
Change Type
Criticality
Cross-Cutting Concerns
Architectural Layer
Behavioral Risk
```

## 11.6 Contract Completeness

不得以固定百分比简单表示“合同完整”。应逐维度验证：

```text
Public API
Behavior
Events
Dependencies
Security
Tenant
Persistence
External Integration
Lifecycle
```

每个维度至少达到：

```text
Detected
Analyzed
Evidence
```

---

# 12. Intelligent Verification Engine

## 12.1 目标

测试不是“跑 dotnet test”，而是设计并执行足以证明真实完成的验证策略。

## 12.2 五层验证

```text
Structural Verification
Behavioral Verification
Workflow Verification
Integration Verification
Regression Verification
```

## 12.3 Verification Pipeline

```text
Requirement
 ↓
Feature Model
 ↓
Business Flow
 ↓
Critical Path
 ↓
Dependency Graph
 ↓
Risk Assessment
 ↓
Verification Requirements
 ↓
Test Selection
 ↓
Realism Policy
 ↓
Execution
 ↓
Observation
 ↓
Regression
 ↓
Evidence
```

## 12.4 Minimum Sufficient Verification

禁止两个极端：

```text
穷举一切
```

和：

```text
只做简单 Assertion
```

目标是：

> **最少但足够证明核心功能真实成立的验证集合。**

## 12.5 Realism Policy

统一 Mock 分级：

```text
REAL_REQUIRED
REAL_PREFERRED
MOCK_ALLOWED
MOCK_REQUIRED
```

业务核心通常包括：

```text
API
Repository
DB
Event
Handler
Core Workflow
```

关键链路默认不能全部 Mock。

## 12.6 Critical Path

典型：

```text
API
 ↓
Application
 ↓
Domain
 ↓
Persistence
 ↓
Event
 ↓
Handler
 ↓
Side Effect
```

至少一条关键业务链必须真实执行。

## 12.7 Test Selection

依据：

```text
Change Set
Dependency Graph
Risk
Affected Projects
Critical Flow
```

选择必要的：

```text
Unit
Integration
Workflow
Critical E2E
Regression
```

## 12.8 Verification Budget

预算可包含：

```text
Time Budget
Token Budget
Test Count Budget
Tool Call Budget
Cost Budget
```

达到预算时系统应重新评估，可：

```text
Continue
Split
Escalate
```

不能以固定最大数量自动停止导致验证缩水。

真正需要控制的是：

> **Relevant Verification Coverage，而不是 Maximum Tests。**

## 12.9 Verification Evidence

至少记录：

```text
What ran
What was real
What was mocked
What side effects happened
What data changed
What events occurred
What external calls occurred
```

## 12.10 Fake Green Detection

至少识别：

```text
Assertion Weakened
Test Deleted
Test Skipped
Mock Replaced Real
Hard-Coded Result
Fake API
Fake Event
Empty Implementation
```

---

# 13. Multi-Agent Team Runtime

## 13.1 定义

不是 Agent 群聊，而是：

> **Hierarchical Engineering Team Runtime**

## 13.2 基本团队模型

```text
Lead Agent
│
├── Discovery Specialist
├── Architecture Specialist
├── Implementation Specialist
├── Verification Specialist
└── Reviewer Specialist
```

数量动态决定，不固定为某个数字。

## 13.3 Dynamic Team Sizing

输入：

```text
Task Complexity
Risk
Parallelism
Capability Gap
Cost
```

输出：

```text
Minimum Sufficient Team
```

## 13.4 Role Authority

默认：

```text
Discovery          READ ONLY
Architecture       READ ONLY
Test Analysis      READ ONLY
Implementation     WRITE AUTHORIZED
Verification       READ / EXECUTE
Reviewer           READ ONLY
Lead               DECISION AUTHORITY
```

## 13.5 Context Model

区分：

```text
Shared Context
Private Context
Task Context
Project Context
Evidence Context
```

避免所有 Agent 共享巨大上下文。

## 13.6 DAG Execution

支持真正并发的分析任务：

```text
           Discovery
         /     |      \
Architecture  Test   Performance
         \     |      /
              Plan
               ↓
        Implementation
               ↓
          Verification
               ↓
             Review
```

## 13.7 Governed Communication Graph

默认可设：

```text
Specialist ↔ Specialist = DENY
```

但经过 Policy / Capability / Permission 授权，可以允许必要的直接通信：

```text
ALLOW_DIRECT
```

通信规则不得永久写死成单一拓扑。

## 13.8 Artifact Ownership

必须定义：

```text
File Ownership
Artifact Ownership
Task Ownership
Evidence Ownership
```

所有 Mutation 必须绑定：

```text
Task
Agent
Mutation Boundary
Artifact
```

两个 Agent 默认不得同时修改同一 Artifact。

## 13.9 Team Completion

不能因为所有 Specialist 都返回成功就 Complete。

必须：

```text
Team Results
+
Evidence
+
Gate
+
Lead Decision
```

---

# 14. Artifact Ownership & Mutation Model

## 14.1 核心问题

每次变更必须能够回答：

```text
Who owns it?
Who may modify?
Who may approve?
Who may revert?
Who may review?
```

## 14.2 最低 Mutation Contract

每次 Mutation 必须具有：

```text
Task ID
Agent ID
Workspace / Mutation Boundary
Artifact Identity
Authorization
Before Snapshot
Mutation
After Snapshot
Diff
Evidence
```

## 14.3 并行隔离

规则是：

> **每个并行变更任务必须拥有独立且可追踪的 Mutation Boundary。**

实现可采用：

```text
Branch
Worktree
Workspace
Container
Sandbox
Virtual FS
```

具体方案由实现阶段决定。

---

# 15. Evidence Ledger 与证据完整性

## 15.1 每个工程动作必须可追溯

至少包含：

```text
Task ID
Agent ID
Team ID
Phase
Stage
Action
Capability
Input
Output
Before Hash
After Hash
Tool
Timestamp
Policy Decision
Result
Evidence
```

## 15.2 证据类型

至少：

```text
Discovery Evidence
Contract Evidence
Plan Evidence
Mutation Evidence
Diff Evidence
Build Evidence
Test Evidence
Repair Evidence
Review Evidence
Gate Evidence
Recovery Evidence
```

## 15.3 Evidence Integrity

采用：

```text
Evidence ID
Sequence
Hash
Previous Hash
Payload
Timestamp
Actor / Signer
```

最低要求：

> **Append-only + Integrity Verification**

不要求为了实现证据完整性而“区块链化”。

未来可扩展：

```text
Merkle
Digital Signature
WORM Storage
External Attestation
```

---

# 16. Resilience & Recovery

1.1 对 1.0 的另一个关键补全是：不能只关心 Agent 做错，还必须定义 Runtime 自己失败时怎么办。

系统必须考虑至少：

```text
Agent Crash
Runtime Restart
Machine Restart
Evidence Write Failure
Tool Timeout
Partial Mutation
```

不得出现：

```text
状态说成功
实际未成功
```

## 16.1 Transactional Work Unit

每个可变更 Work Unit 至少具有：

```text
Before Snapshot
Mutation Boundary
After Snapshot
Recovery Strategy
```

## 16.2 Recovery Strategy

允许：

```text
Rollback
Compensating Change
Forward Repair
Manual Resolution
```

不得把“自动 Git Reset”作为所有失败的默认策略。

## 16.3 Rollback ≠ Self Repair

Rollback 代表恢复。

Self Repair 必须是：

```text
Analyze
 ↓
Diagnose
 ↓
Modify
 ↓
Verify
```

仅执行 Rollback 不构成 Self Repair。

---

# 17. Failure Ownership & Escalation

所有失败必须进行 Ownership Classification：

```text
PRE_EXISTING
CURRENT_CHANGE
TRANSITIVE_IMPACT
UNKNOWN
```

若判断为 PRE_EXISTING，必须留下：

```text
Baseline Evidence
Reason
Impact
Decision
```

不得只写“legacy 问题”。

## 17.1 Blocking Protocol

失败升级不能只依靠“连续三次”。

必须考虑：

```text
SameFailureSignature
+
NoStateProgress
```

形成：

```text
RepeatedFailure
+
NoProgress
→
Escalation
```

具体次数可以作为策略配置，而不是不可变工程法则。

## 17.2 Human Gate

仅当真正需要决策时升级，包括：

```text
Breaking Change
Architecture Conflict
Security Exception
Irreversible Migration
Scope Expansion
Critical Production Risk
Policy Override
```

普通工作：

```text
Search
Coding
Testing
Repair
Regression
```

原则上 Agent 自主闭环。

---

# 18. Controlled Initiative

强 Agent 不应该被 OS 降级。

允许主动：

```text
搜索
发现问题
优化
修复
增加验证
发现关联风险
```

但主动修复必须有边界。

可以直接处理：

```text
Direct Safety Issue
Regression Issue
Architecture Violation
Clearly Related Defect
```

以下默认转为：

```text
Deferred Finding
```

```text
Optional Improvement
Large Scope Refactor
Unrelated Cleanup
```

防止无限扩张任务范围。

---

# 19. Anti-Pattern Library

Anti-Pattern 不是 README 装饰品，而应成为测试资产：

```text
Anti-Pattern
 ↓
Example
 ↓
Adversarial Fixture
 ↓
Automated Test
```

至少覆盖：

```text
Fake Green
Skip Build
Skip Test
Delete Test
Weaken Assertion
Direct Tool Bypass
Fake API
Fake Event
Empty Implementation
Narrative Success
```

系统必须能够故意触发违规行为并验证 Hook / Policy / Gate 是否 BLOCK。

---

# 20. Phase 模型与严格顺序

整个工程保持 8 个主阶段：

```text
Phase 0  Baseline & Contract Freeze
Phase 1  Control Plane Governance Integration
Phase 2  AgentOS Governance & Capability Boundary
Phase 3  Engineering Cognition Engine
Phase 4  Intelligent Verification Engine
Phase 5  Multi-Agent Team Runtime
Phase 6  Expert Integration & Real Engineering
Phase 7  Production Hardening & Benchmark
```

严格顺序：

```text
0 → 1 → 2 → 3 → 4 → 5 → 6 → 7
```

前一 Phase 未通过 Gate，不得宣布下一 Phase 正式完成。

---

# 21. Phase 0 — Baseline & Contract Freeze

## 目标

确认并冻结：

```text
AI Engineering Control Plane
AgentOS Runtime Foundation
Expert Agent Foundation
```

## 必查

```text
Rules
Workflow
Skills
Templates
Gates
Project Constitution
Baseline
Active Phase
Decision Records
```

```text
Identity
State
Context
Lifecycle
Execution
Capability
Memory
Evidence
Hooks
Runtime Tests
```

外部能力：

```text
Superpowers
Serena MCP
ecc-memory
Other MCP
Other Providers
```

## 必须证明

```text
0 Architecture Boundary Violation
0 Governance Authority Duplication
0 Unknown Critical Component
```

## Gate-0

```text
[✓] Control Plane preserved
[✓] AgentOS baseline frozen
[✓] External roles defined
[✓] Architecture boundary verified
[✓] Existing tests preserved
```

---

# 22. Phase 1 — Control Plane Governance Integration

## 目标

不是重写 Control Plane，而是把现有治理能力转成 AgentOS 可理解、可执行的 Machine Policy。

## 核心能力

```text
Policy
PolicyRule
PolicyScope
PolicySeverity
PolicyDecision
PolicyEvidenceRequirement
```

```text
Gate
GateCondition
GateResult
GateFailure
GateEvidence
```

```text
PreAction Hook
PostAction Hook
PreMutation Hook
PreBuild Hook
PreTest Hook
PreComplete Hook
```

至少机器化：

```text
Real Build Required
Real Test Required
Diff Required
Contract Preservation
No Function Loss
No Skip
No Fake Green
Completion Verification
```

## Adversarial Validation

至少故意制造：

```text
Skip Build
Fake Pass
Skip Test
Delete Test
Weaken Assertion
Complete Without Evidence
Direct Tool Bypass
```

均必须被阻止。

## Gate-1

> 所有高风险工程规则均存在机器 Enforcement，违规 Agent 无法进入 Completed。

---

# 23. Phase 2 — AgentOS Governance & Capability Boundary

## 目标

正式隔离：

```text
Expert
Runtime
Capability
External MCP
Memory
```

## Architecture Contract

需要建立：

```text
Agent Contract
Capability Contract
MCP Adapter Contract
Memory Contract
Capability Registry
Permission Registry
```

## Gate-2

必须证明：

```text
[✓] Expert cannot bypass Runtime
[✓] Runtime does not depend on Expert domain logic
[✓] MCP cannot bypass Policy
[✓] Memory Provider cannot alter Governance
[✓] Unauthorized capability is blocked
```

---

# 24. Phase 3 — Engineering Cognition Engine

## 目标

使弱 Agent 获得认知脚手架，使强 Agent 保有自主探索空间。

核心能力：

```text
Task Classifier
Risk Classifier
Cognitive Requirement Resolver
Discovery Engine
Architecture Analyzer
Risk Analyzer
Cognitive Evidence
```

## Gate-3

使用真实任务验证：

```text
Bug Fix
Feature
Refactor
Runtime Change
```

证明：没有完成 Required Cognition 就不能进入 Implementation。

---

# 25. Phase 4 — Intelligent Verification Engine

## 目标

把“跑测试”升级为“设计足够证明真实完成的验证策略”。

核心能力：

```text
Verification Planner
Test Selector
Mock Boundary Policy
Critical Path Analyzer
Behavior Observer
Fake Green Detector
Verification Evidence Model
```

## Gate-4

至少验证：

```text
真实业务 API
真实数据库操作
真实 Event Flow
真实 Workflow
真实 Regression
至少一次故意失败
```

并证明：

```text
Failure → Detection → Diagnosis
```

---

# 26. Phase 5 — Multi-Agent Team Runtime

## 目标

将 Single Expert 升级为 Lead + Managed Specialists。

必须具备：

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

## Gate-5

至少演示一个真实 Lead + 多个 Specialists：

```text
Parallel Analysis
Result Aggregation
Decision
Implementation
Verification
```

同时满足：

```text
No File Conflict
No Permission Boundary Violation
No Gate Bypass
No Evidence Loss
```

---

# 27. Phase 6 — Expert Integration & Real Engineering

这是第一条真正的 AI Engineering OS Vertical Slice。

## Pilot

使用真实 JNPF 工程。

首选目标：

```text
FlowCommentService
```

或由 Baseline 审核确定的等价高复杂度真实类。

## 强制闭环

```text
1. Baseline Snapshot
2. Task Classification
3. Cognitive Requirements
4. Dependency Discovery
5. Business / Architecture Analysis
6. Contract Freeze
7. Verification Plan
8. Controlled Mutation
9. Real Diff
10. Real Build
11. Real Verification
12. Failure Injection
13. Failure Ownership
14. Self Diagnosis
15. Self Repair
16. Rebuild
17. Regression
18. Reviewer
19. Evidence Integrity Verification
20. Completion Gate
```

## Real Mutation

必须留下：

```text
Before File
After File
Git Diff
Mutation Evidence
```

且能回答：

```text
改了什么？
为什么改？
谁执行？
谁批准？
```

## Real Build

必须是真实：

```text
项目
Compiler
Execution
ExitCode
```

禁止：

```text
Skip
Mock Build
Synthetic Build
Narrative Success
```

## Real Test

至少覆盖与任务复杂度相匹配的：

```text
Unit
Integration
Critical Workflow
Regression
```

## Failure Injection

故意制造 Build 或 Test Failure：

```text
Detect
 ↓
Diagnose
 ↓
Repair
 ↓
Rebuild
 ↓
Retest
```

## Gate-6

必须证明：

```text
真实 JNPF 类
真实修改
真实 Diff
真实 Build
真实 Test
真实 Failure
真实 Self Repair
真实 Reviewer
完整 Evidence
```

任一缺失：Phase 6 FAIL。

---

# 28. Phase 7 — Production Hardening & Benchmark

## 目标

从“能跑”升级到：

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

## Benchmark Task Set

至少：

```text
Bug Fix
Feature
Refactor
Architecture Change
Performance Fix
Integration Feature
Runtime Change
```

## Quality Metrics

至少持续采集：

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

这些指标用于比较与诊断，不得反过来成为为了达标而牺牲真实能力的数字铁律。

---

# 29. 标准 Phase Execution Protocol

每个 Phase 必须严格遵循：

```text
PHASE START
    ↓
Baseline Verification
    ↓
Phase Context Read
    ↓
Active Phase Contract Read
    ↓
Task Checklist Establishment
    ↓
Implementation / Execution
    ↓
Build
    ↓
Test
    ↓
Self Evaluation
    ↓
Self Repair
    ↓
Reviewer Review
    ↓
Evidence Collection
    ↓
Gate Evaluation
    ↓
Phase Closure
```

不得出现：

```text
完成 Task A
→ 宣布 Phase 完成
```

Phase 完成必须是整个 Phase Contract 完成。

---

# 30. Phase Context / Checklist / Gate 三件套

为防止上下文漂移，每个 Phase 必须存在：

```text
PHASE-CONTEXT.md
PHASE-CHECKLIST.md
PHASE-GATE.md
```

每次工作轮开始时重新读取：

```text
Current Phase
Current Gate
Outstanding Items
```

## PHASE-CHECKLIST 至少包含

```text
Tasks
Required Artifacts
Required Tests
Required Evidence
Gate Conditions
Open Findings
Deferred Findings
```

任何完成报告必须能够回指：

```text
Checklist
+
Evidence
+
Gate
```

---

# 31. 每个 Phase 的最小交付物

任何 Phase 至少产生：

```text
1. Implementation Artifacts
2. Tests
3. Evidence
4. Review Result
5. Gate Result
6. Phase Completion Report
```

缺少任何一项：

```text
Phase = OPEN
```

---

# 32. Completion Contract

任何 Agent 不得直接宣布：

```text
Completed = true
```

必须由 Completion Evaluator 检查：

```text
Required Artifacts
Required Evidence
Required Gates
Required Validation
Required Review
State Legality
Recovery Integrity
```

典型类级重构完成条件：

```text
[✓] Discovery
[✓] Contract
[✓] Plan
[✓] Mutation
[✓] Diff
[✓] Build
[✓] Test
[✓] Regression
[✓] Reviewer
[✓] Evidence
```

全部成立才允许：

```text
COMPLETED
```

---

# 33. Human Gate Protocol

Human 不参与普通工程细节。

只有以下情况升级：

```text
Breaking Change
Architecture Conflict
Security Exception
Irreversible Migration
Scope Expansion
Critical Production Risk
Policy Override
Unresolved Governance Conflict
```

原则：

> **只有真正需要人类决策时才进入 Human Gate；其余工作必须尽可能自主闭环。**

---

# 34. AI Engineering Console

控制台不是普通 Dashboard，而是：

> **AI Engineering OS Control Plane UI / Operational Control Plane**

至少应呈现：

```text
Current Project
Current Phase
Current Task
Active Agent
Team
Agent State
Current Cognitive Stage
Current Gate
Policy Decisions
Tool Calls
Build
Test
Repair
Evidence
Risks
Human Gate
Recovery State
```

示例状态应能够明确呈现：

```text
State: IMPLEMENTATION

Gates:
Discovery  ✅
Contract   ✅
Plan       ✅
Mutation   ✅
Build      ❌
Test       ⏳
Review     ⏳

Reason:
REAL_BUILD_EVIDENCE_MISSING

Completion:
BLOCKED
```

---

# 35. 变更管理（Change Management）

任何对本权威执行版的修改都必须显式管理，不得静默覆盖 Baseline。

## 35.1 Change Record 至少记录

```text
Change ID
Source
Reason
Affected Sections
Before
After
Compatibility Impact
Migration Impact
Validation Evidence
Approval
Effective Version
```

## 35.2 变更原则

```text
No Silent Overwrite
No Hidden Contract Change
No Runtime Change Without Contract Review
No Gate Relaxation Without Explicit Approval
```

新增能力可以是 additive，但若改变既有行为、权限、Gate、Evidence 或 Completion 语义，必须进入正式变更记录。

---

# 36. Software / AgentOS Supply Chain Governance

所有关键外部依赖统一治理：

```text
Artifact
Version
Source
Integrity
Compatibility
Approval
```

范围不仅是 NuGet / MCP，还包括：

```text
Model
MCP Server
Prompt Pack
Skill Package
Plugin
Container
CLI
Script
Provider
```

具体版本可以锁定，但“版本锁定”本身不等于治理完成；必须同时考虑兼容性、完整性与批准状态。

---

# 37. AI Engineering OS 自验证

AI Engineering OS 不仅要验证业务代码，也必须验证自己。

最低方向：

```text
Governance Self Test
Runtime Self Test
Policy Self Test
Gate Self Test
Capability Boundary Test
Evidence Integrity Test
Recovery Test
Adversarial Anti-Pattern Test
```

任何核心 Governance 机制如果只能“解释自己工作了”，却不能以对抗性测试证明，均视为未充分验证。

---

# 38. 最终能力验收矩阵

## Governance

```text
✅ Rules
✅ Policies
✅ Hooks
✅ Gates
✅ Skill Routing
✅ Completion Enforcement
✅ Authority Model
✅ Change Management
```

## AgentOS

```text
✅ Identity
✅ State
✅ Context
✅ Lifecycle
✅ Capability
✅ Memory
✅ Evidence
✅ Team Runtime
✅ Recovery
✅ Authorization
```

## Engineering Intelligence

```text
✅ Dependency Discovery
✅ Business Boundary
✅ Architecture Analysis
✅ Risk Analysis
✅ Performance Analysis
✅ Verification Planning
✅ Contract Completeness
```

## Verification

```text
✅ Structural
✅ Behavioral
✅ Workflow
✅ Integration
✅ Regression
✅ Realism Policy
✅ Critical Path
✅ Fake Green Detection
```

## Team

```text
✅ Lead Agent
✅ Specialist Agents
✅ Dynamic Team Sizing
✅ Delegation
✅ DAG / Parallel Execution
✅ Ownership
✅ Conflict Resolution
✅ Governed Communication
✅ Team Evidence
```

## Real Engineering

```text
✅ Real Mutation
✅ Real Diff
✅ Real Build
✅ Real Test
✅ Failure Injection
✅ Failure Ownership
✅ Self Diagnosis
✅ Self Repair
✅ Reviewer
✅ Evidence Integrity
✅ Recovery
```

---

# 39. 不可接受的伪完成模式

以下任何一种出现，均应判定为 Governance Violation；在关键路径上直接造成 Gate Failure：

```text
“Build 太慢，所以跳过”
“测试太多，所以删掉”
“这是 legacy，所以不用分析”
“全部 Mock 即可”
“测试全绿，所以功能肯定正常”
“API 存在，所以功能完成”
“方法存在，所以事件实现了”
“写了 TODO，所以功能完成”
“注释解释了，所以功能完成”
“测试不稳定，所以 Skip”
“为了让测试通过修改 Assertion”
“只编译，不运行”
“只测试 Controller，不测试业务链”
“只测试返回值，不测试副作用”
“只测试新代码，不测试关联功能”
```

这些不是“实现取舍”，而是：

> **Engineering Governance Violations。**

---

# 40. 规范术语表（Glossary）

以下术语必须保持语义稳定：

| Term | Definition |
|---|---|
| Agent | 具有身份、状态、生命周期并能执行受控工作的运行时实体 |
| Expert | 具有特定工程领域智能的 Agent |
| Lead | 对 Team Decision 与协调负责的 Agent |
| Specialist | 在委派范围内执行专业任务的 Agent |
| Skill | Agent 的方法 / 专业行为能力描述，不等于可调用外部能力 |
| Capability | Runtime 授权下可以执行的能力单元 |
| Tool | Capability 的实际执行器之一 |
| Provider | Capability / Memory 等抽象的具体实现来源 |
| Policy | 可执行的治理规则组合 |
| Rule | 业务或工程治理规则本身 |
| Hook | 动作边界上的实时 Enforcement 点 |
| Gate | 阶段或完成状态的正式裁决点 |
| Task | 需要完成的工程目标 |
| Stage | Task 当前所处的工程阶段 |
| Operation | Task / Agent 对某个动作的一次执行实例 |
| Artifact | 被创建、修改、审查或证明的工程对象 |
| Evidence | 证明某项事实、动作或状态的可审计记录 |
| Result | 一次执行产生的结果 |
| Finding | 发现的问题、风险或观察结果 |
| Risk | 对系统正确性、完整性、安全性等产生影响的不确定性 |
| Contract | 必须保持或验证的显式工程行为边界 |
| Baseline | 变更前、可追溯、可复现的参考状态 |
| Completion | 由 Runtime + Evidence + Gate 正式裁定的完成状态 |

关键不等式必须长期保持：

```text
Skill ≠ Capability
Tool ≠ Capability
Policy ≠ Rule
Gate ≠ Hook
Evidence ≠ Artifact
```

---

# 41. Architecturally Deferred Decisions

以下内容目前明确不升级为不可变架构铁律：

```text
50ms Policy Timeout
200 Tests
10 Minutes Verification Limit
>5 Callers
90% Contract Coverage
Automatic Git Rollback
Channel<T>
SQLite
固定 Branch
固定 Workspace
固定 .NET minor version
固定 Specialist 通信拓扑
```

它们可以作为某一阶段的实现策略、Benchmark 参数、配置项或试验假设，但不得因为实现方便而升格为不可变工程原则。

核心原则：

> **数字是实现工具，不能取代工程判断。**

---

# 42. 后续 Design Specification 的强制约束

从本版本开始，任何新的 Design Specification 都必须回答：

```text
1. 它属于哪个 Phase？
2. 它遵守哪个 Architecture Boundary？
3. 它改变了哪些 Contract？
4. 它需要哪些 Policy / Hook / Gate？
5. 它需要哪些 State Transition？
6. 它需要哪些 Capability？
7. 它需要哪些 Cognitive Evidence？
8. 它需要哪些 Verification Evidence？
9. 它如何防止 Fake Green？
10. 它如何 Recovery？
11. 它如何证明 Real Engineering？
12. 它的 Completion Criteria 是什么？
```

如果 Design Spec 无法回答其中关键问题，不得进入正式实施。

---

# 43. 后续 Implementation Plan 的强制格式

任何实施计划必须明确：

```text
Baseline
Scope
Non-Goals
Artifacts
Contracts
States
Policies
Capabilities
Mutation Boundary
Tests
Failure Cases
Evidence
Reviewer
Gate
Rollback / Recovery
Completion Criteria
```

禁止只给：

```text
修改文件 A
新增类 B
运行测试 C
```

然后宣称 Phase 完成。

---

# 44. 未来每次工程工作轮的最小闭环

每个工作轮至少追求一次真实闭环：

```text
Discovery
→ Contract
→ Implementation
→ Build
→ Test
→ Self Evaluation
→ Self Repair
→ Reviewer
→ Evidence
```

其中“Self Repair”只有在确有失败、风险或需要修复时才发生，但 Self Evaluation、Evidence 与 Review 不得因“看起来很简单”而自动消失。

---

# 45. 第一原则：最小充分闭环，而不是最小功能

这是整个项目最需要长期锁死的语义。

```text
最小闭环 ≠ 最小功能
```

正确含义是：

> 在不删除目标核心能力、不跳过关键工程步骤、不降低真实验证标准的前提下，用最小充分范围完成真实端到端闭环。

因此：

```text
允许缩小无关范围
允许控制 Scope
允许 Deferred Finding
允许动态团队规模
允许预算管理
```

但：

```text
不允许删除目标核心能力
不允许跳过真实 Build
不允许跳过关键验证
不允许用 Mock 替代必须真实执行的链路
不允许用绿色测试掩盖缺失能力
```

---

# 46. 最终工程哲学

AI Engineering OS 不追求：

```text
更多 Agent
更多 Skill
更多 MCP
更多测试
更多规则
更多数字
```

而追求：

```text
明确架构边界
+
形式化状态
+
可执行 Policy
+
可验证 Cognition
+
智能 Verification
+
受控 Multi-Agent
+
真实 Engineering
+
完整 Evidence
+
可靠 Recovery
```

最终实现：

```text
弱 Agent
 ↓
认知脚手架
 ↓
不容易漏分析
 ↓
不容易假绿
 ↓
不能逃避失败
 ↓
必须真实验证
 ↓
可以自我修复

强 Agent
 ↓
获得更大自主空间
 ↓
主动搜索
 ↓
主动发现
 ↓
主动优化
 ↓
主动修复
 ↓
主动报告新风险
 ↓
仍然接受 Governance / Evidence / Gate
```

---

# 47. Chief Architect Final Acceptance Principle

AI Engineering OS 成功的唯一高阶判断标准，不是：

```text
代码数量
Skill 数量
Agent 数量
测试数量
```

而是：

> **真实 Agent Team 是否能够在统一治理下，对真实企业级代码完成真实工程修改，并通过真实 Build、真实 Test、真实 Failure Recovery、真实 Review，最终交付没有功能缩水且有完整证据链的结果。**

如果答案是：

```text
YES
```

则 AI Engineering OS 成立。

如果最终仍然只是：

```text
Prompt
→ Generate Code
→ Mock Test
→ Green
→ Report Success
```

则 AI Engineering OS 未成立。

---

# 48. FINAL AUTHORITATIVE EXECUTION RULE

从本版本生效起：

```text
本文件 = AI Engineering OS 上位执行契约
```

后续所有：

```text
Design Specification
Implementation Plan
Phase Plan
Skill
Workflow
Test Plan
Gate
Review
Completion Report
```

必须服从本文件。

若后续发现本文件存在真正的架构缺陷：

```text
发现问题
 ↓
形成 Change Record
 ↓
证据化验证
 ↓
Architectural Review
 ↓
批准新版本
 ↓
明确迁移 / 兼容策略
 ↓
更新 Baseline
```

不得以“施工方便”为理由静默偏离。

---

# FINAL ROADMAP

```text
CONTROL PLANE
      ↓
PHASE 0 — Baseline & Contract Freeze
      ↓
PHASE 1 — Policy / Gate / Hook
      ↓
PHASE 2 — AgentOS Governance / Capability Boundary
      ↓
PHASE 3 — Engineering Cognition
      ↓
PHASE 4 — Intelligent Verification
      ↓
PHASE 5 — Multi-Agent Team Runtime
      ↓
PHASE 6 — Real JNPF Engineering Pilot
      ↓
PHASE 7 — Production Hardening & Benchmark
      ↓
AI ENGINEERING OS
```

> **执行原则：先证明架构，再冻结接口；先建立真实闭环，再扩展复杂度；先通过 Gate，再进入下一阶段。**

---

## Source Basis

本文档基于以下两份用户提供的原始材料完成归并：

1. 《构建智能体操作系统1.0.md》
2. 《构建智能体操作系统1.1.md》

本版本的新增组织方式主要体现在：权威层级、状态三分模型、Resilience / Recovery、Policy Semantics、Artifact Mutation Boundary、Change Management、Self-Verification、后续 Design Spec / Implementation Plan 的强制映射关系，以及“架构合同与实现策略分离”的统一规则。
