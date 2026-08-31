# Section 8 Phase A — Runtime Kernel 启动报告

> **本文件性质**：Phase A 启动报告（Start Report），非代码。
>
> **上位文档**：
> - Section 8 Runtime Architecture Spec v1.0 FROZEN
> - Section 8 Implementation Proposal（已批准）
>
> **生效日期**：2026-08-30 · **当前状态**：Phase A 启动报告提交，待 Chief Architect 二次确认后开始编码
>
> **本阶段门禁**：Implementation Gate-A（A1~A5），未通过不得进入 Phase B

---

## 0. 必读确认（Implementation Entry Rule）

Phase A 开始前，实施人员确认已阅读：

- [ ] Section 8 Runtime Architecture Spec v1.0（§0+§1+§13 重点）
- [ ] Section 8 Implementation Proposal（本文件上层）
- [ ] Runtime Constraints Registry（Constraint-01~10）
- [ ] Runtime Anti-Pattern List（AP-01~06）

**核心理解**：

> Agent Runtime ≠ Workflow Engine
>
> Agent Runtime = Identity Container + Lifecycle Controller + State Continuity Engine + Evidence Producer + Governance Boundary

---

## 1. 项目与命名空间（Project / Namespace）

### 1.1 Project 结构

按 D1 多 Project 分层 + D3 Capability Ownership Principle：

```
backend/modules/mod-runtime/
├── Runtime.Abstractions/                    # 所有 Interface（能力归属层）
│   ├── Kernel/                              # 能力归属 = Kernel
│   │   ├── IRuntimeKernel.cs
│   │   ├── IRuntimeLifecycleController.cs
│   │   └── IStateMachineDriver.cs
│   ├── State/
│   │   ├── ISessionStore.cs                 # 能力归属 = State（不是 Kernel）
│   │   └── IStateStore.cs
│   ├── Evidence/
│   │   ├── IEvidenceStore.cs
│   │   └── IEvidenceCapture.cs
│   ├── Persistence/
│   │   └── IPersistenceAdapter.cs
│   ├── Governance/
│   │   ├── IGovernanceAdapter.cs
│   │   └── IGovernanceInterceptor.cs
│   └── Extension/
│       ├── IModeLoader.cs
│       ├── IProfileLoader.cs
│       ├── IKnowledgeRouterAdapter.cs
│       └── IExtensionHookRegistry.cs
│
├── Runtime.Kernel/                          # Phase A 范围
│   ├── Kernel/                              # Lifecycle Supervisor + State Machine Driver + Governance Interceptor
│   │   ├── RuntimeKernel.cs
│   │   ├── RuntimeLifecycleController.cs
│   │   ├── StateMachineDriver.cs
│   │   └── GovernanceInterceptor.cs
│   ├── Identity/                            # AgentId / SessionId / RuntimeContext
│   │   ├── AgentId.cs
│   │   ├── SessionId.cs
│   │   └── RuntimeContext.cs
│   ├── Events/                              # RuntimeStarted/StateChanged/CheckpointCreated/RuntimeSuspended
│   │   ├── RuntimeEvent.cs
│   │   └── EventTypes.cs
│   └── Runtime.Kernel.csproj
│
└── Runtime.Tests/
    └── Gate-A-Verification/                 # Phase A 验证
        └── Gate-A-Tests.cs
```

### 1.2 Namespace 划分

| Project | Namespace | 依赖 |
|---------|-----------|------|
| Runtime.Abstractions | `JNPF.Runtime.Abstractions.*` | 无（依赖 .NET BCL） |
| Runtime.Kernel | `JNPF.Runtime.Kernel.*` | Runtime.Abstractions |
| Runtime.Tests | `JNPF.Runtime.Tests.*` | Runtime.Kernel + Abstractions |

**依赖规则（D3 Capability Ownership）**：
- Abstractions 不允许依赖具体实现 ✅
- Kernel 不依赖 Persistence 具体实现 ✅
- Runtime 不依赖 Intelligence ✅（Phase 2+ 才考虑）

### 1.3 .csproj 关键配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Runtime.Abstractions\Runtime.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

---

## 2. Kernel 核心对象设计

### 2.1 Runtime Identity 对象

#### 2.1.1 AgentId（强类型 ID）

| 字段 | 类型 | 含义 |
|------|------|------|
| Value | string | 全局唯一 UUID |

**关键约束**：
- 不可变（record 类型）
- 跨重启保持一致（Identity Continuity）

#### 2.1.2 SessionId

