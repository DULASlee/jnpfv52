using JNPF.Runtime.Core;
using Xunit;

namespace JNPF.Tests.Runtime.Core.Execution;

public class ExecutionIdTests
{
    [Fact]
    public void New_ShouldCreateUniqueId()
    {
        var id1 = ExecutionId.New();
        var id2 = ExecutionId.New();

        Assert.NotEqual(id1, id2);
        Assert.NotEqual(Guid.Empty, id1.Value);
    }

    [Fact]
    public void Empty_ShouldBeDefault()
    {
        var empty = ExecutionId.Empty;

        Assert.Equal(Guid.Empty, empty.Value);
        Assert.True(empty.IsEmpty);
    }

    [Fact]
    public void Equality_ShouldWorkCorrectly()
    {
        var guid = Guid.NewGuid();
        var id1 = new ExecutionId(guid);
        var id2 = new ExecutionId(guid);

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
    }

    [Fact]
    public void Inequality_ShouldWorkCorrectly()
    {
        var id1 = new ExecutionId(Guid.NewGuid());
        var id2 = new ExecutionId(Guid.NewGuid());

        Assert.NotEqual(id1, id2);
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var id = new ExecutionId(guid);

        Assert.Equal(guid.ToString(), id.ToString());
    }
}

public class ExecutionStateTests
{
    [Fact]
    public void ExecutionState_ShouldHaveFiveStates()
    {
        var values = Enum.GetValues<ExecutionState>();

        Assert.Contains(ExecutionState.Pending, values);
        Assert.Contains(ExecutionState.Running, values);
        Assert.Contains(ExecutionState.Completed, values);
        Assert.Contains(ExecutionState.Failed, values);
        Assert.Contains(ExecutionState.Cancelled, values);
    }
}

public class ExecutionResultTests
{
    [Fact]
    public void Success_ShouldCreateCompletedResult()
    {
        var id = ExecutionId.New();
        var duration = TimeSpan.FromSeconds(1);

        var result = ExecutionResult.Success(id, duration);

        Assert.Equal(id, result.ExecutionId);
        Assert.Equal(ExecutionState.Completed, result.State);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.False(result.IsCancelled);
        Assert.Null(result.FailureReason);
        Assert.Null(result.Exception);
        Assert.Equal(duration, result.Duration);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var id = ExecutionId.New();
        var duration = TimeSpan.FromSeconds(1);
        var ex = new InvalidOperationException("Test error");

        var result = ExecutionResult.Failure(id, ex.Message, ex, duration);

        Assert.Equal(id, result.ExecutionId);
        Assert.Equal(ExecutionState.Failed, result.State);
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.False(result.IsCancelled);
        Assert.Equal(ex.Message, result.FailureReason);
        Assert.Same(ex, result.Exception);
    }

    [Fact]
    public void Cancelled_ShouldCreateCancelledResult()
    {
        var id = ExecutionId.New();
        var duration = TimeSpan.FromSeconds(1);

        var result = ExecutionResult.Cancelled(id, duration);

        Assert.Equal(id, result.ExecutionId);
        Assert.Equal(ExecutionState.Cancelled, result.State);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.True(result.IsCancelled);
    }
}
