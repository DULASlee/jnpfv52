using JNPF.Runtime.Core;
using Xunit;

namespace JNPF.Tests.Runtime.Core.Execution;

public class ExecutionContextTests
{
    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var sessionId = Guid.NewGuid();

        var context = ExecutionContextFactory.Create(sessionId);

        Assert.NotEqual(ExecutionId.Empty, context.Id);
        Assert.Equal(sessionId, context.SessionId);
    }

    [Fact]
    public void Create_WithHooks_ShouldUseProvidedRegistry()
    {
        var sessionId = Guid.NewGuid();
        var registry = new ExecutionHookRegistry();

        var context = ExecutionContextFactory.Create(sessionId, registry);

        Assert.Same(registry, context.Hooks);
        Assert.Equal(sessionId, context.SessionId);
    }

    [Fact]
    public void Cancel_ShouldSetCancellationRequested()
    {
        var sessionId = Guid.NewGuid();
        var context = ExecutionContextFactory.Create(sessionId);

        Assert.False(context.IsCancellationRequested);

        context.Cancel();

        Assert.True(context.IsCancellationRequested);
    }

    [Fact]
    public void Token_ShouldBeLinkedToCancellationSource()
    {
        var sessionId = Guid.NewGuid();
        var context = ExecutionContextFactory.Create(sessionId);

        Assert.False(context.Token.IsCancellationRequested);

        context.Cancel();

        Assert.True(context.Token.IsCancellationRequested);
    }

    [Fact]
    public void Dispose_ShouldDisposeResources()
    {
        var sessionId = Guid.NewGuid();
        var context = ExecutionContextFactory.Create(sessionId);

        context.Dispose();

        // Should not throw on double dispose
        context.Dispose();
    }

    [Fact]
    public void MultipleCreate_ShouldGenerateUniqueIds()
    {
        var sessionId = Guid.NewGuid();
        var context1 = ExecutionContextFactory.Create(sessionId);
        var context2 = ExecutionContextFactory.Create(sessionId);

        Assert.NotEqual(context1.Id, context2.Id);
    }
}
