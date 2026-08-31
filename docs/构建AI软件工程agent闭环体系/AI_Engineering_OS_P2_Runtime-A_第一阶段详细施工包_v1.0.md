# AI Engineering OS — P2 / Runtime-A 第一阶段详细施工包 v1.0

> **文档性质：AUTHORITATIVE / EXECUTION PACKAGE**
>
> **当前唯一授权施工阶段：P2 / Runtime-A — AgentOS Runtime Kernel**
>
> 本文件是《AI Engineering OS 完整系统分阶段总体实施计划 v1.0》在当前阶段的下位详细施工包。任何 AI 工程师、Reviewer、测试工程师、实现 Agent，均必须以本文件作为 Runtime-A 的直接执行依据。
>
> 本文件不得改变上位 v2.1-FDE 规格和 Master Implementation Plan 的目标、边界、Gate 或完成定义。任何冲突必须停止在当前边界内，形成 Change Record，禁止静默改线。

---

# 0. Chief Architect 开工裁决

## 0.1 当前阶段只有一个目标

本阶段不是开发智能体“智慧”，不是开发 Cognition Engine，不是开发 Verification Engine，不是开发 Multi-Agent Team，也不是开发 FDE。

本阶段唯一目标是：

> **把已经冻结的 AgentOS Runtime 架构第一次变成真实、可运行、可测试、可验证的 Runtime Kernel。**

上位 Master Plan 已明确当前唯一授权工作为：

```text
P2 / Runtime-A
        ↓
D1-D8 Decision Closure
        ↓
Runtime Kernel Implementation
        ↓
Gate-A
```

并明确禁止在 Runtime-A 中提前建设 P3–P9 能力。fileciteturn9file4L770-L796

## 0.2 当前真实工程站位

```text
P0 Governance Foundation                  ✅ PASS
P1 Control Plane Vertical Slice           ✅ PASS
P2 Runtime Architecture                   ✅ FROZEN
P2 / Runtime-A                            🟡 CURRENT
P2 / Runtime-B~H                          ⏸ NOT AUTHORIZED
P3 Cognition                              ⛔ NOT AUTHORIZED
P4 Verification                           ⛔ NOT AUTHORIZED
P5 Team Runtime                            ⛔ NOT AUTHORIZED
P6 System Design                           ⛔ NOT AUTHORIZED
P7 UI / UX                                ⛔ NOT AUTHORIZED
P8 FDE                                     ⛔ NOT AUTHORIZED
P9 Hardening                               ⛔ NOT AUTHORIZED
```

Master Plan 明确指出：当前工程报告中的“Phase 1 Runtime Kernel”实际上属于 **P2 AgentOS Runtime 的 Runtime Phase A**，不能解释为重新开始 P1。fileciteturn9file7L1226-L1230

## 0.3 本阶段的最终产出不是“几个 C# 类”

Runtime-A 必须最终形成：

```text
可加载的 Runtime
+
真实 Agent Identity
+
真实 Task Identity
+
合法 Lifecycle
+
Authoritative State Boundary
+
Core Execution Loop
+
Runtime Test Suite
+
实际 Build Result
+
实际 Test Result
+
Gate-A Evidence
+
Independent Review
```

“代码存在”不是完成；“测试代码存在”也不是完成。只有真实执行、结果、证据、Review 和 Gate 全部成立，才能升级状态。

---

# 1. 上位约束

## 1.1 权威链

```text
Human / Chief Architect
        ↓
AI Engineering OS v2.1-FDE
        ↓
AI Engineering OS Master Implementation Plan v1.0
        ↓
P2 / Runtime-A Active Contract
        ↓
Runtime Architecture Specification
        ↓
Approved Runtime-A Implementation Plan
        ↓
This Execution Package
        ↓
Task Bundles
        ↓
Code / Tests / Evidence
        ↓
Gate-A
```

下位文件不能覆盖上位文件。

## 1.2 本阶段必须遵守的核心铁律

### IRON-R1：Runtime 必须是通用 Runtime

Runtime Core 不得知道：

```text
JNPF
FlowCommentService
类级重构策略
表级重构策略
业务流程
具体行业知识
```

Runtime 只负责：

```text
Identity
State
Lifecycle
Execution
Capability Boundary
Policy Boundary
Security Boundary
Evidence Boundary
Recovery Boundary
Coordination Foundation
```

这直接承接上位规格对“Runtime Must Not Own Domain Intelligence”的要求。fileciteturn7file3L273-L289

### IRON-R2：Expert 不得绕过 Runtime

所有工程能力必须遵循：

```text
Agent
 ↓
Runtime
 ↓
Capability Authorization
 ↓
Capability
 ↓
Tool / Provider / MCP
```

不得出现：

```text
Agent → File API
Agent → Shell
Agent → Build
Agent → Test
Agent → Git
Agent → MCP
```

绕过 Runtime 的直接调用。fileciteturn7file3L865-L881

### IRON-R3：不得以绿色为目标反向删能力

禁止：

```text
删除 Runtime 能力
删除测试
降低 Assertion
Skip 关键测试
用 Mock 代替必须真实执行的能力
用 Stub 吞掉 Unexpected Call
用硬编码结果制造 PASS
```

这些行为属于 Governance Violation，而不是正常工程取舍。fileciteturn7file8L883-L904

### IRON-R4：Completion 不能自我宣布

Agent 无权自行设置：

```text
Completed = true
```

必须通过：

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

才允许进入 Completed。fileciteturn7file8L922-L938

