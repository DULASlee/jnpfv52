using JNPF.Runtime.Core;
using Xunit;

namespace JNPF.Tests.Runtime.Core;

/// <summary>
/// Phase 2-A 验收测试：RuntimeStateMachine 合法转换与非法转换拦截。
/// </summary>
public sealed class RuntimeStateMachineTests
{
    [Theory]
    [InlineData(RuntimeState.Created, RuntimeState.Initialized, true)]
    [InlineData(RuntimeState.Initialized, RuntimeState.Running, true)]
    [InlineData(RuntimeState.Running, RuntimeState.Paused, true)]
    [InlineData(RuntimeState.Running, RuntimeState.Completed, true)]
    [InlineData(RuntimeState.Running, RuntimeState.Failed, true)]
    [InlineData(RuntimeState.Paused, RuntimeState.Running, true)]
    [InlineData(RuntimeState.Paused, RuntimeState.Completed, true)]
    [InlineData(RuntimeState.Paused, RuntimeState.Failed, true)]
    [InlineData(RuntimeState.Completed, RuntimeState.Disposed, true)]
    [InlineData(RuntimeState.Failed, RuntimeState.Disposed, true)]
    public void CanTransition_ValidPath_ReturnsTrue(RuntimeState from, RuntimeState to, bool expected)
    {
        Assert.Equal(expected, RuntimeStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(RuntimeState.Created, RuntimeState.Running)]
    [InlineData(RuntimeState.Created, RuntimeState.Completed)]
    [InlineData(RuntimeState.Created, RuntimeState.Failed)]
    [InlineData(RuntimeState.Initialized, RuntimeState.Completed)]
    [InlineData(RuntimeState.Completed, RuntimeState.Running)]
    [InlineData(RuntimeState.Failed, RuntimeState.Running)]
    [InlineData(RuntimeState.Disposed, RuntimeState.Created)]
    [InlineData(RuntimeState.Disposed, RuntimeState.Running)]
    public void CanTransition_InvalidPath_ReturnsFalse(RuntimeState from, RuntimeState to)
    {
        Assert.False(RuntimeStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(RuntimeState.Created, RuntimeState.Disposed)]
    [InlineData(RuntimeState.Initialized, RuntimeState.Disposed)]
    [InlineData(RuntimeState.Running, RuntimeState.Disposed)]
    [InlineData(RuntimeState.Paused, RuntimeState.Disposed)]
    [InlineData(RuntimeState.Completed, RuntimeState.Disposed)]
    [InlineData(RuntimeState.Failed, RuntimeState.Disposed)]
    public void CanTransition_ToDisposedFromAnyNonDisposed_ReturnsTrue(RuntimeState from, RuntimeState to)
    {
        Assert.True(RuntimeStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void Transition_ValidPath_UpdatesSessionState()
    {
        // Arrange
        var context = RuntimeContext.Create("t", "p", "pl", "u");
        var session = new RuntimeSession(context);

        // Act
        RuntimeStateMachine.Transition(session, RuntimeState.Initialized, "test");

        // Assert
        Assert.Equal(RuntimeState.Initialized, session.State);
        Assert.Equal("test", session.StateReason);
        Assert.NotEqual(default, session.StateChangedAtUtc);
    }

    [Fact]
    public void Transition_InvalidPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = RuntimeContext.Create("t", "p", "pl", "u");
        var session = new RuntimeSession(context);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            RuntimeStateMachine.Transition(session, RuntimeState.Running, "illegal"));
        Assert.Contains("Invalid state transition", ex.Message);
    }

    [Fact]
    public void Transition_FromDisposed_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = RuntimeContext.Create("t", "p", "pl", "u");
        var session = new RuntimeSession(context);
        RuntimeStateMachine.Transition(session, RuntimeState.Initialized);
        RuntimeStateMachine.Transition(session, RuntimeState.Running);
        RuntimeStateMachine.Transition(session, RuntimeState.Completed);
        RuntimeStateMachine.Transition(session, RuntimeState.Disposed);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            RuntimeStateMachine.Transition(session, RuntimeState.Running));
    }
}
