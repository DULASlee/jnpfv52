using JNPF.Runtime.Core;
using Xunit;
using JNPFExecCtx = JNPF.Runtime.Core.ExecutionContext;

namespace JNPF.Tests.Runtime.Core.Execution;

public class IntegrationTests
{
    [Fact]
    public async Task FullLifecycle_ShouldWork()
    {
        // Arrange
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("tenant-1", "project-1", "pipeline-1", "user-1");

        // Initialize Session
        var session = await controller.InitializeAsync(context);

        // Create Execution
        var execution = controller.CreateExecution(session.SessionId);

        // Verify Execution
        Assert.NotEqual(ExecutionId.Empty, execution.Id);
        Assert.Equal(session.SessionId, execution.SessionId);
        Assert.False(execution.IsCancellationRequested);

        // Execute
        var result = await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        // Verify Result
        Assert.True(result.IsSuccess);
        Assert.Equal(execution.Id, result.ExecutionId);
    }

    [Fact]
    public async Task ExecutionWithHooks_ShouldInvokeAllHooks()
    {
        // Arrange
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("tenant-1", "project-1", "pipeline-1", "user-1");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);

        var invocationOrder = new List<string>();
        execution.Hooks.Register(new NamedHook(ExecutionHookType.Before, 0, "before", invocationOrder));
        execution.Hooks.Register(new NamedHook(ExecutionHookType.After, 0, "after", invocationOrder));

        // Execute
        await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        // Verify
        Assert.Contains("before", invocationOrder);
        Assert.Contains("after", invocationOrder);
        Assert.Equal("before", invocationOrder[0]); // Before before After
    }

    [Fact]
    public async Task ExecutionWithException_ShouldReturnFailure()
    {
        // Arrange
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("tenant-1", "project-1", "pipeline-1", "user-1");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);

        // Execute with exception
        var result = await controller.ExecuteAsync(execution,
            ctx => throw new InvalidOperationException("Test failure"));

        // Verify
        Assert.True(result.IsFailure);
        Assert.Equal("Test failure", result.FailureReason);
    }

    [Fact]
    public async Task ExecutionWithCancellation_ShouldReturnCancelled()
    {
        // Arrange
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("tenant-1", "project-1", "pipeline-1", "user-1");
        var session = await controller.InitializeAsync(context);
        var execution = controller.CreateExecution(session.SessionId);

        // Cancel before execution
        execution.Cancel();

        // Execute
        var result = await controller.ExecuteAsync(execution, ctx => Task.CompletedTask);

        // Verify
        Assert.True(result.IsCancelled);
    }

    [Fact]
    public async Task MultipleExecutions_ShouldBeIndependent()
    {
        // Arrange
        var controller = new RuntimeLifecycleController();
        var context = RuntimeContext.Create("tenant-1", "project-1", "pipeline-1", "user-1");
        var session = await controller.InitializeAsync(context);

        var execution1 = controller.CreateExecution(session.SessionId);
        var execution2 = controller.CreateExecution(session.SessionId);

        // Execute both
        var result1 = await controller.ExecuteAsync(execution1, ctx => Task.CompletedTask);
        var result2 = await controller.ExecuteAsync(execution2, ctx => Task.CompletedTask);

        // Verify
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(execution1.Id, execution2.Id);
    }
}

internal class NamedHook : IExecutionHook
{
    private readonly string _name;
    private readonly List<string> _order;

    public NamedHook(ExecutionHookType type, int order, string name, List<string> orderList)
    {
        HookType = type;
        Order = order;
        _name = name;
        _order = orderList;
    }

    public ExecutionHookType HookType { get; }
    public int Order { get; }

    public Task OnBeforeExecutionAsync(JNPFExecCtx context, CancellationToken ct = default)
    {
        _order.Add(_name);
        return Task.CompletedTask;
    }
    public Task OnAfterExecutionAsync(JNPFExecCtx context, ExecutionResult result, CancellationToken ct = default)
    {
        _order.Add(_name);
        return Task.CompletedTask;
    }
    public Task OnExecutionFailedAsync(JNPFExecCtx context, Exception exception, CancellationToken ct = default) =>
        Task.CompletedTask;
    public Task OnExecutionCancelledAsync(JNPFExecCtx context, CancellationToken ct = default) =>
        Task.CompletedTask;
}
