# Phase 2-B Implementation Plan

> **Phase:** Section 8 Runtime Foundation — Phase 2-B: Execution Boundary & Hook Pipeline  
> **Date:** 2026-08-31

---

## 1. Implementation Order

```
A. Execution Identity (ExecutionId, ExecutionDescriptor)
    ↓
B. Execution State & Result (ExecutionState, ExecutionResult)
    ↓
C. Hook Contract (IExecutionHook, ExecutionHookType)
    ↓
D. Hook Registry (IHookRegistry, ExecutionHookRegistry)
    ↓
E. Execution Context (ExecutionContext, ExecutionContextFactory)
    ↓
F. Runtime Events (IRuntimeEvent, RuntimeEventPublisher)
    ↓
G. Lifecycle Controller Extension (IRuntimeLifecycleController)
    ↓
H. Integration Tests
```

---

## 2. New Files

### 2.1 Execution/

| 文件 | 行数 | Public API |
|------|------|-----------|
| `ExecutionId.cs` | 50 | `ExecutionId` struct |
| `ExecutionState.cs` | 40 | `ExecutionState` enum |
| `ExecutionDescriptor.cs` | 30 | `ExecutionDescriptor` record |
| `ExecutionResult.cs` | 70 | `ExecutionResult` class |

### 2.2 Hooks/

| 文件 | 行数 | Public API |
|------|------|-----------|
| `ExecutionHookType.cs` | 30 | `ExecutionHookType` enum |
| `IExecutionHook.cs` | 60 | `IExecutionHook` interface |
| `BeforeExecutionHook.cs` | 40 | `BeforeExecutionHook` abstract |
| `AfterExecutionHook.cs` | 40 | `AfterExecutionHook` abstract |
| `IHookRegistry.cs` | 40 | `IHookRegistry` interface |
| `ExecutionHookRegistry.cs` | 100 | `ExecutionHookRegistry` class |

### 2.3 Events/

| 文件 | 行数 | Public API |
|------|------|-----------|
| `IRuntimeEvent.cs` | 20 | `IRuntimeEvent` interface |
| `IRuntimeEventHandler.cs` | 15 | `IRuntimeEventHandler` interface |
| `IRuntimeEventPublisher.cs` | 25 | `IRuntimeEventPublisher` interface |
| `RuntimeEventPublisher.cs` | 120 | `RuntimeEventPublisher` class |
| `ExecutionStartedEvent.cs` | 20 | `ExecutionStartedEvent` record |
| `ExecutionCompletedEvent.cs` | 20 | `ExecutionCompletedEvent` record |
| `ExecutionFailedEvent.cs` | 20 | `ExecutionFailedEvent` record |
| `ExecutionCancelledEvent.cs` | 20 | `ExecutionCancelledEvent` record |

### 2.4 Execution/

| 文件 | 行数 | Public API |
|------|------|-----------|
| `ExecutionContext.cs` | 80 | `ExecutionContext` class |
| `ExecutionContextFactory.cs` | 30 | `ExecutionContextFactory` static |

---

## 3. Modified Files

### 3.1 IRuntimeLifecycleController

```csharp
// NEW members to add
ExecutionContext CreateExecution(Guid sessionId);
ExecutionContext CreateExecution(Guid sessionId, IHookRegistry hookRegistry);
Task<ExecutionResult> ExecuteAsync(
    ExecutionContext execution,
    Func<ExecutionContext, Task> work,
    CancellationToken cancellationToken = default);
```

### 3.2 RuntimeLifecycleController

- Implement new IRuntimeLifecycleController members
- Add ExecutionHookRegistry instance
- Implement ExecuteAsync with Hook pipeline

---

## 4. Test Files

### 4.1 Execution Tests

| 文件 | 覆盖 |
|------|------|
| `ExecutionIdTests.cs` | Equality, New, Empty |
| `ExecutionStateTests.cs` | State transitions |
| `ExecutionResultTests.cs` | Factory methods, Immutability |

### 4.2 Hook Tests

| 文件 | 覆盖 |
|------|------|
| `ExecutionHookRegistryTests.cs` | Register, Unregister, GetHooks, Thread safety |
| `ExecutionHookTests.cs` | Order, Type, All callback types |

### 4.3 Execution Context Tests

| 文件 | 覆盖 |
|------|------|
| `ExecutionContextTests.cs` | Create, Cancel, Dispose |

### 4.4 Event Tests

| 文件 | 覆盖 |
|------|------|
| `RuntimeEventPublisherTests.cs` | Publish, Subscribe, Unsubscribe, Memory leak |
| `ExecutionEventsTests.cs` | Event creation, Payload |

### 4.5 Integration Tests

| 文件 | 覆盖 |
|------|------|
| `ExecutionLifecycleTests.cs` | Full lifecycle with hooks |
| `HookPipelineTests.cs` | Hook order, Before/After/OnFailure |
| `CancellationTests.cs` | Cancellation propagation |
| `ConcurrencyTests.cs` | Parallel registration |

---

## 5. Dependencies

### 5.1 Project Reference

```
JNPF.Runtime.Core
    ↑
    |
[No new dependencies]
```

### 5.2 NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| (none) | — | 仅使用 .NET 8 内置类型 |

---

## 6. API Verification

### 6.1 Public API Surface (v0.2)

| Type | Visibility | New in v0.2 |
|------|-----------|-------------|
| `ExecutionId` | public | ✅ |
| `ExecutionState` | public | ✅ |
| `ExecutionDescriptor` | public | ✅ |
| `ExecutionResult` | public | ✅ |
| `ExecutionHookType` | public | ✅ |
| `IExecutionHook` | public | ✅ |
| `BeforeExecutionHook` | public | ✅ |
| `AfterExecutionHook` | public | ✅ |
| `IHookRegistry` | public | ✅ |
| `ExecutionHookRegistry` | internal | ✅ |
| `ExecutionContext` | public | ✅ |
| `ExecutionContextFactory` | public | ✅ |
| `IRuntimeEvent` | public | ✅ |
| `IRuntimeEventHandler` | public | ✅ |
| `IRuntimeEventPublisher` | public | ✅ |
| `RuntimeEventPublisher` | internal | ✅ |
| `ExecutionStartedEvent` | public | ✅ |
| `ExecutionCompletedEvent` | public | ✅ |
| `ExecutionFailedEvent` | public | ✅ |
| `ExecutionCancelledEvent` | public | ✅ |

---

## 7. Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Hook 循环调用 | Hook 不得启动新的 Execution |
| Hook 死锁 | Hook 不得在持有锁时 await |
| Event 内存泄漏 | 使用 WeakReference |
| Cancellation 竞态 | CancellationTokenSource 正确管理 |

---

## 8. Deferred Items

| Item | Reason | Target Phase |
|------|--------|--------------|
| Distributed Execution | MVP Scope | Phase 2-C+ |
| Execution Persistence | MVP Scope | Phase 2-C+ |
| Hook Timeout Policy | 需要更多数据 | Phase 2-C+ |
| Execution Priority | MVP Scope | Phase 2-C+ |

---

## 9. Success Criteria

| Criteria | Verification |
|----------|--------------|
| All 76 existing tests PASS | `dotnet test` |
| New tests ≥ 50 | Test count |
| Build succeeds | `dotnet build` |
| Runtime.Core ↔ Capability isolation | Dependency check |
| No new public API without justification | Code review |

---

**Status:** Plan COMPLETED
**Next:** TDD → Implementation