### IRON-R5：规格可执行，但实现不能被架构锁死

不得因为想“明确”而提前锁死：

```text
某个具体锁实现
某个具体线程模型
某个具体数据库
某个具体 Channel 实现
某个固定 timeout 数字
某个固定测试数量
```

架构合同规定“必须具有什么性质”；实现方式由本阶段设计审查决定。

---

# 2. Runtime-A 范围定义

## 2.1 In Scope

Runtime-A 允许实施：

```text
A. Runtime Project Skeleton
B. Agent Identity
C. Task Identity
D. Lifecycle Kernel
E. State Authority Boundary
F. Core Runtime Loop
G. Runtime Invocation Boundary
H. 基础 Runtime Error Model
I. Gate-A 测试基础设施
J. Runtime-A Evidence
K. Runtime-A Observability 基础钩子（仅为证明 Kernel 行为）
```

## 2.2 Out of Scope

Runtime-A 明确禁止实现：

```text
❌ Engineering Cognition Engine
❌ Verification Intelligence Engine
❌ Test Selector
❌ Fake Green Detector
❌ Multi-Agent Team Scheduler
❌ DAG Team Runtime
❌ Dynamic Team Sizing
❌ System Analysis Agent
❌ Product Manager Agent
❌ Domain Design Agent
❌ UI / UX Agent
❌ Frontend Agent
❌ FDE Team
❌ 类级重构 Agent Wrapper
❌ 表级重构 Agent Wrapper
❌ JNPF 专用 Runtime Logic
❌ 任何“顺便优化”型大规模重构
```

Master Plan 对 Runtime-A 的边界已经明确：只做 Runtime Kernel、Identity、Task Identity、Lifecycle、State Boundary、Core Loop、Gate-A 和 Runtime 测试/证据。fileciteturn8file1L241-L288

## 2.3 允许的最小依赖

可以依赖：

```text
已有基础框架 / SDK
已有 Control Plane 契约
已有 Harness Governance
已有已批准的通用测试基础设施
```

不能为了“测试方便”新建一套与真实 Runtime 平行的伪 Runtime。

---

# 3. Runtime-A 的成功定义

## 3.1 Kernel Definition of Done

Runtime-A 只有在以下全部成立时才算完成：

```text
[ ] Runtime 程序可构建
[ ] Runtime 核心组件可实例化
[ ] Agent Identity 可建立
[ ] Task Identity 可建立
[ ] Lifecycle 合法转换可执行
[ ] 非法 Lifecycle 转换被拒绝
[ ] Runtime 核心 Loop 可启动、推进、终止
[ ] Runtime 状态边界唯一且可追踪
[ ] Runtime 不依赖领域 Expert
[ ] Runtime 不包含 JNPF / 类重构业务逻辑
[ ] Runtime 行为可通过真实自动化测试复现
[ ] Gate-A A1-A5 全部真实通过
[ ] dotnet build 真实成功
[ ] dotnet test 真实执行
[ ] 无关键测试以 Skip 方式获得绿色
[ ] 所有结果来自实际执行而非预写 Evidence
[ ] Independent Review PASS
[ ] Evidence 完整
```

## 3.2 不属于完成证明的内容

以下任何单项均不能作为完成证明：

```text
代码文件已经创建
接口已经定义
项目可以打开
测试类已经创建
README 写好了
Evidence 文档写好了
Agent 自己报告 PASS
编译器某个局部项目成功
单元测试全部 Mock 通过
```

---

# 4. 开工前 Gate — P2A-Entry

Runtime-A 开工前首先执行 Entry Audit。

## 4.1 必须回读的文件

AI 工程师第一动作必须重新读取：

```text
1. AI_Engineering_OS_最终权威执行版_v2.1_FDE.md
2. AI_Engineering_OS_完整系统分阶段总体实施计划_v1.0.md
3. P2 Context
4. P2 Checklist
5. P2 Gate Contract
6. Section8 Runtime Architecture Specification
7. Runtime Design / Implementation Proposal
8. D1-D8 当前 Decision Record（如已建立）
```

Master Plan 已强制规定 Runtime-A 开工前必须完成这组回读。fileciteturn9file1L289-L336

## 4.2 Entry Audit 必须回答

```text
当前 Phase 是什么？
当前 Subphase 是什么？
当前 Gate 是什么？
本阶段唯一目标是什么？
本阶段禁止做什么？
之前已经验证了什么？
之前没有验证什么？
本阶段依赖哪些既有资产？
本阶段产生哪些新资产？
```

## 4.3 Entry Audit 结果

只允许：

```text
AUTHORIZED
BLOCKED
```

禁止使用模糊状态：

```text
差不多可以开始
基本完成
大部分就绪
```

若发现上位文件冲突，必须 `BLOCKED + Change Record`。

---

# 5. Workstream 总览

Runtime-A 分成 8 个施工 Workstream，但它们不是允许无限并行的 8 个独立任务；必须按依赖推进。

```text
RA-00 Baseline / Decision Closure
        ↓
RA-01 Repository / Project Skeleton
        ↓
RA-02 Identity
        ↓
RA-03 Lifecycle
        ↓
RA-04 State Authority
        ↓
RA-05 Core Runtime Loop
        ↓
RA-06 Integration Tests / Gate-A
        ↓
RA-07 Final Evidence / Review / Closure
```

其中：

```text
RA-02 Identity
RA-03 Lifecycle
RA-04 State Authority
```

