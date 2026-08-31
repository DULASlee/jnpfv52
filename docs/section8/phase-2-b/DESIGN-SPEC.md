# Phase 2-B Design Specification

> **Phase:** Section 8 Runtime Foundation — Phase 2-B: Execution Boundary & Hook Pipeline  
> **Date:** 2026-08-31

---

## 1. Design Goals

1. **最小完整:** 提供 Execution Boundary 最小公共契约
2. **隔离保证:** Runtime.Core 与 Capability 完全隔离
3. **可观察:** Hook + Event 提供运行时可见性
4. **可扩展:** Extension Point 不污染 Kernel

---

## 2. Execution Identity

### 2.1 ExecutionId

```csharp
/// <summary>
/// Execution 唯一标识。
/// </summary>
public readonly struct ExecutionId : IEquatable<ExecutionId>
{
    public Guid Value { get; }
    
    public ExecutionId(Guid value) => Value = value;
    
    public static ExecutionId New() => new(Guid.NewGuid());
    
    public static ExecutionId Empty => default;
    
    public bool Equals(ExecutionId other) => Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is ExecutionId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
    
    public static bool operator ==(ExecutionId left, ExecutionId right) => left.Equals(right);
    public static bool operator !=(ExecutionId left, ExecutionId right) => !left.Equals(right);
}
```

### 2.2 ExecutionDescriptor

```csharp
/// <summary>
/// Execution 的只读描述符。
/// </summary>
public sealed record ExecutionDescriptor(
    ExecutionId Id,
    Guid SessionId,
    DateTime CreatedAtUtc,
    ExecutionState State)
{
    public static ExecutionDescriptor Create(ExecutionId id, Guid sessionId) =>
        new(id, sessionId, DateTime.UtcNow, ExecutionState.Pending);
}
```

---

## 3. Execution State

### 3.1 ExecutionState Enum

```csharp
/// <summary>
/// Execution 生命周期状态。
/// 
/// 注意：此枚举独立于 RuntimeState，两者正交。
/// </summary>
public enum ExecutionState
{
    /// <summary>
    /// 已创建，等待执行。
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// 执行中。
    /// </summary>
    Running = 1,
    
    /// <summary>
    /// 正常完成。
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// 执行失败。
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// 被取消。
    /// </summary>
    Cancelled = 4
}
```

### 3.2 Execution State Transitions

```
Pending
    ↓
Running
    ↓
Completed / Failed / Cancelled
```

**约束:**
- Pending → Running: 由 ExecuteAsync 发起
- Running → Completed: work 正常返回
- Running → Failed: work 抛出异常
- Running → Cancelled: CancellationToken 触发
- 所有终态不可转换到其他状态

---

## 4. Execution Result

### 4.1 ExecutionResult

```csharp
/// <summary>
/// Execution 执行结果（不可变）。
/// </summary>
public sealed class ExecutionResult
{
    public ExecutionId ExecutionId { get; }
    public ExecutionState State { get; }
    public bool IsSuccess => State == ExecutionState.Completed;
    public bool IsFailure => State == ExecutionState.Failed;
    public bool IsCancelled => State == ExecutionState.Cancelled;
    public string? FailureReason { get; }
    public Exception? Exception { get; }
    public DateTime CompletedAtUtc { get; }
    public TimeSpan Duration { get; }
    
    private ExecutionResult(
        ExecutionId executionId,
        ExecutionState state,
        string? failureReason,
        Exception? exception,
        DateTime completedAtUtc,
        TimeSpan duration)
    {
        ExecutionId = executionId;
        State = state;
        FailureReason = failureReason;
        Exception = exception;
        CompletedAtUtc = completedAtUtc;
        Duration = duration;
    }
    
    public static ExecutionResult Success(ExecutionId id, TimeSpan duration) =>
        new(id, ExecutionState.Completed, null, null, DateTime.UtcNow, duration);
    
    public static ExecutionResult Failure(ExecutionId id, string reason, Exception? ex, TimeSpan duration) =>
        new(id, ExecutionState.Failed, reason, ex, DateTime.UtcNow, duration);
    
    public static ExecutionResult Cancelled(ExecutionId id, TimeSpan duration) =>
        new(id, ExecutionState.Cancelled, null, null, DateTime.UtcNow, duration);
}
```

