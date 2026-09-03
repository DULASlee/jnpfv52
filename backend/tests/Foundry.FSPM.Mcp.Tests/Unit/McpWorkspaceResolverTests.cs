// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/McpWorkspaceResolverTests
// =============================================================================
//
//  MCP-06-05: resolver unit tests against self-created temp dirs
//  (deterministic, self-cleaning — no repo paths touched).
// =============================================================================

using Foundry.FSPM.Mcp.Workspace;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public sealed class McpWorkspaceResolverTests : IDisposable
{
    private readonly IMcpWorkspaceResolver _resolver = new McpWorkspaceResolver();
    private readonly string _tempRoot;

    public McpWorkspaceResolverTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "fspm-ws-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(Path.Combine(_tempRoot, "demo.sln"), "sln");
        File.WriteAllText(Path.Combine(_tempRoot, "demo.csproj"), "csproj");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Best-effort temp cleanup; never fail a test on teardown IO.
        }
    }

    [Fact]
    public void Resolve_ExistingRootWithoutHints_Succeeds()
    {
        var outcome = _resolver.Resolve(_tempRoot);

        Assert.True(outcome.IsResolved);
        Assert.NotNull(outcome.Workspace);
        Assert.Equal(_tempRoot, outcome.Workspace!.RootPath);
        Assert.Null(outcome.Workspace.SolutionPath);
        Assert.Null(outcome.Workspace.ProjectPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_MissingRoot_Fails(string? root)
    {
        var outcome = _resolver.Resolve(root);

        Assert.False(outcome.IsResolved);
        Assert.Equal("workspaceRoot", outcome.Field);
    }

    [Fact]
    public void Resolve_NonExistentRoot_Fails()
    {
        var outcome = _resolver.Resolve(Path.Combine(_tempRoot, "no-such-dir"));

        Assert.False(outcome.IsResolved);
        Assert.Equal("workspaceRoot", outcome.Field);
    }

    [Fact]
    public void Resolve_SolutionByBareName_Succeeds()
    {
        var outcome = _resolver.Resolve(_tempRoot, solutionName: "demo");

        Assert.True(outcome.IsResolved);
        Assert.Equal(Path.Combine(_tempRoot, "demo.sln"), outcome.Workspace!.SolutionPath);
    }

    [Fact]
    public void Resolve_ProjectByBareName_Succeeds()
    {
        var outcome = _resolver.Resolve(_tempRoot, projectName: "demo");

        Assert.True(outcome.IsResolved);
        Assert.Equal(Path.Combine(_tempRoot, "demo.csproj"), outcome.Workspace!.ProjectPath);
    }

    [Fact]
    public void Resolve_UnknownSolution_Fails()
    {
        var outcome = _resolver.Resolve(_tempRoot, solutionName: "missing");

        Assert.False(outcome.IsResolved);
        Assert.Equal("solution", outcome.Field);
    }

    [Fact]
    public void Resolve_UnknownProject_Fails()
    {
        var outcome = _resolver.Resolve(_tempRoot, projectName: "missing");

        Assert.False(outcome.IsResolved);
        Assert.Equal("project", outcome.Field);
    }
}
