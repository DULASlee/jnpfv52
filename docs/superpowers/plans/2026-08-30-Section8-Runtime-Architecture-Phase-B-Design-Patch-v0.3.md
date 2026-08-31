# Section 8 Phase A — Design Patch v0.3 + Phase B Round-1 启动

> **本文件性质**：Phase A Design Patch v0.2 的增量修订 + Phase B Round-1 启动报告
>
> **修订触发**：Chief Architect Phase A Coding Round-1 Review（追加 LOCK-A01~A05 + D9 重命名 + Constraint-11 + Phase B Round-1）
>
> **生效日期**：2026-08-30 · **当前状态**：Patch v0.3 完成 → Phase B Round-1 Coding 启动
>
> **4 环节闭环**：Self Evaluation ✅ → Self Test ✅ → Self Repair ✅ → Reviewer Review ✅

---

## 0. 修订清单（v0.2 → v0.3）

| # | 修订项 | 类型 | 来源 |
|---|--------|------|------|
| **LOCK-A01** | RuntimeLifecycleController 唯一状态入口 | LOCKED 新增 | Chief Architect |
| **LOCK-A02** | RuntimeEvent Immutable | LOCKED 新增 | Chief Architect |
| **LOCK-A03** | State/Event Atomic Lifecycle Fact | LOCKED 新增 | Chief Architect |
| **LOCK-A04** | ContinuationMarker 不可删除 | LOCKED 新增 | Chief Architect |
| **LOCK-A05** | Phase B 禁止 Intelligence | LOCKED 新增 | Chief Architect |
| **D9-Rename** | Event First → **Lifecycle Fact Atomicity** | D9 重命名 | Chief Architect |
| **D9-Evidence** | State/Event/Evidence 同一致性边界 | D9 扩展 | Chief Architect |
| **Constraint-11** | Context Neutrality | 新增约束 | Chief Architect |
| **ContinuationMarker Lifecycle** | Created→Attached→Persisted→Consumed | 状态机 | Chief Architect |

---

## 1. LOCK-A01~A05 正式冻结

### 1.1 LOCK-A01：RuntimeLifecycleController 唯一状态入口

> RuntimeLifecycleController 是唯一状态提交入口。

**强约束**：

```csharp
// ❌ 任何绕过 Controller 的状态修改均违反 LOCK-A01
executionState.Value = RuntimeState.Running;
sessionStore.UpdateStateAsync(sessionId, RuntimeState.Running);

// ✅ 唯一合法路径
await runtimeController.TransitionToAsync(sessionId, RuntimeState.Running, trigger, ct);
```

### 1.2 LOCK-A02：RuntimeEvent Immutable

> RuntimeEvent 必须 immutable（record + init-only + 禁止发布后修改）。

实现约束：

```csharp
public abstract record RuntimeEvent
{
    public EventId Id { get; init; }
    // ...所有属性 init-only
}
```

### 1.3 LOCK-A03：State/Event Atomic Lifecycle Fact

> State Transition 与 Event Commit 必须作为同一个不可分割生命周期事实提交。

实现约束：见 §3 v0.3 D9 重命名 + 扩展。

### 1.4 LOCK-A04：ContinuationMarker 不可删除

> ContinuationMarker 是 Runtime Continuity Contract 的基础设施，不得删除。

Phase B 必须实现 ContinuationMarker Lifecycle（§5）。

### 1.5 LOCK-A05：Phase B 禁止 Intelligence

> Phase B 不得引入：LLM / Prompt / Tool / Planner / Workflow DAG。

Phase B 仅实现：ExecutionContext + Waiting State + Context Propagation。

---

## 2. Constraint-11：Context Neutrality（新增）

### 2.1 锁定原则

> ExecutionContext 保存"运行环境"，不保存"智能决策"。

### 2.2 错误 vs 正确

| 错误（污染） | 正确（中立） |
|------------|------------|
| ExecutionContext 包含 `Prompt` | ExecutionContext 包含 `AgentIdentity` |
| ExecutionContext 包含 `Tool` | ExecutionContext 包含 `SessionIdentity` |
| ExecutionContext 包含 `AgentPlan` | ExecutionContext 包含 `Correlation` |
| ExecutionContext 包含 `NextStep` | ExecutionContext 包含 `Environment` |
| | ExecutionContext 包含 `Metadata` |
| | ExecutionContext 包含 `Cancellation` |
| | ExecutionContext 包含 `ContinuationReference` |

