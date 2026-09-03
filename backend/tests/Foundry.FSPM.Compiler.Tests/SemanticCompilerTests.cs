using Foundry.FSPM.Compiler.Compiler;
using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.WorkspaceNS;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Phase 13 shared loader (compiles the CSharpSemanticCompiler facade so
// P13 tests get a real FspmCompilationSnapshot end-to-end).
[Collection("RoslynWorkspace")]
public sealed class GoldenSemanticCompilation
{
    public static async Task<FspmSemanticCompilationResult> CompileGoldenAsync()
    {
        var loader = new FspmProjectLoader();
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Fixtures", "SemanticGolden", "SemanticGolden.csproj"));
        return await CSharpSemanticCompiler.CompileAsync(loader, path);
    }
}

// G13-01/02/14: real workspace + real compilation + real symbols.
[Collection("RoslynWorkspace")]
public sealed class SemanticCompilerTests
{
    [Fact]
    public async Task Loads_real_semantic_golden_project()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            Assert.NotNull(compiled.Snapshot);
            Assert.NotNull(compiled.Snapshot.Compilation);
            Assert.NotEmpty(compiled.Snapshot.Documents);
            Assert.NotNull(compiled.Index);
            Assert.NotNull(compiled.Resolver);

            var user = compiled.Snapshot.Compilation.GetTypeByMetadataName("SemanticGolden.Domain.User");
            Assert.NotNull(user);
            Assert.IsAssignableFrom<INamedTypeSymbol>(user);
        }
    }

    [Fact]
    public async Task SymbolIndex_ContainsUserPropertyAndMethod()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            Assert.Contains(compiled.Index.OfKind(FspmSymbolKind.Entity),
                r => r.Identity.Value == "SemanticGolden|T:SemanticGolden.Domain.User");
            Assert.Contains(compiled.Index.OfKind(FspmSymbolKind.Property),
                r => r.Identity.Value == "SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber");
            Assert.Contains(compiled.Index.OfKind(FspmSymbolKind.Operation),
                r => r.Identity.Value.EndsWith("User.Create(System.String)~SemanticGolden.Domain.User", StringComparison.Ordinal));
        }
    }
}
