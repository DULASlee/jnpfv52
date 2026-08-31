using System.Text.Json;
using Xunit;

namespace JNPF.Tests.Agent;

public sealed class GateATests
{
    private const string BaselineJsonPath = @"D:\JNPF-v52\backend\tests\JNPF.Tests.Runtime.Expert\build-baseline.json";
    private const string FlowCommentProjectPath = @"D:\JNPF-v52\backend\modularity\workflow\JNPF.WorkFlow\JNPF.WorkFlow.csproj";

    [Fact]
    public void GateA_BaselineJson_ContainsPreRefactorCommitAndBlobSha()
    {
        Assert.True(File.Exists(BaselineJsonPath), $"baseline.json missing at {BaselineJsonPath}");
        var doc = JsonDocument.Parse(File.ReadAllText(BaselineJsonPath));
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("preRefactorCommit", out var commitEl));
        Assert.False(string.IsNullOrWhiteSpace(commitEl.GetString()));

        // [B1] v5: BLOB SHA (NOT a file hash) — 40-char hex
        Assert.True(root.TryGetProperty("referenceBlobSha", out var blobEl));
        var blob = blobEl.GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(blob));
        Assert.Matches("^[0-9a-f]{40}$", blob);
    }

    [Fact]
    public void GateA_CanonicalCommand_MatchesWhatBaselineUsed()
    {
        // [B4] baseline.command MUST equal CanonicalBuildRunner.ComposeCommandLine.
        // This proves the same command string was used for baseline and (after this test)
        // will be used for after-build verification.
        var baseline = JsonDocument.Parse(File.ReadAllText(BaselineJsonPath)).RootElement;
        var baselineCommand = baseline.GetProperty("command").GetString()!;
        var expected = CanonicalBuildRunner.ComposeCommandLine(FlowCommentProjectPath);
        Assert.Equal(expected, baselineCommand);
    }

    [Fact(Timeout = 600000)]
    public void GateA_AfterRefactor_BuildSucceeds_ViaCanonicalRunner()
    {
        var result = CanonicalBuildRunner.Run(FlowCommentProjectPath, TimeSpan.FromMinutes(10));
        Assert.True(result.Success,
            $"Build failed (exit={result.ExitCode}, errors={result.ErrorCount}).\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact(Timeout = 600000)]
    public void GateA_WarningsDoNotIncreaseFromPreRefactorBaseline()
    {
        var baseline = JsonDocument.Parse(File.ReadAllText(BaselineJsonPath)).RootElement;
        var baselineWarnings = baseline.GetProperty("warningCount").GetInt32();

        var result = CanonicalBuildRunner.Run(FlowCommentProjectPath, TimeSpan.FromMinutes(10));
        Assert.True(result.Success, "Build must succeed for warning comparison");
        Assert.True(result.WarningCount <= baselineWarnings,
            $"Warnings_after ({result.WarningCount}) > Warnings_baseline ({baselineWarnings})");
    }

    [Fact(Timeout = 600000)]
    public void GateA_CanonicalRunnerExitCode_IsZero()
    {
        // Build artefact MUST be produced; canonical runner guarantees the command
        // string is the same as baseline (proves identical command).
        var result = CanonicalBuildRunner.Run(FlowCommentProjectPath, TimeSpan.FromMinutes(10));
        Assert.Equal(0, result.ExitCode);
    }
}