可以在设计层面并行分析，但存在代码依赖时仍必须以真实依赖顺序落地。

---

# 6. RA-00 — Baseline / D1-D8 Decision Closure

## 6.1 目标

在任何 Runtime 代码修改前，把实施 Proposal 中未决的 D1-D8 变成正式、可追踪的工程决策。

当前报告已明确：Implementation Proposal 存在 8 个技术决策点 D1-D8，当前需先完成 Decision Closure，再进入 Runtime-A。Master Plan 也明确将 D1-D8 放在 Runtime-A Kernel 之前。fileciteturn8file1L241-L288

## 6.2 D1-D8 的处理规则

本施工包不重新发明 D1-D8 的编号和内容。

AI 工程师必须：

```text
读取原 Proposal
 ↓
逐项复制原始 D1-D8 标题/问题
 ↓
列出 Proposed Decision
 ↓
记录 Alternatives
 ↓
记录 Architectural Impact
 ↓
记录 Implementation Impact
 ↓
记录 Test Impact
 ↓
记录 Migration / Compatibility Impact
 ↓
记录 Rejection Reason（被否决方案）
 ↓
形成 Decision Record
```

## 6.3 已知必须重点核对的决策领域

根据当前工程报告，至少必须重点审查：

```text
多 Project 分层
.NET DI / Composition Root 策略
按层划分 Runtime 职责
Adapter 独立性
```

除此之外，严格以原 Proposal 的 D1-D8 为准，不得自行新增“D9/D10”并改变决策集。

## 6.4 Decision Record 最低格式

```text
Decision ID:
Question:
Context:
Decision:
Alternatives:
Rejected Alternatives:
Architectural Rationale:
Runtime Impact:
Test Impact:
Compatibility Impact:
Known Risks:
Validation Method:
Status:
```

## 6.5 RA-00 Exit

必须满足：

```text
[ ] D1-D8 均有明确状态
[ ] 每个 Decision 均有依据
[ ] 无未解决关键矛盾
[ ] 无未批准技术选择被写入代码
[ ] Decision Record 可追溯
```

如果 D1-D8 任一项仍 `OPEN / UNKNOWN` 且可能影响 Runtime Kernel 架构：

```text
RA-00 = BLOCKED
```

不得直接编码。

---

# 7. RA-01 — Runtime Repository / Project Skeleton

## 7.1 目标

建立最小但真实的 Runtime 项目结构，为后续 Kernel 提供宿主边界。

## 7.2 施工原则

项目结构必须体现职责，而不是为了“看起来完整”创建十几个空项目。

只创建经 Architecture Decision 批准、当前 Kernel 真正需要的项目。

## 7.3 必查边界

```text
Runtime Core
Runtime Contracts
Runtime Infrastructure / Host（如经 D1-D8 批准）
Runtime Tests
```

具体 Project 数量由批准后的 D1-D8 决定，不在本施工包中擅自锁死。

## 7.4 依赖方向

最低必须满足：

```text
Runtime Core
    ↓
Runtime Contracts / Abstractions
```

或按已批准的真实架构执行，但必须禁止：

```text
Runtime Core → JNPF
Runtime Core → Class Refactor Logic
Runtime Core → Specific Expert
Runtime Core → UI
Runtime Core → Concrete MCP
```

## 7.5 Repository 验证

必须验证：

```text
[ ] solution / project load
[ ] reference graph
[ ] no forbidden reference
[ ] no circular project dependency
[ ] runtime test project can reference only approved surfaces
```

## 7.6 RA-01 Exit Evidence

```text
project-tree.txt
project-reference-graph.txt
architecture-boundary-check.json
initial-build-result.json
```

不得在没有真实 Build 的情况下宣布 RA-01 完成。

---

# 8. RA-02 — Agent Identity

## 8.1 目标

实现真实 Agent 身份对象及其最小生命周期关联，而不是简单的字符串 ID。

上位规格要求 Expert 具备 Identity、State、Lifecycle、Task、Skills、Context、Capability、Evidence。fileciteturn7file8L826-L843

Runtime-A 只负责把 Identity 作为 Kernel 级运行对象建立，不负责实现 Cognition/Skill Intelligence。

## 8.2 Identity 必须能够表达

```text
Agent ID
Agent Type / Role Identity
Creation Metadata
Lifecycle Association
Task Association（如已存在）
Runtime Instance Identity
```

具体字段必须从 Runtime Architecture Spec 推导，不允许自由发明大量未来字段。

## 8.3 Identity 不得承担

```text
业务规则
Prompt Intelligence
领域知识
具体项目知识
具体 Skill 内容
```

## 8.4 测试

至少证明：

```text
创建唯一 Agent Identity
同一 Identity 可被 Runtime 识别
非法 / 缺失 Identity 被拒绝
Identity 不会因为普通 Operation 被无意重建
```

## 8.5 RA-02 Exit

```text
Identity Unit Tests = PASS
Boundary Tests = PASS
No Domain Dependency = PASS
```

---

# 9. RA-03 — Lifecycle Kernel

## 9.1 目标

实现 Runtime 可控制、可验证的 Agent / Task 生命周期基础。

注意：生命周期必须是正式 Runtime Contract，不是简单状态字符串。

上位 v2.1 已将状态形式化作为 AgentOS 必须能力，并要求 Runtime 知道“谁可以改变、为什么可以改变、改变后的条件以及失败处理”。fileciteturn2file1L1079-L1121

## 9.2 状态分层要求

不得把：