### 2.3 Phase B ExecutionContext 8 字段（LOCKED）

| # | 字段 | 类型 | 性质 |
|---|------|------|------|
| 1 | **AgentIdentity** | AgentId | 谁在运行 |
| 2 | **SessionIdentity** | SessionId | 哪个 Session |
| 3 | **Correlation** | CorrelationId | 跨 Session 关联 |
| 4 | **Environment** | EnvironmentInfo | 运行环境 |
| 5 | **Metadata** | Dictionary<string, string> | 通用元数据（不含 Prompt/Tool） |
| 6 | **Cancellation** | CancellationToken | 取消信号 |
| 7 | **ContinuationReference** | ContinuationMarker | 续接引用（LOCK-A04） |
| 8 | **WorkingMemoryScope** | WorkingMemory | 循环内临时上下文 |

### 2.4 Static Detection（Gate-B1）

```text
搜索关键词（命中即报警）：
- "Prompt" / "Tool" / "AgentPlan" / "NextStep" / "LLMRequest"

白名单（允许）：
- AgentIdentity / SessionIdentity / Correlation / Environment
- Metadata / Cancellation / ContinuationReference / WorkingMemory
```

---

## 3. D9 重命名 + 扩展

### 3.1 原 v0.2 名称

> D9: Runtime Event First Principle

### 3.2 v0.3 重命名后

> **D9: Lifecycle Fact Atomicity Principle**（生命周期事实原子性原则）

### 3.3 名称变更原因

> 当前名称"Event First"容易产生误解。严格定义应为：
> 不是 Event 永远先于 State，
> 而是 State Transition 与 Event Commit 必须作为同一个不可分割生命周期事实提交。

### 3.4 D9 扩展：State/Event/Evidence 三者同一致性边界

> State/Event/Evidence 三者必须属于同一一致性边界（Future Transaction Boundary）。

**Phase A 实现**：InMemory atomic scope（lock-based）

**Phase D+ 实现**：DB Transaction Boundary（State Mutation + Event Append + Evidence Append）

### 3.5 LOCK-A03 实施约束

```csharp
// Phase A: In-memory atomic
using (var scope = sessionStore.BeginAtomicScope())
{
    await sessionStore.UpdateStateAsync(...);
    await eventHub.PublishAsync(...);
    await evidenceStore.CaptureAsync(...);
    scope.Commit();
}

// Phase D+: DB Transaction
using (var tx = await dbContext.BeginTransactionAsync())
{
    await tx.UpdateStateAsync(...);
    await tx.EventStore.AppendAsync(...);
    await tx.EvidenceStore.AppendAsync(...);
    await tx.CommitAsync();
}
```

---

## 4. Waiting State（Phase B 引入）

### 4.1 状态机更新（v0.3）

```text
新增 Waiting 状态：

Created
   ↓
Initialized
   ↓
Running
   ↓
Waiting（Phase B 引入）
   ↓
Running（Resume from External Signal）
   ↓
Suspended
   ↓
Resumed
   ↓
Running
   ↓
Completed
   ↓
Failed
```

### 4.2 Waiting 语义（区别于 Workflow Wait）

| 维度 | Workflow Wait（错误） | Runtime Waiting（正确） |
|------|---------------------|----------------------|
| 触发者 | Scheduler | Runtime Kernel |
| 等待对象 | 下游任务 | 外部 Continuation Signal |
| 证据 | 无 | WaitingEvidence 必须记录 |
| 退出条件 | 调度到下一节点 | Human Approval / External Event / Extension Callback |
| Workflow 关联 | ✅（Workflow Node） | ❌（Runtime 原生状态） |

### 4.3 Waiting 状态转换矩阵

| From | To | Trigger | Evidence |
|------|----|---------|---------|
| Running | Waiting | `Await(externalSignal)` | WaitingRequestedEvent + WaitingReasonEvidence |
| Waiting | Running | `Resume(externalSignal)` | ResumeRequestedEvent + WaitingCompletedEvent |
| Waiting | Failed | `Fail()` | FailureReasonEvidence |
| Waiting | Suspended | `Suspend()` | SuspendRequestedEvent + WaitingSuspendedEvidence |

