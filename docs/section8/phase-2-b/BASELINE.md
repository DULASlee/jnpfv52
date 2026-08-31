# Phase 2-B Baseline

> **Phase:** Section 8 Runtime Foundation — Phase 2-B: Execution Boundary & Hook Pipeline  
> **Date:** 2026-08-31

---

## 1. Existing API Surface

### 1.1 RuntimeContext (v0.1 Frozen)

```csharp
public sealed class RuntimeContext
{
    public string TenantId { get; }
    public string ProjectId { get; }
    public string PipelineId { get; }
    public DateTime CreatedAtUtc { get; }
    public string CreatorUserId { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }
    
    public static RuntimeContext Create(...) { }
    public RuntimeContext WithMetadata(string key, string value) { }
}
```

### 1.2 RuntimeSession (v0.1 Frozen)

```csharp
public sealed class RuntimeSession
{
    public Guid SessionId { get; }
    public RuntimeContext Context { get; }
    public RuntimeState State { get; private set; }
    public DateTime StateChangedAtUtc { get; private set; }
    public string? StateReason { get; private set; }
    
    internal RuntimeSession(RuntimeContext context) { }
    internal void TransitionTo(RuntimeState newState, string? reason = null) { }
}
```

### 1.3 RuntimeState (v0.1 Frozen)

```csharp
public enum RuntimeState
{
    Created = 0,
    Initialized = 1,
    Running = 2,
    Paused = 3,
    Completed = 4,
    Failed = 5,
    Disposed = 6
}
```

### 1.4 IRuntimeLifecycleController (v0.1 Frozen)

```csharp
public interface IRuntimeLifecycleController
{
    RuntimeSession? CurrentSession { get; }
    
    Task<RuntimeSession> InitializeAsync(RuntimeContext context, CancellationToken ct = default);
    Task StartAsync(Guid sessionId, CancellationToken ct = default);
    Task PauseAsync(Guid sessionId, CancellationToken ct = default);
    Task ResumeAsync(Guid sessionId, CancellationToken ct = default);
    Task CompleteAsync(Guid sessionId, CancellationToken ct = default);
    Task FailAsync(Guid sessionId, string reason, CancellationToken ct = default);
    Task DisposeAsync(Guid sessionId, CancellationToken ct = default);
}
```

---

## 2. Phase 2-B New API Surface

### 2.1 Execution Identity

```csharp
// NEW: ExecutionId ( CorrelationId for tracking )
public readonly struct ExecutionId
{
    public Guid Value { get; }
    public static ExecutionId New() => new(Guid.NewGuid());
}

// NEW: ExecutionDescriptor
public sealed record ExecutionDescriptor(
    ExecutionId Id,
    RuntimeSession Session,
    DateTime StartedAtUtc);
```

### 2.2 Execution State (extends RuntimeState)

```csharp
// NEW: ExecutionState (aligned with RuntimeState but isolated)
public enum ExecutionState
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
```

### 2.3 Execution Result

```csharp
// NEW: ExecutionResult (immutable)
public sealed class ExecutionResult
{
    public ExecutionId ExecutionId { get; }
    public ExecutionState State { get; }
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public bool IsCancelled { get; }
    public string? FailureReason { get; }
    public Exception? Exception { get; }
    public DateTime CompletedAtUtc { get; }
}
```

### 2.4 Hook Contract

```csharp
// NEW: IExecutionHook (interface)
public interface IExecutionHook
{
    ExecutionHookType HookType { get; }
    int Order { get; }
    Task OnBeforeExecutionAsync(ExecutionContext context, CancellationToken ct = default);
    Task OnAfterExecutionAsync(ExecutionContext context, ExecutionResult result, CancellationToken ct = default);
    Task OnExecutionFailedAsync(ExecutionContext context, Exception exception, CancellationToken ct = default);
    Task OnExecutionCancelledAsync(ExecutionContext context, CancellationToken ct = default);
}

// NEW: ExecutionHookType
public enum ExecutionHookType
{
    Before,
    After,
    OnFailure,
    OnCancelled
}
```

### 2.5 Hook Registry