```text
Task State
Execution Stage
Operation State
```

揉成一个超级枚举。

必须保持三层语义：

```text
Task State
Execution Stage
Operation State
```

Master v2.1 对此已经明确。fileciteturn2file1L1162-L1235

## 9.3 Runtime-A 的重点

Runtime-A 不需要完成全部未来 Stage 的业务语义，但必须建立可扩展的状态承载与合法转换机制。

至少需要证明：

```text
合法转换 → ACCEPT
非法转换 → REJECT
未知转换 → FAIL CLOSED
```

## 9.4 状态变更并发约束

要求：

> **State transition 从 State Authority 视角必须是 atomic / linearizable。**

具体实现可以是：

```text
lock
CAS
transaction
actor
single-threaded authority
```

但这些不能在未评估前被写成架构铁律。v2.1 已明确区分架构性质与具体实现技术。fileciteturn2file1L1274-L1304

## 9.5 Lifecycle Adversarial Tests

必须构造：

```text
合法 forward transition
非法 backward transition（若当前 Contract 禁止）
非法跨阶段 transition
并发 transition race
重复 transition
Transition after terminal state
Unknown state / invalid state
```

所有非法转换必须被 Runtime 拒绝。

---

# 10. RA-04 — Authoritative State Boundary

## 10.1 目标

建立 Runtime 内唯一可信的 State Authority。

不能出现：

```text
Agent 自己一份 State
Task 自己一份 State
Memory 一份 State
UI 一份 State
```

然后互相冲突。

## 10.2 State Authority 必须回答

```text
当前状态是什么？
谁修改过？
何时修改？
为什么允许修改？
修改前是什么？
修改后是什么？
如果冲突怎么办？
```

## 10.3 最小状态转换证据

每次 authoritative transition 至少应能关联：

```text
Task ID
Agent ID
Previous State
Requested Transition
Resulting State
Policy / Rule Context（若已有）
Timestamp
Outcome
```

## 10.4 重要禁止项

不得让：

```text
UI
Prompt
Skill
Memory Provider
External MCP
```

成为 State Authority。

---

# 11. RA-05 — Core Runtime Loop

## 11.1 目标

建立最小真实 Runtime Loop：

```text
Create / Load Task
      ↓
Resolve Agent
      ↓
Validate Lifecycle
      ↓
Execute Runtime Operation
      ↓
Update Authoritative State
      ↓
Produce Runtime Result
      ↓
Terminate / Continue
```

这不是 Cognition Loop。

它是 Runtime Kernel Loop。

## 11.2 Kernel Loop 的职责

Kernel Loop 只负责：

```text
接收合法 Runtime Operation
验证前置状态
验证 Runtime 边界
执行 Kernel Operation
更新状态
返回结果
```

## 11.3 Kernel Loop 不负责

```text
分析业务
设计架构
选择重构策略
判断 UI
生成测试策略
规划 Multi-Agent Team
```

这些都是后续 Engineering Intelligence / FDE 能力。

## 11.4 Failure Semantics

Operation 失败时必须能表达：

```text
Failed
TimedOut
Cancelled
Recovered
```

具体可用状态以 Runtime Architecture Spec 为准。

不得出现：

```text
Exception swallowed → Success
Unexpected Call → default
Failure → silently ignored
```

---

# 12. RA-06 — Gate-A Verification

这是 Runtime-A 的核心施工验收 Workstream。

当前已有 Phase-A Start Report 规划 Gate-A A1-A5；本施工包要求严格以该批准 Gate Contract 的具体定义为准，不得重新解释 Gate-A。

## 12.1 Gate-A 总原则

```text
Gate-A PASS
≠
“项目结构存在”
```

而必须证明：

```text
真实 Runtime Kernel 行为成立
```

## 12.2 A1-A5 执行规则

AI 工程师必须从现有 Phase-A Start Report 和 Gate Contract 中逐项提取：

```text
A1 名称 + Contract + Test
A2 名称 + Contract + Test
A3 名称 + Contract + Test
A4 名称 + Contract + Test
A5 名称 + Contract + Test
```

**不得凭记忆改写 A1-A5。**

每项必须具备：

```text
Requirement
Test Fixture
Execution Command
Observed Result
Evidence
Pass/Fail
```

## 12.3 Gate-A 必须是执行型 Gate

禁止：

```text
planned PASS
expected PASS
static review only
source inspection only
mock-only verification
```

## 12.4 最低真实验证要求

在具体 Gate Contract 允许范围内，至少要验证：

```text
Kernel can start
Kernel can create / identify Agent
Kernel can create / identify Task
Valid lifecycle path works
Invalid lifecycle path is blocked
Runtime boundary is enforced
```

## 12.5 Gate-A Failure Handling

任意 A1-A5 FAIL：

```text
Gate-A = FAIL
P2 / Runtime-A remains OPEN
```

不得：

```text
删掉测试
修改 Assertion
降低 Gate 条件
将失败改名为 Warning
用 Mock 代替真实路径
直接进入 Runtime-B
```

---

# 13. RA-07 — Failure / Repair / Evidence Closure

## 13.1 失败必须分类

任何重要失败必须分类：

```text
PRE_EXISTING
CURRENT_CHANGE
TRANSITIVE_IMPACT
UNKNOWN
```

上位工程闭环已经要求这样处理。fileciteturn3file0L899-L927

## 13.2 重复失败规则

不得简单使用：

```text
失败三次 → Human
```

而使用：

