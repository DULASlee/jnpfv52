# Phase 2-C Discovery Report

> **Phase:** Section 8 Runtime Foundation — Phase 2-C: Section 9 Mode Integration  
> **Date:** 2026-08-31  
> **Status:** IN PROGRESS

---

## 1. Section 9 Mode System (Frozen Baseline)

### 1.1 Core Contracts

**IMode Interface:**
```csharp
public interface IMode
{
    string Name { get; }
    ModeType Type { get; }
    ModeCapabilitySet Capabilities { get; }
    ConstraintSet Constraints { get; }
}
```

**ModeType Enum:**
```csharp
public enum ModeType { Audit, Verify, Execute, Assist }
```

**Capability Enum:**
```csharp
public enum Capability
{
    Observe, Evaluate, Reflect, Build, Test,
    ApplyApprovedPatch, ApplyUnapprovedChange, ModifyState,
    ReadEvidence, WriteEvidence
}
```

### 1.2 Default Mode Capabilities

| Capability | Audit | Verify | Execute | Assist |
|-------------|-------|--------|---------|--------|
| Observe | ✅ | ✅ | ✅ | Profile |
| Evaluate | ✅ | ✅ | ✅ | Profile |
| Reflect | ✅ | ✅ | ✅ | Profile |
| ReadEvidence | ✅ | ✅ | ✅ | Profile |
| Build | ❌ | ✅ | ✅ | Profile |
| Test | ❌ | ✅ | ✅ | Profile |
| WriteEvidence | ❌ | ❌ | ✅ | Profile |
| ApplyApprovedPatch | ❌ | ❌ | ✅ | Profile |
| ModifyState | ❌ | ❌ | ✅ | Profile |
| ApplyUnapprovedChange | ❌ | ❌ | ❌ | ❌ |

### 1.3 Mode Constraints

```csharp
public record ConstraintSet
{
    bool ModeRequiresExplicitAuthorization { get; init; }  // Execute=true
    bool CanTriggerStateTransition { get; init; }        // false
}
```

### 1.4 IModeProvider

```csharp
public interface IModeProvider
{
    Task<IMode> ResolveAsync(ModeType modeType, CancellationToken ct);
    Task<List<IMode>> ListAsync(CancellationToken ct);
}
```

### 1.5 M17 Binding Rule

> **Runtime → Mode 单向依赖。Mode 不持有 Runtime 引用。**

```
✅ RuntimeLifecycleController → IModeProvider → IMode
❌ IMode → Runtime（禁止）
```

---

## 2. Runtime.Core v0.1 (Frozen Baseline)

### 2.1 Core Components

| Component | Description |
|-----------|-------------|
| RuntimeContext | TenantId, ProjectId, PipelineId, UserId |
| RuntimeSession | SessionId, RuntimeState, RuntimeContext |
| RuntimeState | Initialized, Running, Paused, Completed, Failed, Disposed |
| IRuntimeLifecycleController | Session lifecycle methods |
| ExecutionContext | ExecutionId, SessionId, Hooks, Cancellation |
| ExecutionState | Pending, Running, Completed, Failed, Cancelled |
| ExecutionResult | Immutable execution result |
| IHookRegistry | Register, Unregister, GetHooks |
| IExecutionHook | Before, After, OnFailure, OnCancelled |

### 2.2 Execution Flow

```
CreateExecution(sessionId)
      ↓
ExecuteAsync(execution, work)
      ↓
  1. Publish ExecutionStartedEvent
      ↓
  2. Invoke Before Hooks
      ↓
  3. Check Cancellation
      ↓
  4. Execute Work
      ↓
  5. Invoke After Hooks
      ↓
  6. Publish ExecutionCompletedEvent
      ↓
  7. Return ExecutionResult
```

### 2.3 Current API Surface (IRuntimeLifecycleController)

```csharp
public interface IRuntimeLifecycleController
{
    RuntimeSession? CurrentSession { get; }
    Task<RuntimeSession> InitializeAsync(RuntimeContext context, CancellationToken ct);
    Task StartAsync(Guid sessionId, CancellationToken ct);
    Task PauseAsync(Guid sessionId, CancellationToken ct);
    Task ResumeAsync(Guid sessionId, CancellationToken ct);
    Task CompleteAsync(Guid sessionId, CancellationToken ct);
    Task FailAsync(Guid sessionId, string reason, CancellationToken ct);
    Task DisposeAsync(Guid sessionId, CancellationToken ct);
    
    // Phase 2-B Extension
    ExecutionContext CreateExecution(Guid sessionId);
    ExecutionContext CreateExecution(Guid sessionId, IHookRegistry hookRegistry);
    Task<ExecutionResult> ExecuteAsync(ExecutionContext execution, Func<ExecutionContext, Task> work, CancellationToken ct);
}
```