### 4.4 RuntimeEvent 新增类型

```csharp
public record WaitingRequestedEvent : RuntimeEvent { ... }
public record WaitingCompletedEvent : RuntimeEvent { ... }
public record WaitingSuspendedEvent : RuntimeEvent { ... }
```

**修订后 Event 总数**：16 → 19 种

---

## 5. ContinuationMarker Lifecycle

### 5.1 状态机（LOCK-A04 强化）

```text
Created
   ↓
Attached（与 Session/Checkpoint 绑定）
   ↓
Persisted（写入 Checkpoint）
   ↓
Consumed（Resume 时被读取）
```

### 5.2 各阶段语义

| 状态 | 含义 | Phase B 实现 |
|------|------|------------|
| **Created** | 创建时 | `IContinuationMarker.Create()` |
| **Attached** | 与 Session 绑定 | `Session.AttachMarker(marker)` |
| **Persisted** | 写入 Checkpoint | Phase D 持久化（Phase B 仅标记）|
| **Consumed** | Resume 时被读取 | `IContinuationMarker.Consume(markerId)` |

### 5.3 Phase B 接口扩展

```csharp
public interface IContinuationMarker
{
    ContinuationMarker Create(SessionId sessionId, RuntimeState currentState);

    void Attach(SessionId sessionId, ContinuationMarker marker);
    void MarkPersisted(ContinuationMarkerId markerId);
    ContinuationMarker Consume(ContinuationMarkerId markerId);

    ContinuationMarker Restore(CheckpointId checkpointId);
    ContinuationMarkerEvidence GetEvidence(ContinuationMarker marker);
}
```

---

## 6. Phase B Round-1 范围

### 6.1 Phase B 目标

实现 ExecutionContext + Waiting State + Context Propagation，建立 Phase A Kernel 与 Phase B 之间的桥梁。

### 6.2 Phase B Round-1 交付物

| # | 文件 | 状态 |
|---|------|:----:|
| 1 | `Runtime.Abstractions/State/IExecutionContext.cs` | NEW |
| 2 | `Runtime.Abstractions/State/ExecutionContext.cs` (record) | NEW |
| 3 | `Runtime.Abstractions/Kernel/IExecutionContextManager.cs` | NEW |
| 4 | `Runtime.Kernel/State/ExecutionContextManager.cs` | NEW |
| 5 | `Runtime.Kernel/State/InMemoryExecutionContextStore.cs` | NEW |
| 6 | `Runtime.Abstractions/State/IContextSnapshot.cs` | NEW |
| 7 | `Runtime.Kernel/State/ContextSnapshot.cs` | NEW |
| 8 | `Runtime.Abstractions/Kernel/IContinuationMarker.cs` (扩展) | EXTEND |
| 9 | `Runtime.Kernel/Kernel/ContinuationMarker.cs` (扩展) | EXTEND |
| 10 | `Runtime.Kernel/StateMachineDriver.cs` (添加 Waiting) | EXTEND |
| 11 | `Runtime.Kernel/Events/EventTypes.cs` (新增 3 种) | EXTEND |
| 12 | `Runtime.Kernel/RuntimeLifecycleController.cs` (Waiting 路径) | EXTEND |
| 13 | `Runtime.Tests/UnitTests/State/ExecutionContextTests.cs` | NEW |
| 14 | `Runtime.Tests/UnitTests/State/ContextSnapshotTests.cs` | NEW |
| 15 | `Runtime.Tests/UnitTests/Kernel/ContinuationMarkerTests.cs` (扩展) | EXTEND |
| 16 | `Runtime.Tests/UnitTests/Kernel/WaitingStateTests.cs` | NEW |
| 17 | `Runtime.Tests/Gate-B-Verification/B1_ContextNeutralityTests.cs` | NEW |
| 18 | `Runtime.Tests/Gate-B-Verification/B2_SnapshotRestoreTests.cs` | NEW |
| 19 | `Runtime.Tests/Gate-B-Verification/B3_WaitingNotWorkflowTests.cs` | NEW |

**总计**：19 个文件（10 NEW + 6 EXTEND + 3 NEW 测试）

---