```text
Same Failure Signature
+
No State Progress
→
Escalation
```

这是 v2.1 已锁定的阻塞语义。fileciteturn5file1L1029-L1047

## 13.3 Runtime-A Recovery

Runtime-A 至少要验证 Runtime 自身出现：

```text
Operation failure
Runtime restart
Test process failure
```

时不会无声产生：

```text
State = Success
Actual = Failure
```

完整 Resilience 会在后续 Runtime 子阶段继续展开，本阶段只实现 Gate-A 所需的最小 Kernel 可靠性。

## 13.4 Evidence

所有关键 Runtime 操作至少必须能关联：

```text
Task ID
Agent ID
Phase
Operation
Input
Output
Timestamp
Result
Evidence
```

这是统一 Evidence Ledger 的核心要求。fileciteturn3file1L1189-L1224

---

# 14. RA-08 — Final Review / Completion

## 14.1 Self Evaluation

AI 工程师必须在声明 Gate-A 前自行回答：

```text
本阶段是否仍只做 Runtime-A？
是否产生了任何 P3+ 代码？
是否改变 Frozen Contract？
是否新增未批准依赖？
是否有 Skip？
是否有 Mock 代替真实 Kernel 行为？
是否所有测试真的执行？
是否 Build 真的执行？
是否 Evidence 可从实际命令追溯？
是否所有失败都有归属？
```

## 14.2 Independent Review

Reviewer 必须独立于实施 Agent 的最终判断，至少检查：

```text
Architecture Boundary
Project Dependencies
Identity
Lifecycle
State Authority
Runtime Loop
Error Semantics
Test Integrity
Real Build
Real Test
Evidence Integrity
Scope Creep
```

Reviewer 不能只检查“方法存在”。

## 14.3 Completion 条件

必须同时：

```text
Implementation = PASS
Build = PASS
Tests = PASS
Gate-A = PASS
Review = PASS
Evidence = PASS
Scope = CLEAN
```

任一不是 PASS：

```text
Runtime-A = OPEN
```

---

# 15. Runtime-A 测试策略

## 15.1 测试分层

至少建立：

```text
Unit Tests
Contract Tests
Boundary Tests
Lifecycle Tests
State Authority Tests
Runtime Kernel Integration Tests
Gate-A Tests
```

具体数量不设硬数字。

## 15.2 测试真实性原则

测试目标应优先验证真实 Runtime 组件，而不是：

```text
Mock Runtime
Fake State Authority
Fake Lifecycle
```

Mock 可以用于隔离真正不属于 Kernel 的外部依赖，但不能把 Kernel 自己 Mock 掉。

## 15.3 测试必须能证明“失败会失败”

必须至少有负路径：

```text
Invalid Identity
Invalid Transition
Unauthorized Operation
Unexpected State
Failure Path
```

这些不能因为“测试难写”被删掉。

## 15.4 Test Integrity

必须额外检查：

```text
Test Deleted?
Test Skipped?
Assertion Weakened?
Expected Exception removed?
Fixture bypassed?
Mock boundary widened?
```

Fake Green 是治理重点，上位系统要求其最终成为自动化反作弊资产。fileciteturn5file1L1125-L1167

---

# 16. Runtime-A 实际执行顺序

禁止一次性创建所有代码后最后才测试。

采用以下循环：

```text
RA-00 Decision Closure
        ↓
RA-01 Skeleton
        ↓
REAL BUILD
        ↓
RA-02 Identity
        ↓
UNIT TEST
        ↓
REAL BUILD
        ↓
RA-03 Lifecycle
        ↓
TEST + INVALID PATH
        ↓
REAL BUILD
        ↓
RA-04 State Authority
        ↓
CONCURRENCY / BOUNDARY TEST
        ↓
REAL BUILD
        ↓
RA-05 Core Runtime Loop
        ↓
INTEGRATION TEST
        ↓
REAL BUILD
        ↓
RA-06 Gate-A A1-A5
        ↓
REAL TEST
        ↓
RA-07 Failure / Evidence
        ↓
INTEGRITY TEST
        ↓
RA-08 REVIEW
        ↓
FINAL BUILD
        ↓
FINAL TEST
        ↓
GATE-A DECISION
```

这样每一轮都有局部闭环，不允许“开发八轮后再发现整个基础设计错了”。

---

# 17. 每一轮 AI 工程师必须执行的上下文防漂移协议

这是本施工包的强制部分。

## 17.1 Round Start

每轮开始首先输出内部执行上下文：

```text
CURRENT PROGRAM PHASE = P2
CURRENT SUBPHASE = Runtime-A
CURRENT WORKSTREAM = RA-XX
CURRENT GATE = Gate-A / Entry / Local Gate
CURRENT OBJECTIVE = <唯一目标>
COMPLETED = <已验证完成>
NOT COMPLETED = <未完成>
FORBIDDEN = <当前禁止事项>
LAST VERIFIED BASELINE = <基线>
NEXT AUTHORIZED ACTION = <下一步>
```

Master Plan 已明确要求每轮重新读取 Current Phase、Current Gate、Outstanding Items，并保持防上下文漂移。fileciteturn8file1L319-L345

## 17.2 Round End

必须更新：

```text
Completed
Not Completed
Actual Evidence
Failures
Repairs
Open Items
Decision Changes
Next Authorized Step
```

## 17.3 任何 AI 工程师不得自行宣布

```text
Phase Completed
进入 Runtime-B
开始 Cognition
开始 Team
```