| 字段 | 类型 | 含义 |
|------|------|------|
| Value | string | UUID |
| AgentId | AgentId | 所属 Agent |

**关键约束**：
- SessionId 由 AgentId 派生，保证唯一
- Identity Continuity 不可丢失

#### 2.1.3 RuntimeContext

| 字段 | 类型 | 含义 |
|------|------|------|
| AgentId | AgentId | Agent 身份 |
| SessionId | SessionId | Session 身份 |
| CreatedAt | DateTime | 创建时间 |
| Environment | EnvironmentInfo | 环境信息（Phase A 占位） |

**关键约束**：
- 不可变结构（仅追加，不修改）
- 跨 Restart 持久化

### 2.2 Kernel 核心接口

#### 2.2.1 IRuntimeKernel（顶层入口）

```csharp
public interface IRuntimeKernel
{
    // Lifecycle
    Task<SessionId> CreateSessionAsync(AgentId agentId, CancellationToken ct);
    Task InitializeAsync(SessionId sessionId, CancellationToken ct);
    Task StartAsync(SessionId sessionId, CancellationToken ct);
    Task SuspendAsync(SessionId sessionId, CancellationToken ct);
    Task ResumeAsync(SessionId sessionId, CancellationToken ct);
    Task CompleteAsync(SessionId sessionId, CancellationToken ct);
    Task FailAsync(SessionId sessionId, FailureReason reason, CancellationToken ct);

    // Query
    Task<ExecutionState> GetStateAsync(SessionId sessionId, CancellationToken ct);
}
```

**关键约束（Constraint-04）**：
- Extension 不能调用此接口的修改方法
- 唯一调用者：Runtime 内部组件

#### 2.2.2 IRuntimeLifecycleController

```csharp
public interface IRuntimeLifecycleController
{
    Task<LifecycleTransitionResult> RequestTransitionAsync(
        SessionId sessionId,
        LifecycleTrigger trigger,
        CancellationToken ct);
}
```

**职责**：
- 接收所有状态转换请求
- 验证转换合法性（转换矩阵）
- 驱动 State Machine 执行转换
- 通知 Event Hub

#### 2.2.3 IStateMachineDriver

```csharp
public interface IStateMachineDriver
{
    StateTransitionResult Transition(
        ExecutionState currentState,
        LifecycleTrigger trigger);
}
```

**职责**：
- 纯逻辑层（不涉及 I/O）
- 接受当前状态 + 触发器，返回新状态 + 转换证据
- 验证转换合法性（8 态状态机的转换矩阵）

### 2.3 ExecutionState（Phase A 实现）

Phase A 实现 §5.5 ExecutionState 5 字段：

| 字段 | 类型 | 含义 |
|------|------|------|
| Value | StateValue | Created/Initialized/Running/Waiting/Suspended/Completed/Failed |
| Timestamp | DateTime | 进入时间 |
| TransitionReason | string | 原因 |
| TriggeringEvidence | EvidenceId? | 触发证据（Phase A 占位 null） |
| SessionId | SessionId | 所属 Session |

**Phase A 简化**：
- TriggeringEvidence 字段保留，但 Phase A 不实际生成 Evidence
- 实际 Evidence Capture 在 Phase C 实现
- Phase A 状态机**仅在内存**（不持久化，Phase D 持久化）

### 2.4 状态转换矩阵（Phase A 实现）

实现 §3.3 转换矩阵：

```
Created    → Initialized    (Initialize)
Initialized → Running       (Start)
Running    → Suspended      (Suspend)
Suspended  → Running        (Resume)
Running    → Completed      (Complete)
Running    → Failed         (Fail)
Suspended  → Failed         (Fail)
Failed     → Running        (Retry)
```

Phase A 不实现：Waiting/Resume from Waiting（Phase B 引入 ExecutionContext 后实现）

---

## 3. State Transition 实现方案

### 3.1 状态转换流程

```
Extension/Runtime 请求
       │
       ▼
IRuntimeLifecycleController.RequestTransitionAsync()
       │
       ├─→ IStateMachineDriver.Transition()（验证合法性）
       │
       ├─→ 转换合法 → 更新 ExecutionState
       │
       ├─→ 生成 StateTransitionEvidence 占位（Phase A 仅内存）
       │
       └─→ 触发 RuntimeEvent（StateChanged）
```

### 3.2 转换合法性验证