---

## 3. Gap Analysis

### 3.1 Missing Integrations

| Gap | Description | Priority |
|-----|-------------|----------|
| Mode Resolution | No IModeProvider in Runtime | P0 |
| Execution Policy | No policy abstraction linking Mode to Execution | P0 |
| Admission Control | No policy check before Execute | P0 |
| Mode in ExecutionContext | Execution doesn't know current Mode | P1 |
| Authorization Check | Execute Mode requires explicit auth | P1 |

### 3.2 Integration Target Architecture

```
Section 9
    │
    ▼
IModeProvider
    │
    ▼
IMode → ExecutionPolicy (minimal contract)
    │
    ▼
ExecutionContext + ExecutionAdmission
    │
    ▼
RuntimeLifecycleController.ExecuteAsync
    │
    ├── Before Hooks
    ├── Work
    ├── After Hooks
    └── Result
```

---

## 4. Section 9 Frozen Contract Summary

| Contract | Location | Frozen |
|----------|----------|--------|
| IMode | Runtime.Capability/Modes/IMode.cs | ✅ |
| ModeType | Runtime.Capability/Modes/ModeType.cs | ✅ |
| Capability | Runtime.Capability/Capabilities/Capability.cs | ✅ |
| IModeProvider | Runtime.Capability/Loading/IModeProvider.cs | ✅ |
| ModeCapabilitySet | Runtime.Capability/Capabilities/ModeCapabilitySet.cs | ✅ |
| ConstraintSet | Runtime.Capability/Constraints/ConstraintSet.cs | ✅ |
| 4 Default Modes | Runtime.Capability/Modes/*.cs | ✅ |

---

## 5. Key Design Questions (from Phase 2-C Spec)

| # | Question | Proposed Answer |
|---|----------|-----------------|
| 1 | Mode 属于 Session 还是 Execution？ | **Session** — Mode 是执行上下文的权限声明 |
| 2 | Session 是否允许 Mode 在 Execution 之间切换？ | **否** — Mode 在 Session 级别固定 |
| 3 | Execution 创建时是否冻结 Mode？ | **是** — Mode Policy 在 Admission 时确定 |
| 4 | Execution 过程中 Mode 是否允许改变？ | **否** — Execution 生命周期内 Policy 不可变 |
| 5 | Mode 与 ExecutionState 的关系？ | **独立** — Mode 影响 Admission，不影响 State |
| 6 | Mode Policy 是否 immutable？ | **是** — Snapshot at Admission |
| 7 | Policy 在 Admission 验证还是每个 Hook 验证？ | **Admission** — Hook 不做 Policy 验证 |
| 8 | Execute Mode 的授权在哪里检查？ | **Admission** — ConstraintSet.ModeRequiresExplicitAuthorization |
| 9 | Mode 如何参与取消？ | **不参与** — Cancellation 是 Execution 内部机制 |
| 10 | Mode 如何参与失败？ | **不参与** — Failure 是 Execution 结果 |
| 11 | Mode 是否可以阻止 Execution？ | **是** — Admission 拒绝 |
| 12 | Mode 是否可以修改 Runtime State？ | **否** — M8 Constraint |
| 13 | Hook 是否可以改变 Policy？ | **否** — Policy immutable |
| 14 | Event Handler 是否可以改变 Policy？ | **否** — Event 是观察者模式 |
| 15 | Runtime 是否需要知道 Audit/Verify/Execute？ | **否** — 只认识 ExecutionPolicy Contract |

---

## 6. Discovery Conclusion

**Ready for Phase 1: Requirement & Architecture Analysis**

Key findings:
1. Section 9 Mode Contract is complete (frozen)
2. Runtime.Core Execution is complete (frozen)
3. Gap: Mode → Execution Policy abstraction missing
4. Design decision: Mode belongs to Session, Policy snapshot at Admission
