using JNPF.Runtime.Core;
using Xunit;
using JNPFExecCtx = JNPF.Runtime.Core.ExecutionContext;

namespace JNPF.Tests.Runtime.Core.Execution;

public class HookRegistryTests
{
    [Fact]
    public void Register_ShouldAddHook()
    {
        var registry = new ExecutionHookRegistry();
        var hook = new TestHook(ExecutionHookType.Before, 0);

        registry.Register(hook);

        Assert.False(registry.IsEmpty);
        var hooks = registry.GetHooks(ExecutionHookType.Before);
        Assert.Contains(hook, hooks);
    }

    [Fact]
    public void Unregister_ShouldRemoveHook()
    {
        var registry = new ExecutionHookRegistry();
        var hook = new TestHook(ExecutionHookType.Before, 0);
        registry.Register(hook);

        registry.Unregister(hook);

        Assert.True(registry.IsEmpty);
    }

    [Fact]
    public void GetHooks_ShouldReturnOrderedByOrder()
    {
        var registry = new ExecutionHookRegistry();
        var hook1 = new TestHook(ExecutionHookType.Before, 2);
        var hook2 = new TestHook(ExecutionHookType.Before, 0);
        var hook3 = new TestHook(ExecutionHookType.Before, 1);
        registry.Register(hook1);
        registry.Register(hook2);
        registry.Register(hook3);

        var hooks = registry.GetHooks(ExecutionHookType.Before);

        Assert.Equal(3, hooks.Count);
        Assert.Same(hook2, hooks[0]); // Order 0
        Assert.Same(hook3, hooks[1]); // Order 1
        Assert.Same(hook1, hooks[2]); // Order 2
    }

    [Fact]
    public void GetHooks_ShouldReturnOnlyMatchingType()
    {
        var registry = new ExecutionHookRegistry();
        var beforeHook = new TestHook(ExecutionHookType.Before, 0);
        var afterHook = new TestHook(ExecutionHookType.After, 0);
        registry.Register(beforeHook);
        registry.Register(afterHook);

        var beforeHooks = registry.GetHooks(ExecutionHookType.Before);

        Assert.Single(beforeHooks);
        Assert.Same(beforeHook, beforeHooks[0]);
    }

    [Fact]
    public void GetAllHooks_ShouldReturnAll()
    {
        var registry = new ExecutionHookRegistry();
        registry.Register(new TestHook(ExecutionHookType.Before, 0));
        registry.Register(new TestHook(ExecutionHookType.After, 0));
        registry.Register(new TestHook(ExecutionHookType.OnFailure, 0));

        var all = registry.GetAllHooks();

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void Register_Null_ShouldThrow()
    {
        var registry = new ExecutionHookRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void Unregister_Null_ShouldThrow()
    {
        var registry = new ExecutionHookRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Unregister(null!));
    }

    [Fact]
    public void DuplicateRegister_ShouldNotDuplicate()
    {
        var registry = new ExecutionHookRegistry();
        var hook = new TestHook(ExecutionHookType.Before, 0);
        registry.Register(hook);
        registry.Register(hook); // Second register should be no-op

        var hooks = registry.GetHooks(ExecutionHookType.Before);

        Assert.Single(hooks);
    }

    [Fact]
    public void Dispose_ShouldClearHooks()
    {
        var registry = new ExecutionHookRegistry();
        registry.Register(new TestHook(ExecutionHookType.Before, 0));
        registry.Register(new TestHook(ExecutionHookType.After, 0));

        registry.Dispose();

        Assert.True(registry.IsEmpty);
    }

    [Fact]
    public void Register_AfterDispose_ShouldThrow()
    {
        var registry = new ExecutionHookRegistry();
        registry.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            registry.Register(new TestHook(ExecutionHookType.Before, 0)));
    }
}

internal class TestHook : IExecutionHook
{
    public TestHook(ExecutionHookType type, int order)
    {
        HookType = type;
        Order = order;
    }

    public ExecutionHookType HookType { get; }
    public int Order { get; }

    public Task OnBeforeExecutionAsync(JNPFExecCtx context, CancellationToken ct = default) => Task.CompletedTask;
    public Task OnAfterExecutionAsync(JNPFExecCtx context, ExecutionResult result, CancellationToken ct = default) => Task.CompletedTask;
    public Task OnExecutionFailedAsync(JNPFExecCtx context, Exception exception, CancellationToken ct = default) => Task.CompletedTask;
    public Task OnExecutionCancelledAsync(JNPFExecCtx context, CancellationToken ct = default) => Task.CompletedTask;
}
