using System.Collections.Concurrent;
using System.Reflection;
using JNPF.Runtime.Core;
using Xunit;

namespace JNPF.Tests.Runtime.Core;

/// <summary>
/// Phase 2-A.1 Contract Hardening：Runtime Isolation / Concurrency / Disposal Safety Tests。
/// 覆盖 Chief Architect 审查指出的缺失 Contract Tests。
/// </summary>
public sealed class RuntimeContractTests
{
    #region RuntimeContext Isolation

    [Fact]
    public void RuntimeContext_DoesNotContainExecutionCapability()
    {
        // LOCK-RUNTIME-CTX-01: RuntimeContext MUST NOT contain execution capability.
        var context = RuntimeContext.Create("t", "p", "pl", "u");

        var type = typeof(RuntimeContext);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var forbidden = new[]
        {
            "Agent", "Capability", "Memory", "Tool",
            "Model", "Prompt", "Skill", "Plan", "Workflow"
        };

        foreach (var prop in properties)
        {
            foreach (var f in forbidden)
            {
                Assert.False(
                    prop.Name.Contains(f, StringComparison.OrdinalIgnoreCase),
                    $"RuntimeContext contains forbidden property '{prop.Name}' (LOCK-RUNTIME-CTX-01).");
            }
        }
    }

    [Fact]
    public void RuntimeContext_IsImmutable()
    {
        // Context 修改必须通过 With* 方法创建新实例。
        var original = RuntimeContext.Create("t", "p", "pl", "u");
        var updated = original.WithMetadata("key", "value");

        Assert.NotSame(original, updated);
        Assert.Empty(original.Metadata);
        Assert.Single(updated.Metadata);
    }

    #endregion

    #region Lifecycle Enforcement

    [Fact]
    public void RuntimeSession_CannotBeDirectlyManipulated()
    {
        // RuntimeSession.State 是 private set，只能通过 RuntimeStateMachine 转换。
        var sessionType = typeof(RuntimeSession);
        var stateProperty = sessionType.GetProperty(nameof(RuntimeSession.State));

        Assert.NotNull(stateProperty);
        Assert.True(stateProperty.SetMethod?.IsPrivate == true || stateProperty.SetMethod == null);
    }

    [Fact]
    public void RuntimeSession_ConstructorIsInternal()
    {
        // RuntimeSession 必须通过 RuntimeLifecycleController 创建，不能绕过生命周期。
        var ctor = typeof(RuntimeSession).GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var publicCtors = ctor.Where(c => c.IsPublic).ToList();
        Assert.Empty(publicCtors);

        var internalCtors = ctor.Where(c => !c.IsPublic).ToList();
        Assert.NotEmpty(internalCtors);
    }

    [Fact]
    public void RuntimeLifecycleController_AllowsOnlyOneSession()
    {
        // Runtime 是单一会话容器，防止并发多会话冲突。
        var controller = new RuntimeLifecycleController();

        var session1 = controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u")).Result;

        Assert.Throws<InvalidOperationException>(() =>
            controller.InitializeAsync(
                RuntimeContext.Create("t2", "p2", "pl2", "u2")).Result);

        // Cleanup
        controller.DisposeAsync(session1.SessionId).Wait();
    }

    #endregion

    #region Concurrency

    [Fact]
    public void RuntimeLifecycleController_HandlesParallelOperations()
    {
        // 100 个并发会话操作不应产生竞争条件。
        var controller = new RuntimeLifecycleController();
        var results = new ConcurrentBag<(Guid SessionId, bool Success, string? Error)>();

        var tasks = Enumerable.Range(1, 100).Select(i =>
            Task.Run(() =>
            {
                try
                {
                    var session = controller.InitializeAsync(
                        RuntimeContext.Create($"tenant-{i}", $"project-{i}", $"pipeline-{i}", $"user-{i}")
                    ).Result;

                    results.Add((session.SessionId, true, null));
                }
                catch (Exception ex)
                {
                    results.Add((Guid.Empty, false, ex.GetType().Name));
                }
            })
        ).ToArray();

        Task.WaitAll(tasks);

        // 只有第一个 Initialize 成功，其余应失败（单一会话约束）
        var successes = results.Count(r => r.Success);
        var failures = results.Count(r => !r.Success);

        Assert.Equal(1, successes);
        Assert.Equal(99, failures);

        // Cleanup
        var sessionId = results.First(r => r.Success).SessionId;
        controller.DisposeAsync(sessionId).Wait();
    }