```csharp
public StateTransitionResult Transition(
    ExecutionState currentState,
    LifecycleTrigger trigger)
{
    // 转换矩阵查找
    var allowedNextStates = GetAllowedStates(currentState.Value, trigger);

    if (allowedNextStates == null)
        return StateTransitionResult.Rejected("Invalid transition");

    return StateTransitionResult.Success(
        new ExecutionState
        {
            Value = allowedNextStates[0],
            Timestamp = DateTime.UtcNow,
            TransitionReason = trigger.ToString(),
            SessionId = currentState.SessionId
        });
}
```

### 3.3 8 态状态机（Phase A 简化为 6 态）

Phase A 不实现 Waiting/Resumed（Phase B 引入 ExecutionContext 后实现）：

| State | Phase A |
|-------|:------:|
| Created | ✅ |
| Initialized | ✅ |
| Running | ✅ |
| Waiting | ❌ Phase B |
| Suspended | ✅ |
| Completed | ✅ |
| Failed | ✅ |
| Resumed | ❌ Phase B |

**Phase A 状态机完整性**：
- 完整 Suspend → Resume 循环 ✅
- 完整 Created → Completed 路径 ✅
- 完整 Fail 路径 ✅
- Waiting/Resumed 在 Phase B 加入（不影响 Gate-A）

---

## 4. Runtime Event 基础设计

### 4.1 事件基类

```csharp
public abstract record RuntimeEvent
{
    public EventId Id { get; init; }
    public SessionId SessionId { get; init; }
    public DateTime Timestamp { get; init; }
    public abstract string Type { get; }
}
```

### 4.2 Phase A 实现的事件类型

| Event Type | 触发时机 |
|-----------|---------|
| **RuntimeStarted** | Runtime 初始化时 |
| **SessionCreated** | Session 创建时 |
| **StateChanged** | 状态转换时 |
| **CheckpointCreated** | Checkpoint 创建时（Phase A 占位） |
| **RuntimeSuspended** | Session Suspend 时 |
| **RuntimeResumed** | Session Resume 时 |
| **SessionCompleted** | Session Completed 时 |
| **SessionFailed** | Session Failed 时 |

### 4.3 Event Hub 基础架构

Phase A 实现最小 Event Hub：

```csharp
public interface IEventHub
{
    Task PublishAsync(RuntimeEvent @event, CancellationToken ct);
    IDisposable Subscribe(string eventType, Func<RuntimeEvent, CancellationToken, Task> handler);
}
```

**Phase A 简化**：
- 仅支持 in-memory 发布订阅
- 不实现持久化（Phase C Evidence Store 接管）
- 不实现跨进程事件（Phase 2+）

---

## 5. Implementation Gate-A 验证计划

### 5.1 Gate-A 5 项验证

| Gate | 验证内容 | 验证方法 | 通过标准 |
|------|---------|---------|---------|
| **A1** | Kernel 可以创建 Agent Identity | xUnit + Integration Test | CreateSessionAsync 返回有效 SessionId |
| **A2** | State Transition 只能经过 Kernel | 静态扫描 + Dynamic Test | Extension 程序集不依赖 State Machine Driver |
| **A3** | Event 可以记录生命周期变化 | xUnit Integration Test | 状态转换后 Event 列表更新 |
| **A4** | 无 Workflow 固化代码 | 静态扫描 Anti-Pattern | 0 个 `ExecuteStep1/2/3` 模式 |
| **A5** | 无 Intelligence 依赖 | 静态扫描 + 依赖分析 | Runtime.Kernel 不引用 LLM/Prompt/Tool |

### 5.2 Gate-A 验证用例设计

#### A1 测试用例

```
Test: Kernel_CreateSession_ReturnsValidId
验证：CreateSessionAsync(agentId) 返回 SessionId
期望：SessionId.Value != null && SessionId.AgentId == agentId

Test: Kernel_DuplicateCreateSession_Throws
验证：重复创建相同 Session 抛出异常
```

#### A2 测试用例

```
Test: Kernel_OnlyKernelCanTransition
验证：Extension 试图调用 StateMachineDriver.Transition 失败
方法：检查 Extension 程序集 IL 引用

Test: Kernel_Transition_UpdatesState
验证：合法转换后 GetStateAsync 返回新状态
```

#### A3 测试用例

```
Test: Event_StateChanged_FiresOnTransition
验证：状态转换后 Event Hub 收到 StateChanged 事件

Test: Event_SessionCreated_FiresOnCreate
验证：Session 创建时 Event Hub 收到 SessionCreated
```

#### A4 测试用例（Anti-Pattern 静态扫描）

