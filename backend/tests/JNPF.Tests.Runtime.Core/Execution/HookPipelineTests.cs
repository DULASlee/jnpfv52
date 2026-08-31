using JNPF.Runtime.Core;
using Xunit;
using JNPFExecCtx = JNPF.Runtime.Core.ExecutionContext;

namespace JNPF.Tests.Runtime.Core.Execution;

public class HookPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoHooks_ShouldComplete()
    {
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("t", "p", "pipe", "user");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);

        var result = await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_WithBeforeHook_ShouldInvokeHook()
    {
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("t", "p", "pipe", "user");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);
        var invoked = false;
        execution.Hooks.Register(new InvokingHook(ExecutionHookType.Before, 0, () => invoked = true));

        await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        Assert.True(invoked);
    }

    [Fact]
    public async Task ExecuteAsync_WithAfterHook_ShouldInvokeHook()
    {
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("t", "p", "pipe", "user");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);
        var invoked = false;
        execution.Hooks.Register(new InvokingHook(ExecutionHookType.After, 0, () => invoked = true));

        await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        Assert.True(invoked);
    }

    [Fact]
    public async Task ExecuteAsync_WithWorkFailure_ShouldInvokeOnFailureHook()
    {
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("t", "p", "pipe", "user");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);
        var invoked = false;
        execution.Hooks.Register(new InvokingHook(ExecutionHookType.OnFailure, 0, () => invoked = true));

        await controller.ExecuteAsync(execution, ctx => throw new InvalidOperationException("Test"));

        Assert.True(invoked);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelledExecution_ShouldInvokeOnCancelledHook()
    {
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("t", "p", "pipe", "user");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);
        var invoked = false;
        execution.Hooks.Register(new InvokingHook(ExecutionHookType.OnCancelled, 0, () => invoked = true));
        execution.Cancel();

        var result = await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        Assert.True(result.IsCancelled);
        Assert.True(invoked);
    }

    [Fact]
    public async Task ExecuteAsync_HookFailure_ShouldNotStopExecution()
    {
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("t", "p", "pipe", "user");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);
        execution.Hooks.Register(new ThrowingHook(ExecutionHookType.Before, 0));

        var result = await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        // Hook failure should not prevent execution completion
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_HooksShouldRespectOrder()
    {
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("t", "p", "pipe", "user");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);
        var order = new List<int>();
        execution.Hooks.Register(new OrderTrackingHook(ExecutionHookType.Before, 2, order));
        execution.Hooks.Register(new OrderTrackingHook(ExecutionHookType.Before, 0, order));
        execution.Hooks.Register(new OrderTrackingHook(ExecutionHookType.Before, 1, order));

        await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        Assert.Equal(3, order.Count);
        Assert.Equal(0, order[0]); // Order 0 first
        Assert.Equal(1, order[1]); // Order 1 second
        Assert.Equal(2, order[2]); // Order 2 third
    }

    [Fact]
    public async Task ExecuteAsync_WithWorkException_ShouldReturnFailedResult()
    {
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("t", "p", "pipe", "user");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);

        var result = await controller.ExecuteAsync(execution, ctx => throw new InvalidOperationException("Test error"));

        Assert.True(result.IsFailure);
        Assert.Equal("Test error", result.FailureReason);
        Assert.NotNull(result.Exception);
    }
}

internal class InvokingHook : IExecutionHook
{
    private readonly Action _action;

    public InvokingHook(ExecutionHookType type, int order, Action action)
    {
        HookType = type;
        Order = order;
        _action = action;
    }

    public ExecutionHookType HookType { get; }
    public int Order { get; }

    public Task OnBeforeExecutionAsync(JNPFExecCtx context, CancellationToken ct = default)
    {
        _action();
        return Task.CompletedTask;
    }
    public Task OnAfterExecutionAsync(JNPFExecCtx context, ExecutionResult result, CancellationToken ct = default)
    {
        _action();
        return Task.CompletedTask;
    }
    public Task OnExecutionFailedAsync(JNPFExecCtx context, Exception exception, CancellationToken ct = default)
    {
        _action();
        return Task.CompletedTask;
    }
    public Task OnExecutionCancelledAsync(JNPFExecCtx context, CancellationToken ct = default)
    {
        _action();
        return Task.CompletedTask;
    }
}

internal class ThrowingHook : IExecutionHook
{
    public ThrowingHook(ExecutionHookType type, int order)
    {
        HookType = type;
        Order = order;
    }

    public ExecutionHookType HookType { get; }
    public int Order { get; }

    public Task OnBeforeExecutionAsync(JNPFExecCtx context, CancellationToken ct = default) =>
        throw new InvalidOperationException("Hook failure");
    public Task OnAfterExecutionAsync(JNPFExecCtx context, ExecutionResult result, CancellationToken ct = default) =>
        throw new InvalidOperationException("Hook failure");
    public Task OnExecutionFailedAsync(JNPFExecCtx context, Exception exception, CancellationToken ct = default) =>
        throw new InvalidOperationException("Hook failure");
    public Task OnExecutionCancelledAsync(JNPFExecCtx context, CancellationToken ct = default) =>
        throw new InvalidOperationException("Hook failure");
}

internal class OrderTrackingHook : IExecutionHook
{
    private readonly List<int> _order;

    public OrderTrackingHook(ExecutionHookType type, int order, List<int> orderList)
    {
        HookType = type;
        Order = order;
        _order = orderList;
    }

    public ExecutionHookType HookType { get; }
    public int Order { get; }

    public Task OnBeforeExecutionAsync(JNPFExecCtx context, CancellationToken ct = default)
    {
        _order.Add(Order);
        return Task.CompletedTask;
    }
    public Task OnAfterExecutionAsync(JNPFExecCtx context, ExecutionResult result, CancellationToken ct = default) =>
        Task.CompletedTask;
    public Task OnExecutionFailedAsync(JNPFExecCtx context, Exception exception, CancellationToken ct = default) =>
        Task.CompletedTask;
    public Task OnExecutionCancelledAsync(JNPFExecCtx context, CancellationToken ct = default) =>
        Task.CompletedTask;
}
