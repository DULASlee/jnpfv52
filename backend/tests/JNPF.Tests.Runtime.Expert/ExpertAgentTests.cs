using JNPF.Runtime.Core;
using JNPF.Runtime.Core.SelfRepair;
using JNPF.Runtime.Expert;
using JNPF.Runtime.Expert.Tools;
using RuntimeExecCtx = JNPF.Runtime.Core.ExecutionContext;
using Xunit;

namespace JNPF.Tests.Agent;

public sealed class ExpertAgentTests
{
    #region Expert Identity Tests

    [Fact]
    public void Expert_CreateClassRefactorExpert_ShouldHaveCorrectType()
    {
        var expert = Expert.CreateClassRefactorExpert();

        Assert.Equal(ExpertType.ClassRefactor, expert.Type);
        Assert.Equal("Class Refactoring Expert", expert.Name);
        Assert.Contains("ClassDiscovery", expert.SupportedSkills);
        Assert.Contains("ContractExtraction", expert.SupportedSkills);
        Assert.NotEqual(Guid.Empty, expert.Id);
    }

    [Fact]
    public void Expert_Create_ShouldCreateCustomExpert()
    {
        var expert = Expert.Create(
            ExpertType.ApiDesign,
            "API Design Expert",
            "v1.0",
            "Expert in API design",
            "Skill1", "Skill2");

        Assert.Equal(ExpertType.ApiDesign, expert.Type);
        Assert.Equal("API Design Expert", expert.Name);
        Assert.Equal("v1.0", expert.Version);
        Assert.Equal(2, expert.SupportedSkills.Count);
    }

    #endregion

    #region ClassRefactorTask Tests

    [Fact]
    public void ClassRefactorTask_Create_ShouldCreateValidTask()
    {
        var sessionId = Guid.NewGuid();
        var task = ClassRefactorTask.Create(
            sessionId,
            @"d:\project\src\MyClass.cs",
            @"d:\project\src\MyProject.csproj",
            @"d:\project",
            "Improve code documentation");

        Assert.NotEqual(Guid.Empty, task.TaskId);
        Assert.Equal(sessionId, task.SessionId);
        Assert.Equal("MyClass", task.TargetClassName);
        Assert.Equal("Improve code documentation", task.RefactorObjective);
        Assert.True(task.Preservation.PreservePublicApi);
        Assert.True(task.Validation.RequireBuildPass);
    }

    [Fact]
    public void PreservationContract_Default_ShouldPreserveAll()
    {
        var preservation = PreservationContract.Default;

        Assert.True(preservation.PreservePublicApi);
        Assert.True(preservation.PreserveBehavior);
        Assert.True(preservation.PreserveAuthorization);
        Assert.True(preservation.PreserveTransaction);
        Assert.True(preservation.PreserveTenantSemantics);
        Assert.True(preservation.PreserveDataAccess);
    }

    #endregion

    #region ExpertExecutionContext Tests

    [Fact]
    public void ExpertExecutionContext_Create_ShouldInitializeCorrectly()
    {
        var expert = Expert.CreateClassRefactorExpert();
        var task = ClassRefactorTask.Create(Guid.NewGuid(), "test.cs", "test.csproj", ".", "Test");
        var runtimeContext = RuntimeExecCtx.Create(Guid.NewGuid());
        var expertContext = ExpertExecutionContext.Create(expert, task, runtimeContext);

        Assert.Equal(ExpertPhase.Created, expertContext.Phase);
        Assert.Equal(ExpertTaskStatus.Pending, expertContext.Status);
        Assert.True(expertContext.CanContinue);
    }

    [Fact]
    public void ExpertExecutionContext_TransitionTo_ShouldUpdateState()
    {
        var expert = Expert.CreateClassRefactorExpert();
        var task = ClassRefactorTask.Create(Guid.NewGuid(), "test.cs", "test.csproj", ".", "Test");
        var runtimeContext = RuntimeExecCtx.Create(Guid.NewGuid());
        var expertContext = ExpertExecutionContext.Create(expert, task, runtimeContext);

        expertContext.TransitionTo(ExpertPhase.Analyzing, "Analyzing...");

        Assert.Equal(ExpertPhase.Analyzing, expertContext.Phase);
        Assert.Equal(ExpertTaskStatus.Running, expertContext.Status);
        Assert.Equal("Analyzing...", expertContext.CurrentMessage);
    }

    [Fact]
    public void ExpertExecutionContext_TransitionTo_Completed_ShouldSetSucceeded()
    {
        var expert = Expert.CreateClassRefactorExpert();
        var task = ClassRefactorTask.Create(Guid.NewGuid(), "test.cs", "test.csproj", ".", "Test");
        var runtimeContext = RuntimeExecCtx.Create(Guid.NewGuid());
        var expertContext = ExpertExecutionContext.Create(expert, task, runtimeContext);

        expertContext.TransitionTo(ExpertPhase.Completed);

        Assert.Equal(ExpertPhase.Completed, expertContext.Phase);
        Assert.Equal(ExpertTaskStatus.Succeeded, expertContext.Status);
        Assert.False(expertContext.CanContinue);
    }

