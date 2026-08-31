using JNPF.Runtime.Core;
using JNPF.Runtime.Core.Observation;
using JNPF.Runtime.Core.SelfRepair;
using Xunit;

namespace JNPF.Tests.Runtime.Core;

public sealed class NewComponentTests
{
    #region ExecutionRecord Tests

    [Fact]
    public void ExecutionRecord_Started_ShouldCreatePendingRecord()
    {
        var executionId = ExecutionId.New();
        var sessionId = Guid.NewGuid();

        var record = ExecutionRecord.Started(executionId, sessionId, "Test Action", null, null, "input");

        Assert.Equal(executionId, record.ExecutionId);
        Assert.Equal(sessionId, record.SessionId);
        Assert.Equal("Test Action", record.Action);
        Assert.Equal("input", record.Input);
        Assert.Null(record.CompletedAtUtc);
        Assert.False(record.IsSuccess);
        Assert.False(record.IsFailure);
    }

    [Fact]
    public void ExecutionRecord_Completed_ShouldCreateSuccessRecord()
    {
        var executionId = ExecutionId.New();
        var sessionId = Guid.NewGuid();
        var started = ExecutionRecord.Started(executionId, sessionId, "Test Action");

        var completed = started.Completed("output", ValidationResult.Pass("OK"));

        Assert.True(completed.IsSuccess);
        Assert.False(completed.IsFailure);
        Assert.Equal("output", completed.Output);
        Assert.NotNull(completed.CompletedAtUtc);
        Assert.NotNull(completed.ValidationResult);
        Assert.True(completed.ValidationResult.IsPassed);
    }

    [Fact]
    public void ExecutionRecord_Failed_ShouldCreateFailureRecord()
    {
        var executionId = ExecutionId.New();
        var sessionId = Guid.NewGuid();
        var started = ExecutionRecord.Started(executionId, sessionId, "Test Action");
        var ex = new InvalidOperationException("Test error");

        var failed = started.Failed("Test failure", ex);

        Assert.False(failed.IsSuccess);
        Assert.True(failed.IsFailure);
        Assert.Equal("Test failure", failed.FailureReason);
        Assert.Equal("System.InvalidOperationException", failed.ExceptionType);
        Assert.NotNull(failed.CompletedAtUtc);
    }

    [Fact]
    public void ExecutionRecord_Duration_ShouldCalculateCorrectly()
    {
        var executionId = ExecutionId.New();
        var record = ExecutionRecord.Started(executionId, Guid.NewGuid(), "Test");
        Thread.Sleep(10);
        var completed = record.Completed();

        Assert.True(completed.Duration.TotalMilliseconds >= 10);
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_Pass_ShouldCreatePassingResult()
    {
        var result = ValidationResult.Pass("Test passed", "detail1", "detail2");

        Assert.True(result.IsPassed);
        Assert.Equal("Test passed", result.Message);
        Assert.Equal(2, result.Details.Count);
    }

    [Fact]
    public void ValidationResult_Fail_ShouldCreateFailingResult()
    {
        var result = ValidationResult.Fail("Test failed", "issue1");

        Assert.False(result.IsPassed);
        Assert.Equal("Test failed", result.Message);
    }

    #endregion

    #region InMemoryExecutionObserver Tests

    [Fact]
    public void InMemoryExecutionObserver_RecordStarted_ShouldStoreRecord()
    {
        var observer = new InMemoryExecutionObserver();
        var executionId = ExecutionId.New();
        var sessionId = Guid.NewGuid();
        var record = ExecutionRecord.Started(executionId, sessionId, "Test");

        observer.RecordStarted(record);

        var records = observer.GetRecordsForSession(sessionId);
        Assert.Single(records);
        Assert.Equal(executionId, records[0].ExecutionId);
    }

    [Fact]
    public void InMemoryExecutionObserver_RecordCompleted_ShouldUpdateRecord()
    {
        var observer = new InMemoryExecutionObserver();
        var executionId = ExecutionId.New();
        var sessionId = Guid.NewGuid();
        var started = ExecutionRecord.Started(executionId, sessionId, "Test");

        observer.RecordStarted(started);
        var completed = started.Completed();
        observer.RecordCompleted(completed);

        var records = observer.GetRecordsForExecution(executionId);
        Assert.Single(records);
        Assert.True(records[0].IsSuccess);
    }

    #endregion

    #region SelfEvaluationResult Tests

    [Fact]
    public void SelfEvaluationResult_Pass_ShouldBePassed()
    {
        var result = SelfEvaluationResult.Pass("All checks passed");

        Assert.True(result.IsPassed);
        Assert.True(result.TaskSatisfied);
        Assert.True(result.ContractsPreserved);
        Assert.True(result.BehaviorPreserved);
        Assert.True(result.FunctionalityIntact);
        Assert.True(result.ArchitectureCompliant);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void SelfEvaluationResult_Evaluate_WithFailure_ShouldReportIssues()
    {
        var result = SelfEvaluationResult.Evaluate(
            taskSatisfied: true,
            contractsPreserved: false,
            behaviorPreserved: true,
            functionalityIntact: true,
            architectureCompliant: true);

        Assert.False(result.IsPassed);
        Assert.Single(result.Issues);
        Assert.Contains("Contracts", result.Issues[0]);
    }

    [Fact]
    public void SelfEvaluationResult_Fail_ShouldBeFailed()
    {
        var result = SelfEvaluationResult.Fail("Multiple issues", "Issue 2");

        Assert.False(result.IsPassed);
        Assert.Equal(2, result.Issues.Count);
    }

    #endregion

    #region SelfTestResult Tests

    [Fact]
    public void SelfTestResult_Create_AllPassing_ShouldBePassed()
    {
        var result = SelfTestResult.Create(
            buildPassed: true,
            unitTestsPassed: true,
            integrationTestsPassed: true,
            regressionTestsPassed: true,
            totalTests: 100,
            passedTests: 100,
            failedTests: 0);

        Assert.True(result.IsPassed);
        Assert.Equal(100, result.TotalTests);
        Assert.Equal(100, result.PassedTests);
    }

    [Fact]
    public void SelfTestResult_BuildFailed_ShouldBeFailed()
    {
        var result = SelfTestResult.BuildFailed(new[] { "Error 1", "Error 2" });

        Assert.False(result.IsPassed);
        Assert.False(result.BuildPassed);
        Assert.Equal(2, result.FailedTests);
    }

    #endregion

    #region RepairResult Tests

    [Fact]
    public void RepairResult_Succeeded_ShouldBeSuccess()
    {
        var result = RepairResult.Succeeded("Fixed the issue", "file1.cs", "file2.cs");

        Assert.True(result.Success);
        Assert.Equal("Fixed the issue", result.Description);
        Assert.Equal(2, result.ModifiedFiles.Count);
        Assert.Null(result.VerificationResult);
    }

    [Fact]
    public void RepairResult_Failed_ShouldBeFailure()
    {
        var result = RepairResult.Failed("Cannot fix");

        Assert.False(result.Success);
        Assert.Equal("Cannot fix", result.Description);
    }

    #endregion
}