```
Test: AntiPattern_NoFixedPipeline
验证：搜索以下模式应 0 命中：
- ExecuteStep1() / ExecuteStep2()
- foreach (step in steps)
- if (step == N)

Test: AntiPattern_NoLLMDependency
验证：Runtime.Kernel 项目不引用：
- LLM Client
- Prompt Template
- Tool Registry
```

#### A5 测试用例

```
Test: DependencyAnalysis_NoIntelligence
验证：Runtime.Kernel 项目的 NuGet 依赖不包含：
- OpenAI
- Anthropic
- Azure.AI.OpenAI
- LangChain

Test: DependencyAnalysis_NoEF
验证：Runtime.Kernel 不引用：
- EF Core
- SqlSugar
- Dapper
```

### 5.3 Gate-A 测试项目结构

```
Runtime.Tests/
├── UnitTests/
│   ├── Kernel/
│   │   ├── RuntimeKernelTests.cs
│   │   ├── RuntimeLifecycleControllerTests.cs
│   │   └── StateMachineDriverTests.cs
│   └── Events/
│       └── EventHubTests.cs
├── Gate-A-Verification/
│   ├── A1_IdentityTests.cs
│   ├── A2_StateTransitionTests.cs
│   ├── A3_EventTests.cs
│   ├── A4_AntiWorkflowTests.cs
│   └── A5_NoIntelligenceTests.cs
└── Runtime.Tests.csproj
```

---

## 6. 自审结果（Self-Audit）

### 6.1 Constraint 自审

| Constraint | Phase A 是否满足 | 验证方法 |
|------------|:--------------:|---------|
| **Constraint-01** No Code Before Contract | ✅ Abstractions 先于实现 | Project 引用检查 |
| **Constraint-02** Domain Neutrality | ✅ 无 LLM/Prompt/Tool 泄漏 | 静态扫描 |
| **Constraint-03** Gate-01 Mapping | ✅ A1~A5 验证 G1~G5 子集 | Gate-A 测试 |
| **Constraint-04** Extension Inversion | ✅ Extension 不调用 Kernel 修改方法 | 静态依赖检查 |
| **Constraint-05** Contract Minimality | ✅ Port 接口仅能力抽象 | 接口审查 |
| **Constraint-06** Anti-Workflow Detection | ✅ Gate-A4 静态扫描 | AP-01~02 检测 |
| **Constraint-07** MVP Completeness | ✅ Suspend/Resume 完整 | 转换矩阵验证 |
| **Constraint-08** Persistence Neutrality | ✅ Phase A 不实现 Persistence | 项目依赖检查 |
| **Constraint-09** Governance Authority | ✅ Governance Interceptor 已就位 | 接口审查 |
| **Constraint-10** Implementation Order | ✅ Phase A 仅 Kernel | 项目范围确认 |

### 6.2 Iron Law 自审

| Iron Law | Phase A 是否满足 |
|---------|:--------------:|
| IRON-01（禁止降级为流程引擎） | ✅ State Machine 纯逻辑 |
| IRON-05（Agent 状态必须显式存在） | ✅ ExecutionState 独立维护 |
| IRON-08（Runtime 不拥有治理权） | ✅ Governance Adapter Port 已定义 |
| IRON-13（Governance 是 Active Runtime） | ✅ Governance Interceptor 嵌入 Lifecycle |
| IRON-14（Capability 必须行为真实） | ✅ State Transition 真实逻辑 |

### 6.3 LOCKED Decision 自审

| LOCKED | Phase A 是否满足 |
|--------|:--------------:|
| **EXT-01** Extension 不拥有 State Authority | ✅ State Machine Driver 仅 Kernel 内部使用 |
| **EXT-02** Extension 不拥有 Evidence Authority | ⏳ Phase A 不实现 Evidence Capture（Phase C） |
| **EXT-03** Extension 不拥有 Execution Authority | ✅ Extension 不能调用 RuntimeLifecycleController |

### 6.4 Gate-01（设计层面）

| Gate | Phase A 覆盖度 |
|------|:------------:|
| **G1** Agent Identity Preservation | ✅ AgentId/SessionId/RuntimeContext |
| **G2** State Preservation | ⏳ Phase A 仅内存，Phase D 持久化 |
| **G3** Evidence Preservation | ⏳ Phase A 占位，Phase C 实现 |
| **G4** Governance Enforcement | ✅ Governance Interceptor 已定义 |
| **G5** Extension Preservation | ⏳ Phase A 不实现 Port（Phase F） |

**Phase A Gate-01 覆盖**：2/5 设计 + 3/5 占位（符合渐进推进原则）