除非对应 Gate 已 PASS。

---

# 18. Task Bundle 标准格式

Runtime-A 的每一个具体执行 Task 必须具有：

```text
Task ID
Parent Workstream
Objective
Scope
Dependencies
Allowed Files / Projects
Forbidden Files / Projects
Required Evidence
Required Tests
Acceptance Criteria
Rollback / Recovery Strategy
Expected Output
Current Gate Impact
```

## 18.1 Task 必须有“Forbidden Scope”

例如：

```text
Allowed:
backend/modules/mod-runtime/**
Runtime tests
Runtime docs/evidence

Forbidden:
P3 cognition
P4 verification intelligence
P5 team runtime
P6 system design
P7 UI
P8 FDE
```

不得只有“要做什么”，还必须明确“不能做什么”。

---

# 19. Mutation Boundary

任何代码 Mutation 必须绑定：

```text
Task
Agent
Workspace / Mutation Boundary
Artifact
Before Snapshot
After Snapshot
Approval / Authorization
Evidence
```

这与 Master Plan 的统一 Mutation Boundary 要求一致。fileciteturn9file3L645-L658

## 19.1 Runtime-A 修改前

至少保存：

```text
Git Status
Relevant Commit / Baseline
Project Tree
Affected File Hash / Blob Identity（如现有机制已采用）
```

## 19.2 修改后

必须形成：

```text
Actual Diff
Build Result
Test Result
Evidence
```

不能把“计划中的 Diff”作为真实 Diff。

---

# 20. Dependency / Supply Chain 规则

新增 Runtime-A 依赖必须记录：

```text
Artifact
Version
Source
Integrity
Compatibility
Approval
```

适用范围不限于 NuGet，也包括未来可能进入 Runtime 的：

```text
MCP Server
Model Provider
Plugin
Container
CLI
Script
Skill Package
```

这是 Master Plan 对 AgentOS Supply Chain Governance 的统一要求。fileciteturn8file1L190-L221

任何新依赖如果没有批准记录：

```text
BLOCKED
```

---

# 21. Anti-Pattern Checklist

Runtime-A 每次 Review 都必须检查：

```text
[ ] 巨型 Runtime 类
[ ] Runtime 引用 Expert 具体实现
[ ] Runtime 引用 JNPF
[ ] Runtime 引用重构 Skill 内部知识
[ ] 外部工具绕 Runtime
[ ] Memory 直接修改 Runtime Authority
[ ] Agent 自己决定 Completed
[ ] State 没有唯一 Authority
[ ] Invalid transition 被吞掉
[ ] Exception 被吞掉
[ ] Unexpected Call 返回 default
[ ] Test Skip 换 Green
[ ] Mock 替代 Kernel 本体
[ ] Hard-coded PASS
[ ] 通过测试修改来适应错误实现
[ ] 为测试制造平行伪 Runtime
[ ] 偷偷引入 P3+ 能力
[ ] 未经 Change Record 修改 Frozen Contract
```

其中多数直接来自上位系统禁止的伪完成模式和治理边界。fileciteturn6file6L1125-L1151

---

# 22. Evidence Bundle 标准

Runtime-A 最终必须形成一个完整 Evidence Bundle。

## 22.1 Baseline Evidence

```text
Baseline Commit
Working Tree Status
Architecture Baseline
Decision Records
```

## 22.2 Implementation Evidence

```text
Project Creation
File Changes
Reference Changes
Identity Implementation
Lifecycle Implementation
State Authority Implementation
Kernel Loop Implementation
```

## 22.3 Build Evidence

```text
Exact Command
Working Directory
Timestamp
Exit Code
stdout/stderr location
Result
```

必须是实际执行结果。

## 22.4 Test Evidence

```text
Exact Command
Test Project / Scope
Test Count / Outcome
Skipped Tests
Failure Details
Final Result
```

任何 Skip 都必须明确分类；核心 Gate 测试不得用 Skip 取得 PASS。

## 22.5 Gate Evidence

```text
A1 Evidence
A2 Evidence
A3 Evidence
A4 Evidence
A5 Evidence
Gate Evaluation
```

## 22.6 Review Evidence

```text
Reviewer Identity
Review Scope
Findings
Severity
Resolution
Final Verdict
```

---

# 23. Gate-A 最终判定表

| 维度 | PASS 条件 | 失败处理 |
|---|---|---|
| Architecture | 无 Runtime → Domain 反向依赖 | FAIL |
| Identity | Agent/Task Identity 可真实建立、识别 | FAIL |
| Lifecycle | 合法路径可执行、非法路径被拒绝 | FAIL |
| State Authority | 唯一、可追踪、并发语义成立 | FAIL |
| Kernel Loop | 真实执行成功 | FAIL |
| Boundary | Runtime 绕过与越权被阻断 | FAIL |
| Build | 真实 Build 成功 | FAIL |
| Test | 关键 Runtime 测试真实执行 | FAIL |
| Evidence | 证据来自实际执行且可追溯 | FAIL |
| Review | Independent Review PASS | FAIL |
| Scope | 无 P3+ 偷跑、无越权修改 | FAIL |

最终：

```text
ALL PASS → Runtime-A = CLOSED / Gate-A = PASS
ANY FAIL → Runtime-A = OPEN / Gate-A = FAIL
```

---

# 24. Blocked / Failed / Deferred 语义

## PASS

所有硬性条件、真实验证、Evidence、Review 满足。

## FAIL

至少一个硬性条件未满足，或者发生 Iron Law 违规。