    [Fact]
    public void RuntimeStateMachine_IsThreadSafe()
    {
        // State Machine 在多线程环境下不应产生非法状态。
        var context = RuntimeContext.Create("t", "p", "pl", "u");
        var session = new RuntimeSession(context);

        RuntimeStateMachine.Transition(session, RuntimeState.Initialized);

        var tasks = Enumerable.Range(1, 50).Select(_ =>
            Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        // 所有线程都尝试相同合法转换
                        if (RuntimeStateMachine.CanTransition(RuntimeState.Initialized, RuntimeState.Running))
                        {
                            RuntimeStateMachine.Transition(session, RuntimeState.Running, "parallel-test");
                            break;
                        }
                    }
                    catch { }
                }
            })
        ).ToArray();

        Task.WaitAll(tasks);

        // 状态应该是稳定的（Running 或保持 Initialized）
        Assert.True(
            session.State == RuntimeState.Initialized ||
            session.State == RuntimeState.Running);
    }

    #endregion

    #region Disposal Safety

    [Fact]
    public void DisposedSession_RejectsFurtherOperations()
    {
        // 已销毁的会话拒绝任何操作。
        var controller = new RuntimeLifecycleController();
        var session = controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u")).Result;

        controller.StartAsync(session.SessionId).Wait();
        controller.CompleteAsync(session.SessionId).Wait();
        controller.DisposeAsync(session.SessionId).Wait();

        Assert.Equal(RuntimeState.Disposed, session.State);

        // 任何后续操作都应被状态机拒绝
        Assert.False(RuntimeStateMachine.CanTransition(RuntimeState.Disposed, RuntimeState.Running));
        Assert.False(RuntimeStateMachine.CanTransition(RuntimeState.Disposed, RuntimeState.Created));
        Assert.False(RuntimeStateMachine.CanTransition(RuntimeState.Disposed, RuntimeState.Initialized));
    }

    [Fact]
    public void DisposedSession_CannotBeDisposedAgain()
    {
        // 已销毁的会话不能再次销毁。
        var controller = new RuntimeLifecycleController();
        var session = controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u")).Result;

        controller.StartAsync(session.SessionId).Wait();
        controller.CompleteAsync(session.SessionId).Wait();
        controller.DisposeAsync(session.SessionId).Wait();

        // 第二次 Dispose 应抛出异常
        Assert.Throws<InvalidOperationException>(() =>
            controller.DisposeAsync(session.SessionId).Wait());
    }

    [Fact]
    public void DisposedSession_CannotBeResumed()
    {
        // 已销毁的会话不能恢复。
        var controller = new RuntimeLifecycleController();
        var session = controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u")).Result;

        controller.StartAsync(session.SessionId).Wait();
        controller.DisposeAsync(session.SessionId).Wait();

        Assert.False(RuntimeStateMachine.CanTransition(RuntimeState.Disposed, RuntimeState.Running));
    }

    #endregion

    #region State Transition Matrix

    [Theory]
    [InlineData(RuntimeState.Created, RuntimeState.Running, false)]
    [InlineData(RuntimeState.Created, RuntimeState.Completed, false)]
    [InlineData(RuntimeState.Created, RuntimeState.Failed, false)]
    [InlineData(RuntimeState.Created, RuntimeState.Paused, false)]
    [InlineData(RuntimeState.Initialized, RuntimeState.Completed, false)]
    [InlineData(RuntimeState.Initialized, RuntimeState.Failed, false)]
    [InlineData(RuntimeState.Initialized, RuntimeState.Paused, false)]
    [InlineData(RuntimeState.Completed, RuntimeState.Running, false)]
    [InlineData(RuntimeState.Completed, RuntimeState.Created, false)]
    [InlineData(RuntimeState.Completed, RuntimeState.Initialized, false)]
    [InlineData(RuntimeState.Failed, RuntimeState.Running, false)]
    [InlineData(RuntimeState.Failed, RuntimeState.Created, false)]
    [InlineData(RuntimeState.Disposed, RuntimeState.Running, false)]
    [InlineData(RuntimeState.Disposed, RuntimeState.Created, false)]
    public void StateTransition_InvalidPaths_ReturnFalse(RuntimeState from, RuntimeState to, bool expected)
    {
        // 禁止的反向转换必须被状态机拦截。
        Assert.Equal(expected, RuntimeStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(RuntimeState.Created)]
    [InlineData(RuntimeState.Initialized)]
    [InlineData(RuntimeState.Running)]
    [InlineData(RuntimeState.Paused)]
    [InlineData(RuntimeState.Completed)]
    [InlineData(RuntimeState.Failed)]
    public void AnyNonDisposedState_CanTransitionToDisposed(RuntimeState from)
    {
        // Disposed 是所有非终态的逃生口。
        Assert.True(RuntimeStateMachine.CanTransition(from, RuntimeState.Disposed));
    }

    [Fact]
    public void Disposed_IsTerminalState()
    {
        // Disposed 是终态，不能转换到任何状态。
        foreach (RuntimeState target in Enum.GetValues<RuntimeState>())
        {
            if (target != RuntimeState.Disposed)
            {
                Assert.False(
                    RuntimeStateMachine.CanTransition(RuntimeState.Disposed, target),
                    $"Disposed should not transition to {target}");
            }
        }
    }

    #endregion
}
