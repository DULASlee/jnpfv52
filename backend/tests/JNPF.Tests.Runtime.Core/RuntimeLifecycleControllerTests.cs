using JNPF.Runtime.Core;
using Xunit;

namespace JNPF.Tests.Runtime.Core;

/// <summary>
/// Phase 2-A 验收测试：RuntimeLifecycleController 完整生命周期。
/// </summary>
public sealed class RuntimeLifecycleControllerTests
{
    private readonly RuntimeLifecycleController _controller = new();

    [Fact]
    public async Task InitializeAsync_CreatesSession_InInitializedState()
    {
        // Arrange
        var context = RuntimeContext.Create("t", "p", "pl", "u");

        // Act
        var session = await _controller.InitializeAsync(context);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(RuntimeState.Initialized, session.State);
        Assert.Same(_controller.CurrentSession, session);
    }

    [Fact]
    public async Task InitializeAsync_WhenSessionExists_Throws()
    {
        // Arrange
        var context = RuntimeContext.Create("t", "p", "pl", "u");
        await _controller.InitializeAsync(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.InitializeAsync(RuntimeContext.Create("t2", "p2", "pl2", "u2")));
    }

    [Fact]
    public async Task StartAsync_TransitionsToRunning()
    {
        // Arrange
        var session = await _controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u"));

        // Act
        await _controller.StartAsync(session.SessionId);

        // Assert
        Assert.Equal(RuntimeState.Running, session.State);
    }

    [Fact]
    public async Task PauseAsync_TransitionsToPaused()
    {
        // Arrange
        var session = await _controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u"));
        await _controller.StartAsync(session.SessionId);

        // Act
        await _controller.PauseAsync(session.SessionId);

        // Assert
        Assert.Equal(RuntimeState.Paused, session.State);
    }

    [Fact]
    public async Task ResumeAsync_TransitionsToRunning()
    {
        // Arrange
        var session = await _controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u"));
        await _controller.StartAsync(session.SessionId);
        await _controller.PauseAsync(session.SessionId);

        // Act
        await _controller.ResumeAsync(session.SessionId);

        // Assert
        Assert.Equal(RuntimeState.Running, session.State);
    }

    [Fact]
    public async Task CompleteAsync_TransitionsToCompleted()
    {
        // Arrange
        var session = await _controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u"));
        await _controller.StartAsync(session.SessionId);

        // Act
        await _controller.CompleteAsync(session.SessionId);

        // Assert
        Assert.Equal(RuntimeState.Completed, session.State);
    }

    [Fact]
    public async Task FailAsync_TransitionsToFailed()
    {
        // Arrange
        var session = await _controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u"));
        await _controller.StartAsync(session.SessionId);

        // Act
        await _controller.FailAsync(session.SessionId, "test failure");

        // Assert
        Assert.Equal(RuntimeState.Failed, session.State);
        Assert.Equal("test failure", session.StateReason);
    }

    [Fact]
    public async Task FailAsync_WithEmptyReason_Throws()
    {
        // Arrange
        var session = await _controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u"));
        await _controller.StartAsync(session.SessionId);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _controller.FailAsync(session.SessionId, ""));
    }

    [Fact]
    public async Task DisposeAsync_RemovesSession()
    {
        // Arrange
        var session = await _controller.InitializeAsync(
            RuntimeContext.Create("t", "p", "pl", "u"));
        await _controller.StartAsync(session.SessionId);
        await _controller.CompleteAsync(session.SessionId);

        // Act
        await _controller.DisposeAsync(session.SessionId);

        // Assert
        Assert.Equal(RuntimeState.Disposed, session.State);
        Assert.Null(_controller.CurrentSession);
    }

    [Fact]
    public async Task DisposeAsync_NonExistentSession_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.DisposeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task FullLifecycle_RunsWithoutException()
    {
        // Arrange
        var context = RuntimeContext.Create("tenant-1", "project-1", "pipeline-1", "user-1");

        // Act
        var session = await _controller.InitializeAsync(context);
        await _controller.StartAsync(session.SessionId);
        await _controller.PauseAsync(session.SessionId);
        await _controller.ResumeAsync(session.SessionId);
        await _controller.CompleteAsync(session.SessionId);
        await _controller.DisposeAsync(session.SessionId);

        // Assert
        Assert.Equal(RuntimeState.Disposed, session.State);
        Assert.Null(_controller.CurrentSession);
    }
}