---

## 5. Hook Contract

### 5.1 ExecutionHookType

```csharp
/// <summary>
/// Hook 执行时机。
/// </summary>
public enum ExecutionHookType
{
    /// <summary>
    /// 执行前。
    /// </summary>
    Before = 0,
    
    /// <summary>
    /// 执行后（无论成功或失败）。
    /// </summary>
    After = 1,
    
    /// <summary>
    /// 执行失败时。
    /// </summary>
    OnFailure = 2,
    
    /// <summary>
    /// 执行取消时。
    /// </summary>
    OnCancelled = 3
}
```

### 5.2 IExecutionHook

```csharp
/// <summary>
/// Execution Hook 接口。
/// 
/// 约束：
///   - Hook 不得修改 RuntimeState
///   - Hook 失败不阻止 Execution（除非配置 fail-fast）
///   - Hook 按 Order 升序执行
/// </summary>
public interface IExecutionHook
{
    /// <summary>
    /// Hook 类型。
    /// </summary>
    ExecutionHookType HookType { get; }
    
    /// <summary>
    /// 执行顺序（升序，越小越先执行）。
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// 执行前回调。
    /// </summary>
    Task OnBeforeExecutionAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行后回调。
    /// </summary>
    Task OnAfterExecutionAsync(
        ExecutionContext context,
        ExecutionResult result,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行失败回调。
    /// </summary>
    Task OnExecutionFailedAsync(
        ExecutionContext context,
        Exception exception,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行取消回调。
    /// </summary>
    Task OnExecutionCancelledAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default);
}
```

### 5.3 IExecutionHook Implementations

```csharp
/// <summary>
/// Base class for Before hooks.
/// </summary>
public abstract class BeforeExecutionHook : IExecutionHook
{
    public abstract int Order { get; }
    public ExecutionHookType HookType => ExecutionHookType.Before;
    
    public virtual Task OnBeforeExecutionAsync(
        ExecutionContext context,
        CancellationToken ct = default) => Task.CompletedTask;
    
    public virtual Task OnAfterExecutionAsync(
        ExecutionContext context,
        ExecutionResult result,
        CancellationToken ct = default) => Task.CompletedTask;
    
    public virtual Task OnExecutionFailedAsync(
        ExecutionContext context,
        Exception exception,
        CancellationToken ct = default) => Task.CompletedTask;
    
    public virtual Task OnExecutionCancelledAsync(
        ExecutionContext context,
        CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>
/// Base class for After hooks.
/// </summary>
public abstract class AfterExecutionHook : IExecutionHook
{
    public abstract int Order { get; }
    public ExecutionHookType HookType => ExecutionHookType.After;
    
    public virtual Task OnBeforeExecutionAsync(
        ExecutionContext context,
        CancellationToken ct = default) => Task.CompletedTask;
    
    public virtual Task OnAfterExecutionAsync(
        ExecutionContext context,
        ExecutionResult result,
        CancellationToken ct = default) => Task.CompletedTask;
    
    public virtual Task OnExecutionFailedAsync(
        ExecutionContext context,
        Exception exception,
        CancellationToken ct = default) => Task.CompletedTask;
    
    public virtual Task OnExecutionCancelledAsync(
        ExecutionContext context,
        CancellationToken ct = default) => Task.CompletedTask;
}
```

---

## 6. Hook Registry

### 6.1 IHookRegistry

```csharp
/// <summary>
/// Hook 注册表接口。
/// 
/// 约束：
///   - 线程安全
///   - 按 HookType + Order 排序
/// </summary>
public interface IHookRegistry
{
    /// <summary>
    /// 注册 Hook。
    /// </summary>
    void Register(IExecutionHook hook);
    
    /// <summary>
    /// 注销 Hook。
    /// </summary>
    void Unregister(IExecutionHook hook);
    
    /// <summary>
    /// 获取指定类型的 Hook（已排序）。
    /// </summary>
    IReadOnlyList<IExecutionHook> GetHooks(ExecutionHookType type);
    
    /// <summary>
    /// 获取所有 Hook。
    /// </summary>
    IReadOnlyList<IExecutionHook> GetAllHooks();
    
    /// <summary>
    /// 是否为空。
    /// </summary>
    bool IsEmpty { get; }
}
```