## BLOCKED

存在明确前置依赖未满足，当前不能继续。

## DEFERRED

经过正式 Change / Decision Record 批准推迟；不计入已完成能力。

这些状态与 Master Plan 的统一 Gate 语义一致。fileciteturn8file1L709-L732

---

# 25. 当前阶段明确禁止的“成功捷径”

以下任何行为出现，直接触发 Runtime-A Governance Failure：

```text
“Runtime 太复杂，先做一个简单假的”
“先把接口搭出来，后面再实现”
“测试先 Mock，真正实现以后再说”
“Build 太慢，所以先跳过”
“这项测试不稳定，所以 Skip”
“这个依赖以后再补，现在直接引用”
“为了 Gate-A 先写死返回值”
“先开发 Cognition，Runtime 以后补”
“先做 Multi-Agent，Runtime 以后接”
“先做 UI，Runtime 不影响”
“先写 Universal Agent Wrapper，Runtime 用假对象”
“Evidence 先写模板，最后统一补 PASS”
```

这些不是施工技巧，而是本工程明确禁止的伪完成模式。

---

# 26. 与现有 Class Refactor Expert 的关系

## 26.1 当前定位

现有：

```text
generic-class-refactor-expert v6.0
```

是已经可用的 Specialist Capability。

但：

```text
Skill Capability        ✅
Universal Agent         ❌
AgentOS Runtime         ❌
Runtime Integration     ❌
```

Master Plan 已明确要求继续保留 Skill 资产，并在 Runtime 完成后再通过统一 Runtime 接入。fileciteturn8file1L641-L673

## 26.2 Runtime-A 禁止做的事情

不要因为已有 Skill 很成熟，就在 Runtime-A 中：

```text
把 Skill 直接包成 Agent
把 Skill 直接当 Runtime
把 Skill Knowledge 塞进 Runtime Core
把 JNPF Profile 注入 Runtime Core
```

## 26.3 正确顺序

```text
Runtime-A~H
   ↓
Universal Agent Runtime
   ↓
Agent Contract
   ↓
Class Refactor Expert Adapter
   ↓
Real Pilot
```

---

# 27. Completion Report 标准

Runtime-A 最终报告必须包含以下章节：

```text
1. Executive Status
2. Current Phase / Subphase
3. Baseline
4. D1-D8 Decision Status
5. Workstream Completion Matrix
6. Changed Files
7. Architecture Boundary Verification
8. Runtime Behavior Verification
9. Build Execution Result
10. Test Execution Result
11. Gate-A A1-A5 Result
12. Failures
13. Repairs
14. Deferred Items
15. Evidence Index
16. Independent Review
17. Final Gate Verdict
18. Next Authorized Step
```

## 27.1 绝对禁止的结论写法

禁止：

```text
“基本完成”
“代码已经差不多”
“测试预计会通过”
“剩余三个 Skip 没关系”
“功能以后补”
“可以先进入下一阶段”
```

必须写成：

```text
PASS
FAIL
BLOCKED
DEFERRED
```

并提供 Evidence。

---

# 28. 本阶段最终输出目录建议

建议 Runtime-A 最终形成结构：

```text
backend/modules/mod-runtime/

    <approved-runtime-projects>/

runtime-tests/
    Gate-A/
    Contract/
    Lifecycle/
    State/
    Kernel/

 docs/architecture/
    runtime-a-context.md
    runtime-a-decisions.md
    runtime-a-architecture-verification.md

 docs/superpowers/plans/
    P2-Runtime-A-Implementation-Plan.md

 evidence/
    P2-runtime-a/
        baseline/
        decisions/
        build/
        tests/
        gate-a/
        review/
        final/

 .ai/
    phase-state/
        P2-Runtime-A-CONTEXT.md
        P2-Runtime-A-CHECKLIST.md
        P2-Runtime-A-GATE.md
```

具体目录名称必须与现有 Repository / Control Plane 约定保持一致；本目录结构仅作为职责模板，不得脱离真实仓库规则强行创建。

---

# 29. AI 工程师实际执行协议

## 开始

```text
1. 读取上位 v2.1-FDE
2. 读取 Master Plan
3. 读取 P2 Context
4. 读取 P2 Checklist
5. 读取 P2 Gate
6. 读取 Runtime Architecture Spec
7. 读取 Implementation Proposal
8. 读取 D1-D8
9. 输出 Entry Audit
10. 仅在 AUTHORIZED 后施工
```

## 每轮

```text
Baseline
 ↓
Task Bundle
 ↓
Implementation
 ↓
Real Build
 ↓
Real Test
 ↓
Self Evaluation
 ↓
Self Repair
 ↓
Reviewer
 ↓
Evidence
 ↓
Local Gate
```

## 完成一个 Workstream

```text
Workstream Evidence
+
Workstream Review
+
Workstream Acceptance
```

然后才进入下一 Workstream。

## 完成 Runtime-A

```text
All Workstreams PASS
+
Gate-A A1-A5 PASS
+
Final Build PASS
+
Final Test PASS
+
Independent Review PASS
+
Evidence Integrity PASS
```

才可以提交 Phase Closure。

---

# 30. Runtime-A 最终禁止跨阶段条件

以下任一行为都表示当前工程师已经偏离施工路线：

```text
P3 code created
P4 code created
P5 scheduler created
P6 system design code created
P7 UI code created
P8 FDE Team code created
Universal Agent full wrapper implemented before Runtime-A closure
```

