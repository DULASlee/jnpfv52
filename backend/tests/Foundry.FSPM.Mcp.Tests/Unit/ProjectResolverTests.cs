// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/ProjectResolverTests
// =============================================================================
//
//  MCP-07-03: project resolution + TFM tests against self-created temp dirs.
// =============================================================================

using Foundry.FSPM.Mcp.Workspace;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public sealed class ProjectResolverTests : IDisposable
{
    private readonly IProjectResolver _resolver = new ProjectResolver();
    private readonly string _tempRoot;

    public ProjectResolverTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "fspm-proj-" + Guid.NewGuid().ToString("N"));
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

    private static void WriteProject(string dir, string name, string? tfm)
    {
        string tfmXml = tfm is null ? string.Empty : $"<TargetFramework>{tfm}</TargetFramework>";
        File.WriteAllText(
            Path.Combine(dir, name + ".csproj"),
            $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>{tfmXml}</PropertyGroup></Project>");
    }

    [Fact]
    public void Resolve_ByName_ReadsTargetFramework()
    {
        WriteProject(_tempRoot, "demo", "net8.0");

        var outcome = _resolver.Resolve(_tempRoot, "demo");

        Assert.True(outcome.IsResolved);
        Assert.Equal("demo", outcome.Project!.ProjectName);
        Assert.Equal(Path.Combine(_tempRoot, "demo.csproj"), outcome.Project.ProjectPath);
        Assert.Equal("net8.0", outcome.Project.TargetFramework);
    }

    [Fact]
    public void Resolve_MissingTfm_ResolvesWithNullTfm()
    {
        WriteProject(_tempRoot, "demo", null);

        var outcome = _resolver.Resolve(_tempRoot, "demo");

        Assert.True(outcome.IsResolved);
        Assert.Null(outcome.Project!.TargetFramework);
    }

    [Fact]
    public void Resolve_MalformedXml_Fails()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "bad.csproj"), "<Project><oops>");

        var outcome = _resolver.Resolve(_tempRoot, "bad");

        Assert.False(outcome.IsResolved);
        Assert.Equal("project", outcome.Field);
    }

    [Fact]
    public void Resolve_UnknownName_Fails()
    {
        WriteProject(_tempRoot, "demo", "net8.0");

        var outcome = _resolver.Resolve(_tempRoot, "missing");

        Assert.False(outcome.IsResolved);
        Assert.Equal("project", outcome.Field);
    }

    [Fact]
    public void Resolve_NullNameSingleProject_Resolves()
    {
        WriteProject(_tempRoot, "only", "net8.0");

        var outcome = _resolver.Resolve(_tempRoot, null);

        Assert.True(outcome.IsResolved);
        Assert.Equal("only", outcome.Project!.ProjectName);
    }

    [Fact]
    public void Resolve_NullNameMultipleProjects_FailsAmbiguous()
    {
        WriteProject(_tempRoot, "a", "net8.0");
        WriteProject(_tempRoot, "b", "net8.0");

        var outcome = _resolver.Resolve(_tempRoot, null);

        Assert.False(outcome.IsResolved);
        Assert.Equal("project", outcome.Field);
        Assert.Contains("a.csproj", outcome.Message);
    }
}
