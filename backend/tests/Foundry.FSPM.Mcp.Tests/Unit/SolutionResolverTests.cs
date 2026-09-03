// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/SolutionResolverTests
// =============================================================================
//
//  MCP-07-02: solution discovery tests against self-created temp dirs.
// =============================================================================

using Foundry.FSPM.Mcp.Workspace;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public sealed class SolutionResolverTests : IDisposable
{
    private readonly ISolutionResolver _resolver = new SolutionResolver();
    private readonly string _tempRoot;

    public SolutionResolverTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "fspm-sln-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
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
    public void Resolve_SingleSolution_Succeeds()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "only.sln"), "sln");

        var outcome = _resolver.Resolve(_tempRoot);

        Assert.True(outcome.IsResolved);
        Assert.Equal(Path.Combine(_tempRoot, "only.sln"), outcome.Solution!.SolutionPath);
    }

    [Fact]
    public void Resolve_NoSolution_Fails()
    {
        var outcome = _resolver.Resolve(_tempRoot);

        Assert.False(outcome.IsResolved);
        Assert.Equal("solution", outcome.Field);
    }

    [Fact]
    public void Resolve_MultipleSolutions_FailsAmbiguous()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "a.sln"), "sln");
        File.WriteAllText(Path.Combine(_tempRoot, "b.sln"), "sln");

        var outcome = _resolver.Resolve(_tempRoot);

        Assert.False(outcome.IsResolved);
        Assert.Equal("solution", outcome.Field);
        Assert.Contains("a.sln", outcome.Message);
        Assert.Contains("b.sln", outcome.Message);
    }

    [Fact]
    public void Resolve_MissingRoot_Fails()
    {
        var outcome = _resolver.Resolve(Path.Combine(_tempRoot, "nope"));

        Assert.False(outcome.IsResolved);
        Assert.Equal("workspaceRoot", outcome.Field);
    }
}