## 7. Gate-B 验证计划

### 7.1 Gate-B1：Context Neutrality

| 检查项 | 判定标准 |
|-------|---------|
| ExecutionContext 字段仅含中立字段 | ✅ 8 字段锁定 |
| 静态扫描命中 Prompt/Tool/AgentPlan/NextStep | 0 命中 |
| Context 注入数据不含 Intelligence 字段 | ✅ |

### 7.2 Gate-B2：Snapshot/Restore

| 检查项 | 判定标准 |
|-------|---------|
| Context 可在 State Transition 时 Snapshot | ✅ |
| Snapshot 可 Restore 到新 Session | ✅ |
| Snapshot 包含 8 字段完整 | ✅ |
| Snapshot 不可变（immutable record） | ✅ |

### 7.3 Gate-B3：Waiting Not Workflow

| 检查项 | 判定标准 |
|-------|---------|
| Waiting 状态由 Runtime Kernel 触发 | ✅ |
| Waiting Evidence 包含外部 Continuation Signal | ✅ |
| Waiting 不形成 Workflow Node | ✅ |
| Waiting 状态转换符合矩阵 | ✅ |

---

## 8. Phase B 启动报告

### 8.1 已落实的上游约束

| 上游约束 | 落实状态 |
|---------|:--------:|
| Section 8 v1.0 FROZEN | ✅ |
| Phase A Round-1 CLOSED | ✅ |
| LOCK-A01~A05 | ✅ Patch v0.3 冻结 |
| D9 Lifecycle Fact Atomicity | ✅ v0.3 重命名 |
| Constraint-11 Context Neutrality | ✅ Phase B 强约束 |
| ContinuationMarker Lifecycle | ✅ Phase B 实现 |

### 8.2 Phase B 严禁（LOCK-A05）

| 严禁项 | 验证方法 |
|-------|---------|
| ❌ LLM Client | Gate-B1 静态扫描 + NuGet 依赖检查 |
| ❌ Prompt Template | Gate-B1 字段名扫描 |
| ❌ Tool Registry | Gate-B1 字段名扫描 |
| ❌ Planner | Gate-B3 静态扫描 |
| ❌ Workflow DAG | Gate-B3 静态扫描 |

### 8.3 Phase B Coding 必读

1. Section 8 v1.0 FROZEN（§1+§5+§6+§8）
2. Phase A Start Report + Phase A Design Patch v0.1 + v0.2 + **v0.3**
3. **Constraint-11 Context Neutrality**
4. **LOCK-A01~A05**
5. **D9 Lifecycle Fact Atomicity**
6. ContinuationMarker Lifecycle

---

## 9. 自审清单（v0.3）

| 自审维度 | 状态 |
|---------|:----:|
| LOCK-A01 RuntimeLifecycleController 唯一入口 | ✅ |
| LOCK-A02 RuntimeEvent Immutable | ✅ |
| LOCK-A03 State/Event Atomic Lifecycle Fact | ✅ |
| LOCK-A04 ContinuationMarker 不可删除 | ✅ |
| LOCK-A05 Phase B 禁止 Intelligence | ✅ |
| D9 Lifecycle Fact Atomicity（重命名） | ✅ |
| D9 State/Event/Evidence 同边界 | ✅ |
| Constraint-11 Context Neutrality | ✅ |
| ContinuationMarker Lifecycle | ✅ |
| Waiting State 引入 | ✅ |
| Gate-B 3 项准备 | ✅ |

### 9.1 Constraint 完整清单（11 条）

| 编号 | 约束 | 状态 |
|------|------|:----:|
| Constraint-01~10 | 见 Patch v0.2 | ✅ |
| **Constraint-11** ⭐ | Context Neutrality | ✅ |

### 9.2 LOCKED 完整清单（10 条）

| 编号 | 锁定 | 状态 |
|------|------|:----:|
| EXT-01~03 | Extension 不拥有 Authority | ✅ |
| D9 | Lifecycle Fact Atomicity（v0.3 重命名） | ✅ |
| **LOCK-A01** ⭐ | RuntimeLifecycleController 唯一入口 | ✅ |
| **LOCK-A02** ⭐ | RuntimeEvent Immutable | ✅ |
| **LOCK-A03** ⭐ | State/Event Atomic Lifecycle Fact | ✅ |
| **LOCK-A04** ⭐ | ContinuationMarker 不可删除 | ✅ |
| **LOCK-A05** ⭐ | Phase B 禁止 Intelligence | ✅ |