```csharp
// NEW: IHookRegistry (interface)
public interface IHookRegistry
{
    void Register(IExecutionHook hook);
    void Unregister(IExecutionHook hook);
    IReadOnlyList<IExecutionHook> GetHooks(ExecutionHookType type);
    IReadOnlyList<IExecutionHook> GetAllHooks();
}

// NEW: ExecutionHookRegistry (default implementation)
public sealed class ExecutionHookRegistry : IHookRegistry, IDisposable
{
    public void Register(IExecutionHook hook) { }
    public void Unregister(IExecutionHook hook) { }
    public IReadOnlyList<IExecutionHook> GetHooks(ExecutionHookType type) { }
    public IReadOnlyList<IExecutionHook> GetAllHooks() { }
    public void Dispose() { }
}
```

### 2.6 Runtime Events

```csharp
// NEW: IRuntimeEventPublisher (interface)
public interface IRuntimeEventPublisher
{
    void Publish(IRuntimeEvent evt);
    IDisposable Subscribe(IRuntimeEventHandler handler);
}

// NEW: IRuntimeEvent, IRuntimeEventHandler
public interface IRuntimeEvent { }
public interface IRuntimeEventHandler
{
    Task HandleAsync(IRuntimeEvent evt, CancellationToken ct = default);
}

// NEW: Concrete Events
public sealed record ExecutionStartedEvent(ExecutionId Id, RuntimeSession Session) : IRuntimeEvent;
public sealed record ExecutionCompletedEvent(ExecutionId Id, ExecutionResult Result) : IRuntimeEvent;
public sealed record ExecutionFailedEvent(ExecutionId Id, Exception Exception) : IRuntimeEvent;
public sealed record ExecutionCancelledEvent(ExecutionId Id) : IRuntimeEvent;
```

### 2.7 Execution Context

```csharp
// NEW: ExecutionContext (execution-scoped, not session-scoped)
public sealed class ExecutionContext
{
    public ExecutionId Id { get; }
    public RuntimeSession Session { get; }
    public IHookRegistry Hooks { get; }
    public CancellationTokenSource CancellationSource { get; }
    public bool IsCancelled => CancellationSource.IsCancellationRequested;
    
    internal ExecutionContext(RuntimeSession session, IHookRegistry hooks);
    public void Cancel() => CancellationSource.Cancel();
}
```

---

## 3. Integration Points

### 3.1 RuntimeSession Extension

```csharp
// NEW: RuntimeSession.AddExecution() - creates ExecutionContext
public sealed class RuntimeSession
{
    // existing...
    
    // NEW
    public ExecutionContext CreateExecution() { }
    public IReadOnlyList<ExecutionContext> ActiveExecutions { get; }
}
```

### 3.2 IRuntimeLifecycleController Extension

```csharp
public interface IRuntimeLifecycleController
{
    // existing methods...
    
    // NEW: ExecuteAsync - runs execution with hooks
    Task<ExecutionResult> ExecuteAsync(
        ExecutionContext execution,
        Func<ExecutionContext, Task> work,
        CancellationToken ct = default);
}
```

---

## 4. Constraint Matrix

| 约束 | 说明 | 验证 |
|------|------|------|
| C1 | Runtime.Core 不依赖 Runtime.Capability | Test |
| C2 | Execution Hook 不得修改 RuntimeState | Test |
| C3 | Runtime Event 不得修改 RuntimeState | Test |
| C4 | Hook 失败不得阻止 Execution 完成 | Test |
| C5 | Cancellation 优先于 Hook 执行 | Test |
| C6 | Hook Registry 线程安全 | Test |
| C7 | Execution 结果不可变 | Test |
| C8 | ExecutionId 全局唯一 | Test |

---

## 5. Baseline Verification

| 检查项 | 状态 |
|--------|------|
| Runtime.Core 76 tests PASS | 待验证 |
| 新增 API 不破坏现有 Contract | 待验证 |
| Runtime.Core 不依赖 Capability | 待验证 |
| Hook 隔离验证 | 待验证 |

---

**Status:** Baseline DEFINED
**Next:** REQUIREMENT-ANALYSIS.md
