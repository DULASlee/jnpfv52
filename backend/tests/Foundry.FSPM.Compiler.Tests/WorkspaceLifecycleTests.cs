using Foundry.FSPM.Compiler.WorkspaceNS;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// G13-LIFECYCLE-01: workspace ownership verification only (no new
// abstraction). success / open failure / build failure / cancellation:
// ownership clear, dispose path exists, exceptions preserved (Dispose
// never swallows).
[Collection("RoslynWorkspace")]
public sealed class WorkspaceLifecycleTests
{
    private static string GoldenCsproj => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "Fixtures", "SemanticGolden", "SemanticGolden.csproj"));

    [Fact]
    public async Task Load_Success_Owns_Workspace_And_Compiles()
    {
        var loader = new FspmProjectLoader();
        using var workspace = await loader.LoadAsync(GoldenCsproj);

        Assert.NotEmpty(workspace.Projects);
        var compilation = await FspmProjectLoader.GetCompilationAsync(workspace);
        Assert.NotNull(compilation);
    }

    [Fact]
    public async Task Load_MissingFile_Throws_FileNotFound_Preserving_Path()
    {
        var loader = new FspmProjectLoader();
        var missing = Path.Combine(Path.GetTempPath(), "fspm-no-such-project.csproj");

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(
            () => loader.LoadAsync(missing));
        Assert.Contains("fspm-no-such-project.csproj", ex.Message);
    }

    [Fact]
    public async Task GetCompilation_EmptyWorkspace_Throws_InvalidOperation()
    {
        using var adhoc = new AdhocWorkspace();
        var empty = new FspmWorkspace
        {
            RootPath = Path.GetTempPath(),
            MSBuildWorkspace = adhoc,
            Projects = System.Array.Empty<Project>(),
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => FspmProjectLoader.GetCompilationAsync(empty));
        Assert.Contains("no projects", ex.Message);
    }

    [Fact]
    public async Task Load_CancelledToken_Throws_OperationCanceled_NotSwallowed()
    {
        var loader = new FspmProjectLoader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => loader.LoadAsync(GoldenCsproj, cts.Token));
    }
}