    [Fact]
    public void ExpertExecutionContext_IncrementRetry_ShouldRespectMaxRetries()
    {
        var expert = Expert.CreateClassRefactorExpert();
        var task = ClassRefactorTask.Create(Guid.NewGuid(), "test.cs", "test.csproj", ".", "Test");
        var runtimeContext = RuntimeExecCtx.Create(Guid.NewGuid());
        var expertContext = ExpertExecutionContext.Create(expert, task, runtimeContext);

        // Max retries is 3 by default
        Assert.True(expertContext.IncrementRetry()); // 1
        Assert.True(expertContext.IncrementRetry()); // 2
        Assert.True(expertContext.IncrementRetry()); // 3
        Assert.False(expertContext.IncrementRetry()); // 4 > 3
    }

    [Fact]
    public void ExpertExecutionContext_AddArtifact_ShouldStoreArtifact()
    {
        var expert = Expert.CreateClassRefactorExpert();
        var task = ClassRefactorTask.Create(Guid.NewGuid(), "test.cs", "test.csproj", ".", "Test");
        var runtimeContext = RuntimeExecCtx.Create(Guid.NewGuid());
        var expertContext = ExpertExecutionContext.Create(expert, task, runtimeContext);

        var artifact = new ExpertArtifact(
            ExpertArtifactType.DiscoveryReport,
            "Test Report",
            "A test report",
            new[] { "test.cs" },
            "{}");

        expertContext.AddArtifact(artifact);

        Assert.Single(expertContext.Artifacts);
        Assert.Equal("Test Report", expertContext.Artifacts[0].Name);
    }

    #endregion

    #region ExpertPhase Tests

    [Theory]
    [InlineData(ExpertPhase.Created, 0)]
    [InlineData(ExpertPhase.Analyzing, 1)]
    [InlineData(ExpertPhase.Planning, 2)]
    [InlineData(ExpertPhase.Executing, 3)]
    [InlineData(ExpertPhase.Validating, 4)]
    [InlineData(ExpertPhase.Completed, 7)]
    [InlineData(ExpertPhase.Failed, 8)]
    public void ExpertPhase_ShouldHaveCorrectValue(ExpertPhase phase, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)phase);
    }

    #endregion

    #region ExpertArtifact Tests

    [Fact]
    public void ExpertArtifact_ShouldCreateWithCorrectType()
    {
        var artifact = new ExpertArtifact(
            ExpertArtifactType.CodeDiff,
            "Diff Report",
            "Code changes",
            new[] { "file1.cs", "file2.cs" },
            "diff content");

        Assert.Equal(ExpertArtifactType.CodeDiff, artifact.Type);
        Assert.Equal("Diff Report", artifact.Name);
        Assert.Equal(2, artifact.FilePaths.Count);
        Assert.NotNull(artifact.Content);
    }

    #endregion

    #region ExpertResult Tests

    [Fact]
    public void ExpertResult_Succeeded_ShouldHaveCorrectProperties()
    {
        var result = ExpertResult.Succeeded(
            "Task completed",
            ExpertPhase.Completed,
            SelfEvaluationResult.Pass("All checks passed"),
            SelfTestResult.Create(true, true, true, true, 10, 10, 0),
            Array.Empty<ExpertArtifact>(),
            TimeSpan.FromMinutes(5));

        Assert.True(result.IsSuccess);
        Assert.Equal(ExpertTaskStatus.Succeeded, result.Status);
        Assert.Equal(ExpertPhase.Completed, result.FinalPhase);
        Assert.NotNull(result.SelfEvaluation);
        Assert.NotNull(result.TestResult);
    }

    [Fact]
    public void ExpertResult_Failed_ShouldHaveCorrectProperties()
    {
        var result = ExpertResult.Failed(
            "Task failed",
            ExpertPhase.Failed,
            Array.Empty<ExpertArtifact>(),
            TimeSpan.FromMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ExpertTaskStatus.Failed, result.Status);
        Assert.Null(result.SelfEvaluation);
    }

    #endregion

    #region IExpertToolSet Tests

    [Fact]
    public async Task FileSystemExpertToolSet_ReadFileAsync_NonExistent_ShouldThrow()
    {
        var tools = new FileSystemExpertToolSet(@"d:\nonexistent");

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            tools.ReadFileAsync("nonexistent.cs"));
    }

    [Fact]
    public void FileSystemExpertToolSet_Constructor_ShouldStoreRoot()
    {
        var tools = new FileSystemExpertToolSet(@"d:\myrepo");

        Assert.NotNull(tools);
    }

    #endregion
}