---

## 7. Phase A 交付物清单

### 7.1 Project / File 清单

| # | 类型 | 路径 |
|---|------|------|
| 1 | Solution | `backend/modules/mod-runtime/mod-runtime.sln` |
| 2 | Abstractions | `backend/modules/mod-runtime/Runtime.Abstractions/Runtime.Abstractions.csproj` |
| 3 | Abstractions/Kernel | `Runtime.Abstractions/Kernel/{IRuntimeKernel,IRuntimeLifecycleController,IStateMachineDriver}.cs` |
| 4 | Abstractions/State | `Runtime.Abstractions/State/{ISessionStore,IStateStore}.cs` |
| 5 | Abstractions/Evidence | `Runtime.Abstractions/Evidence/{IEvidenceStore,IEvidenceCapture}.cs` |
| 6 | Abstractions/Persistence | `Runtime.Abstractions/Persistence/IPersistenceAdapter.cs` |
| 7 | Abstractions/Governance | `Runtime.Abstractions/Governance/{IGovernanceAdapter,IGovernanceInterceptor}.cs` |
| 8 | Abstractions/Extension | `Runtime.Abstractions/Extension/{IModeLoader,IProfileLoader,IKnowledgeRouterAdapter,IExtensionHookRegistry}.cs` |
| 9 | Kernel | `backend/modules/mod-runtime/Runtime.Kernel/Runtime.Kernel.csproj` |
| 10 | Kernel/Identity | `Runtime.Kernel/Identity/{AgentId,SessionId,RuntimeContext}.cs` |
| 11 | Kernel/Kernel | `Runtime.Kernel/Kernel/{RuntimeKernel,RuntimeLifecycleController,StateMachineDriver}.cs` | ⭐ GovernanceInterceptor 已移至 Runtime.Governance |
| 12 | Kernel/Events | `Runtime.Kernel/Events/{RuntimeEvent,EventTypes,IEventHub,InMemoryEventHub}.cs` |
| 13 | Tests | `backend/modules/mod-runtime/Runtime.Tests/Runtime.Tests.csproj` |
| 14 | Gate-A Tests | `Runtime.Tests/Gate-A-Verification/A{1-5}_*Tests.cs` |

### 7.2 Phase A 代码量估算

| 类别 | 行数估算 |
|------|---------|
| Abstractions（14 文件） | 约 200 行 |
| Kernel 实现（7 文件） | 约 400 行 |
| Event 实现（3 文件） | 约 150 行 |
| Unit Tests（10 文件） | 约 600 行 |
| Gate-A Tests（5 文件） | 约 400 行 |
| **总计** | **约 1750 行** |

### 7.3 Phase A 完成时间

| 步骤 | 工作量 |
|------|-------|
| Abstractions 创建 | 0.5 天 |
| Kernel 核心实现 | 3 天 |
| Event 实现 | 0.5 天 |
| Unit Tests | 1 天 |
| Gate-A Tests | 1 天 |
| Gate-A 验证 | 0.5 天 |
| **总计** | **约 1.5 周** |

---

## 8. Phase A 后续衔接

### 8.1 Phase A 完成标志

- [ ] 全部 Project / File 已创建
- [ ] 全部 Unit Tests 通过
- [ ] 全部 Gate-A Tests 通过
- [ ] 自审清单全部 ✅
- [ ] Phase A 完成报告提交

### 8.2 Phase A → Phase B 衔接

Phase A 完成 + Gate-A 全通过后：

1. 提交 Phase A 完成报告
2. Chief Architect 审批
3. 进入 Phase B（State & Context Layer 完整实现）

---

## 9. 风险与缓解

| 风险 | 概率 | 影响 | 缓解 |
|------|:----:|:----:|------|
| Gate-A 验证发现 Anti-Pattern | 中 | 中 | 静态扫描提前发现，立即修复 |
| 状态机实现复杂度低估 | 低 | 中 | 6 态 Phase A 简化（不实现 Waiting）|
| Event Hub 性能问题 | 低 | 低 | Phase A 仅 in-memory，后续优化 |
| Abstractions 设计缺陷影响后续 | 中 | 高 | 本报告需 Chief Architect 二次确认 |

---

> **下一步动作**：等待 Chief Architect 对 Phase A 启动报告的二次确认，确认后开始实际编码。

> **请 Chief Architect 审批**：
> - ✅ 批准 Phase A 启动报告，开始编码
> - ❌ 暂停，Phase A 设计需修订
> - 📋 部分修订（请明确哪部分）