一旦发生：

```text
STOP
↓
Record Scope Violation
↓
Review whether changes must be reverted
↓
Restore Runtime-A boundary
↓
Continue only after Governance decision
```

---

# 31. Runtime-A 与后续阶段的接口关系

Runtime-A 完成后，不是直接进入“智能 Agent”。

必须继续：

```text
Gate-A PASS
   ↓
Runtime-B State / Context
   ↓
Gate-B
   ↓
Runtime-C Execution
   ↓
Gate-C
   ↓
Runtime-D Evidence
   ↓
Gate-D
   ↓
Runtime-E Persistence
   ↓
Gate-E
   ↓
Runtime-F Governance
   ↓
Gate-F
   ↓
Runtime-G Extension
   ↓
Gate-G
   ↓
Runtime-H Integration
   ↓
Gate-H
```

Master Plan 已明确 P2 Runtime-A 至 H 的严格顺序，并禁止在 A-H 未闭环前进入 P3 Cognition。fileciteturn8file2L529-L552

因此：

> **Runtime-A PASS ≠ AgentOS 完成。**

它只代表：

> **AgentOS Runtime Kernel 第一块真实基础设施成立。**

---

# 32. Chief Architect 最终施工指令

从本施工包发布后：

```text
CURRENT AUTHORIZED WORK

P2 / Runtime-A
        ↓
RA-00 D1-D8 Decision Closure
        ↓
RA-01 Skeleton
        ↓
RA-02 Identity
        ↓
RA-03 Lifecycle
        ↓
RA-04 State Authority
        ↓
RA-05 Kernel Loop
        ↓
RA-06 Gate-A
        ↓
RA-07 Evidence / Failure Closure
        ↓
RA-08 Review / Completion
```

当前不得跳到：

```text
P3 Cognition
P4 Verification
P5 Team
P6 System Design
P7 UI
P8 FDE
P9 Hardening
```

同时：

```text
Cognition Design        = Future
UI/FDE Design            = Future
Class Refactor Skill     = Existing Capability
Universal Agent          = Future Integration
```

不得混淆。

---

# 33. Runtime-A 的唯一成功标准

最终只问一个问题：

> **我们是否已经拥有一个真实的、可运行的 AgentOS Runtime Kernel，它可以创建/识别 Agent 与 Task，在合法生命周期下执行 Runtime Operation，在非法状态下拒绝操作，并以真实 Build、真实 Test、真实 Gate-A、真实 Evidence 和 Independent Review 证明这些能力成立？**

如果：

```text
YES
```

则：

```text
Gate-A = PASS
Runtime-A = CLOSED
```

如果：

```text
NO
```

则：

```text
Runtime-A = OPEN
```

不论有多少代码、多少文档、多少测试模板、多少“看起来正确”的实现，都不能把 OPEN 改成 CLOSED。

---

# 34. 本施工包与上位文档的关系

```text
AI_Engineering_OS_最终权威执行版_v2.1_FDE.md
        = 产品与架构最高权威

AI_Engineering_OS_完整系统分阶段总体实施计划_v1.0.md
        = 全系统唯一施工总纲

P2 / Runtime-A 详细施工包 v1.0（本文件）
        = 当前阶段唯一直接施工入口

P2 Context / Checklist / Gate
        = 当前阶段状态事实

Runtime Architecture Spec
        = Runtime 架构合同

Implementation Plan
        = 本施工包的下位具体 HOW

Task Bundle
        = 单轮实际执行

Evidence / Review / Gate
        = 最终事实
```

任何工程师遇到冲突时必须向上追溯，不能向下妥协。

---

# 35. Version / Change Log

## v1.0

状态：**AUTHORITATIVE FOR P2 / RUNTIME-A EXECUTION**

本版本确立：

```text
P2 / Runtime-A 当前唯一施工边界
RA-00 ~ RA-08 施工结构
D1-D8 Decision Closure 前置门
Runtime Kernel 最小真实能力定义
Gate-A 执行原则
上下文防漂移协议
Evidence / Review / Completion 纪律
P3+ 严格冻结
```

任何影响本施工包范围、依赖、Gate 或完成定义的修改，必须通过正式 Change Record。

---

# FINAL EXECUTION ANCHOR

```text
============================================================
AI ENGINEERING OS — CURRENT EXECUTION ANCHOR
============================================================

PROGRAM PHASE : P2 AgentOS Governance & Runtime
SUBPHASE      : Runtime-A Runtime Kernel
CURRENT GATE  : P2A-ENTRY → Gate-A

PRIMARY GOAL  : 建立真实 AgentOS Runtime Kernel

DO             :
  Identity
  Task Identity
  Lifecycle Kernel
  State Authority
  Core Runtime Loop
  Runtime Tests
  Real Build
  Real Test
  Evidence
  Gate-A
  Independent Review

DO NOT         :
  Cognition
  Intelligent Verification
  Multi-Agent Team
  System Design Agent
  UI Agent
  FDE Team
  Universal Agent full integration
  Domain Intelligence

SUCCESS        : Real Execution + Evidence + Review + Gate-A PASS
FAILURE        : Any mandatory condition missing / governance violation

NEXT AUTHORIZED:
  Gate-A PASS → Runtime-B
  Gate-A FAIL → Repair Runtime-A

NEVER:
  Skip the Gate
  Rename failure as warning
  Use Mock as substitute for required real Kernel behavior
  Declare completion from narrative
  Jump to P3+
============================================================
```