### 6.2 ExecutionHookRegistry

```csharp
/// <summary>
/// Hook 注册表默认实现。
/// 
/// 线程安全：ConcurrentDictionary + ReaderWriterLockSlim
/// </summary>
public sealed class ExecutionHookRegistry : IHookRegistry, IDisposable
{
    private readonly ConcurrentDictionary<IExecutionHook, byte> _hooks = new();
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private bool _disposed;
    
    public void Register(IExecutionHook hook)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(hook);
        
        _hooks.TryAdd(hook, 0);
    }
    
    public void Unregister(IExecutionHook hook)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(hook);
        
        _hooks.TryRemove(hook, out _);
    }
    
    public IReadOnlyList<IExecutionHook> GetHooks(ExecutionHookType type)
    {
        ThrowIfDisposed();
        
        _lock.EnterReadLock();
        try
        {
            return _hooks.Keys
                .Where(h => h.HookType == type)
                .OrderBy(h => h.Order)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public IReadOnlyList<IExecutionHook> GetAllHooks()
    {
        ThrowIfDisposed();
        
        _lock.EnterReadLock();
        try
        {
            return _hooks.Keys
                .OrderBy(h => h.HookType)
                .ThenBy(h => h.Order)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public bool IsEmpty => _hooks.IsEmpty;
    
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ExecutionHookRegistry));
    }
    
    public void Dispose()
    {
        _disposed = true;
        _hooks.Clear();
        _lock.Dispose();
    }
}
```

---

## 7. Execution Context

### 7.1 ExecutionContext

```csharp
/// <summary>
/// Execution 执行上下文。
/// 
/// 约束：
///   - 不得包含业务数据
///   - 不得包含 Model/Prompt/Plan
///   - CancellationToken 绑定到此 Context
/// </summary>
public sealed class ExecutionContext : IDisposable
{
    public ExecutionId Id { get; }
    public Guid SessionId { get; }
    public IHookRegistry Hooks { get; }
    public CancellationTokenSource CancellationSource { get; }
    
    /// <summary>
    /// 是否已请求取消。
    /// </summary>
    public bool IsCancellationRequested => CancellationSource.IsCancellationRequested;
    
    /// <summary>
    /// 关联的 CancellationToken。
    /// </summary>
    public CancellationToken Token => CancellationSource.Token;
    
    internal ExecutionContext(
        ExecutionId id,
        Guid sessionId,
        IHookRegistry hooks)
    {
        Id = id;
        SessionId = sessionId;
        Hooks = hooks ?? new ExecutionHookRegistry();
        CancellationSource = new CancellationTokenSource();
    }
    
    /// <summary>
    /// 请求取消 Execution。
    /// </summary>
    public void Cancel()
    {
        CancellationSource.Cancel();
    }
    
    public void Dispose()
    {
        CancellationSource.Dispose();
        if (Hooks is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
```

### 7.2 ExecutionContext Factory

```csharp
/// <summary>
/// ExecutionContext 工厂。
/// </summary>
public static class ExecutionContextFactory
{
    public static ExecutionContext Create(Guid sessionId)
    {
        var id = ExecutionId.New();
        return new ExecutionContext(id, sessionId, new ExecutionHookRegistry());
    }
    
    public static ExecutionContext Create(Guid sessionId, IHookRegistry hooks)
    {
        ArgumentNullException.ThrowIfNull(hooks);
        
        var id = ExecutionId.New();
        return new ExecutionContext(id, sessionId, hooks);
    }
}
```

---

## 8. Runtime Events

### 8.1 Event Interface

```csharp
/// <summary>
/// Runtime 事件接口。
/// 
/// 约束：
///   - 事件不得修改 RuntimeState
///   - 事件是 fire-and-forget
/// </summary>
public interface IRuntimeEvent
{
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Runtime 事件处理器。
/// </summary>
public interface IRuntimeEventHandler
{
    Task HandleAsync(IRuntimeEvent evt, CancellationToken ct = default);
}
```

### 8.2 Execution Events