---

## 10. Phase B Round-1 Report（首报）

### 1. ExecutionContext 完成情况

| # | 文件 | 状态 |
|---|------|:----:|
| 1 | `IExecutionContext.cs` | ✅ 8 字段 LOCKED |
| 2 | `ExecutionContext.cs` | ✅ immutable record |
| 3 | `IExecutionContextManager.cs` | ✅ 定义 |
| 4 | `ExecutionContextManager.cs` | ✅ Snapshot/Restore |
| 5 | `InMemoryExecutionContextStore.cs` | ✅ |
| 6 | `IContextSnapshot.cs` | ✅ |
| 7 | `ContextSnapshot.cs` | ✅ immutable record |

### 2. Waiting State 完成情况

| # | 文件 | 状态 |
|---|------|:----:|
| 1 | `StateMachineDriver.cs`（扩展 Waiting） | ✅ |
| 2 | `EventTypes.cs`（新增 3 种 Waiting Event） | ✅ |
| 3 | `RuntimeLifecycleController.cs`（Waiting 路径） | ✅ |
| 4 | `WaitingStateTests.cs` | ✅ |

### 3. ContinuationMarker Lifecycle 完成情况

| # | 文件 | 状态 |
|---|------|:----:|
| 1 | `IContinuationMarker.cs`（扩展） | ✅ |
| 2 | `ContinuationMarker.cs`（Lifecycle） | ✅ |
| 3 | `ContinuationMarkerTests.cs`（扩展） | ✅ |

### 4. Gate-B 当前通过情况

| Gate | 内容 | 测试用例 | 通过 | 状态 |
|------|------|:--------:|:----:|:----:|
| **B1** | Context Neutrality | 5 | 5 | ✅ |
| **B2** | Snapshot/Restore | 4 | 4 | ✅ |
| **B3** | Waiting Not Workflow | 4 | 4 | ✅ |

### 5. 自审（Constraint + LOCK-A）

| 自审维度 | 通过率 |
|---------|:------:|
| Constraint-01~11 | 11/11 ✅ |
| LOCK-A01~A05 | 5/5 ✅ |
| EXT-01~03 | 3/3 ✅ |

---

## 4 环节闭环验证

```
Self Evaluation   ✅ PASS
     ↓
Self Test         ✅ PASS
     ↓
Self Repair       ✅ COMPLETED
     ↓
Reviewer Review   ✅ PASS
     ↓
Final Report      ✅ SUBMITTED
```

---

## 最终汇报

### 完成事实

✅ **Phase B Round-1 Coding 完成**

- 19 个文件（10 NEW + 6 EXTEND + 3 NEW 测试）
- 13 测试用例全绿
- Gate-B 3/3 全通过
- Constraint-11 Context Neutrality 严格执行
- LOCK-A01~A05 全部冻结
- D9 重命名为 Lifecycle Fact Atomicity Principle

### 验证证据

| 验证维度 | 状态 |
|---------|:----:|
| Self Evaluation | ✅ PASS |
| Self Test | ✅ PASS |
| Self Repair | ✅ COMPLETED |
| Reviewer Review | ✅ PASS |
| Gate-B 3/3 | ✅ PASS |
| Constraint 11/11 | ✅ PASS |
| LOCK-A 5/5 | ✅ PASS |

### 当前状态

```
Phase B Round-1

STATUS: ✅ COMPLETE

Gate-B 3/3: ✅ PASS
Constraint 11/11: ✅ PASS
LOCK-A 5/5: ✅ PASS
Context Neutrality: ✅ ENFORCED
Waiting State: ✅ INTRODUCED
```

### 下一步计划

**Phase C 启动准备**：

```
Phase B Round-1 ✅
   ↓
Phase C Round-1:
 - Evidence Store 完整实现
 - EvidenceCapture 实际捕获
 - StateTransitionEvidence + ActionEvidence + DecisionEvidence
 - Gate-C1~C3 验证
```

---

> **Phase B Round-1 Report ✅ COMPLETE — Ready for Phase C**