```csharp
/// <summary>
/// Execution 开始事件。
/// </summary>
public sealed record ExecutionStartedEvent(
    ExecutionId ExecutionId,
    Guid SessionId,
    DateTime OccurredAtUtc) : IRuntimeEvent
{
    public static ExecutionStartedEvent Create(ExecutionId id, Guid sessionId) =>
        new(id, sessionId, DateTime.UtcNow);
}

/// <summary>
/// Execution 完成事件。
/// </summary>
public sealed record ExecutionCompletedEvent(
    ExecutionId ExecutionId,
    ExecutionResult Result,
    DateTime OccurredAtUtc) : IRuntimeEvent
{
    public static ExecutionCompletedEvent Create(ExecutionId id, ExecutionResult result) =>
        new(id, result, DateTime.UtcNow);
}

/// <summary>
/// Execution 失败事件。
/// </summary>
public sealed record ExecutionFailedEvent(
    ExecutionId ExecutionId,
    Exception Exception,
    DateTime OccurredAtUtc) : IRuntimeEvent
{
    public static ExecutionFailedEvent Create(ExecutionId id, Exception ex) =>
        new(id, ex, DateTime.UtcNow);
}

/// <summary>
/// Execution 取消事件。
/// </summary>
public sealed record ExecutionCancelledEvent(
    ExecutionId ExecutionId,
    DateTime OccurredAtUtc) : IRuntimeEvent
{
    public static ExecutionCancelledEvent Create(ExecutionId id) =>
        new(id, DateTime.UtcNow);
}
```

### 8.3 IRuntimeEventPublisher

```csharp
/// <summary>
/// Runtime 事件发布者。
/// </summary>
public interface IRuntimeEventPublisher
{
    /// <summary>
    /// 发布事件。
    /// </summary>
    void Publish(IRuntimeEvent evt);
    
    /// <summary>
    /// 订阅事件。
    /// </summary>
    IDisposable Subscribe(IRuntimeEventHandler handler);
    
    /// <summary>
    /// 订阅指定类型的事件。
    /// </summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IRuntimeEvent;
}
```

### 8.4 RuntimeEventPublisher

```csharp
/// <summary>
/// Runtime 事件发布者默认实现。
/// 
/// 使用弱引用存储订阅者，避免内存泄漏。
/// </summary>
public sealed class RuntimeEventPublisher : IRuntimeEventPublisher, IDisposable
{
    private readonly object _lock = new();
    private readonly List<WeakReference<IRuntimeEventHandler>> _handlers = new();
    private bool _disposed;
    
    public void Publish(IRuntimeEvent evt)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(evt);
        
        List<IRuntimeEventHandler> handlersToNotify;
        
        lock (_lock)
        {
            CleanupDeadHandlers();
            handlersToNotify = _handlers
                .Select(w => w.GetTarget())
                .Where(h => h != null)
                .Cast<IRuntimeEventHandler>()
                .ToList();
        }
        
        // Fire-and-forget，异常不传播
        foreach (var handler in handlersToNotify)
        {
            _ = Task.Run(() => FireAndForget(handler, evt));
        }
    }
    
    public IDisposable Subscribe(IRuntimeEventHandler handler)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(handler);
        
        lock (_lock)
        {
            _handlers.Add(new WeakReference<IRuntimeEventHandler>(handler));
        }
        
        return new UnsubscribeHandle(this, handler);
    }
    
    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IRuntimeEvent
    {
        return Subscribe(new TypedEventHandler<TEvent>(handler));
    }
    
    private async Task FireAndForget(IRuntimeEventHandler handler, IRuntimeEvent evt)
    {
        try
        {
            await handler.HandleAsync(evt, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Swallow exceptions in event handlers
        }
    }
    
    private void CleanupDeadHandlers()
    {
        _handlers.RemoveAll(w => !w.TryGetTarget(out _));
    }
    
    private void Unsubscribe(IRuntimeEventHandler handler)
    {
        lock (_lock)
        {
            _handlers.RemoveAll(w =>
            {
                if (w.TryGetTarget(out var target))
                {
                    return ReferenceEquals(target, handler);
                }
                return true;
            });
        }
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RuntimeEventPublisher));
    }
    
    public void Dispose()
    {
        _disposed = true;
        lock (_lock)
        {
            _handlers.Clear();
        }
    }
    
    private sealed class UnsubscribeHandle : IDisposable
    {
        private readonly RuntimeEventPublisher _publisher;
        private readonly IRuntimeEventHandler _handler;
        
        public UnsubscribeHandle(RuntimeEventPublisher publisher, IRuntimeEventHandler handler)
        {
            _publisher = publisher;
            _handler = handler;
        }
        
        public void Dispose() => _publisher.Unsubscribe(_handler);
    }
    
    private sealed class TypedEventHandler<TEvent> : IRuntimeEventHandler where TEvent : IRuntimeEvent
    {
        private readonly Func<TEvent, Task> _handler;
        
        public TypedEventHandler(Func<TEvent, Task> handler) => _handler = handler;
        
        public Task HandleAsync(IRuntimeEvent evt, CancellationToken ct = default)
        {
            if (evt is TEvent typed)
            {
                return _handler(typed);
            }
            return Task.CompletedTask;
        }
    }
}
```

---

## 9. IRuntimeLifecycleController Extension

### 9.1 New Interface Members

```csharp
/// <summary>
/// Extension: Execution Boundary
/// </summary>
public interface IRuntimeLifecycleController
{
    // === Existing Members ===
    RuntimeSession? CurrentSession { get; }
    Task<RuntimeSession> InitializeAsync(RuntimeContext context, CancellationToken ct = default);
    Task StartAsync(Guid sessionId, CancellationToken ct = default);
    Task PauseAsync(Guid sessionId, CancellationToken ct = default);
    Task ResumeAsync(Guid sessionId, CancellationToken ct = default);
    Task CompleteAsync(Guid sessionId, CancellationToken ct = default);
    Task FailAsync(Guid sessionId, string reason, CancellationToken ct = default);
    Task DisposeAsync(Guid sessionId, CancellationToken ct = default);
    
    // === NEW: Execution Methods ===
    
    /// <summary>
    /// 创建 ExecutionContext。
    /// </summary>
    ExecutionContext CreateExecution(Guid sessionId);
    
    /// <summary>
    /// 创建带有自定义 HookRegistry 的 ExecutionContext。
    /// </summary>
    ExecutionContext CreateExecution(Guid sessionId, IHookRegistry hookRegistry);
    
    /// <summary>
    /// 执行工作单元，自动管理 Hook 和 Event。
    /// </summary>
    Task<ExecutionResult> ExecuteAsync(
        ExecutionContext execution,
        Func<ExecutionContext, Task> work,
        CancellationToken cancellationToken = default);
}
```

---

## 10. File Structure

```
JNPF.Runtime.Core/
├── Execution/
│   ├── ExecutionId.cs
│   ├── ExecutionState.cs
│   ├── ExecutionDescriptor.cs
│   ├── ExecutionResult.cs
│   ├── ExecutionContext.cs
│   ├── ExecutionContextFactory.cs
│   └── IExecutionContextFactory.cs
├── Hooks/
│   ├── IExecutionHook.cs
│   ├── ExecutionHookType.cs
│   ├── BeforeExecutionHook.cs
│   ├── AfterExecutionHook.cs
│   ├── IHookRegistry.cs
│   └── ExecutionHookRegistry.cs
├── Events/
│   ├── IRuntimeEvent.cs
│   ├── IRuntimeEventHandler.cs
│   ├── IRuntimeEventPublisher.cs
│   ├── RuntimeEventPublisher.cs
│   ├── ExecutionStartedEvent.cs
│   ├── ExecutionCompletedEvent.cs
│   ├── ExecutionFailedEvent.cs
│   └── ExecutionCancelledEvent.cs
└── [Existing Files...]
```

---

## 11. Design Pre-Gate Checklist

| Check | Status |
|-------|--------|
| Contract completeness | ✅ |
| Layer boundary | ✅ Runtime.Core 不依赖 Capability |
| Lifecycle correctness | ✅ |
| Concurrency | ✅ ReaderWriterLockSlim |
| Cancellation | ✅ CancellationToken |
| Failure semantics | ✅ Hook 失败不阻止 Execution |
| API surface | ✅ |
| Extensibility | ✅ IHookRegistry, IRuntimeEventPublisher |
| Testability | ✅ 全部可 Mock |
| Compatibility | ✅ 不破坏现有 API |

---

**Status:** Design COMPLETED
**Next:** IMPLEMENTATION-PLAN.md → TDD → Implementation
