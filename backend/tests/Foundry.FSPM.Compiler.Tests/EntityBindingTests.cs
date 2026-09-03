using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// G08-1 Entity binding (directive 8.1): exact / unknown / ambiguous /
/// cross-project / namespace-collision. All against a REAL compilation.
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class EntityBindingTests
{
    private static FspmEntityDeclarationSyntax Decl(string name) =>
        new FspmEntityDeclarationSyntax(name, 0, name.Length, 1, 1);

    [Fact]
    public async Task ExactEntity_OtherUser_BindsToSingleRealType()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = EntityBinder.Bind(Decl("OtherUser"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Diagnostics);
            var symbol = Assert.IsAssignableFrom<INamedTypeSymbol>(result.Symbol);
            Assert.Equal("SemanticGolden.Domain.OtherUser", symbol.ToDisplayString());
            Assert.NotNull(result.SymbolId);
            Assert.Equal(
                FspmSymbolIdentity.Create(symbol),
                result.SymbolId!.Value);
        }
    }

    [Fact]
    public async Task UnknownEntity_NoSuchType_YieldsFSPM101()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = EntityBinder.Bind(Decl("NoSuchType"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Unknown, result.Status);
            Assert.False(result.IsSuccess);
            Assert.Null(result.Symbol);
            Assert.Null(result.SymbolId);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.EntityNotFound, diagnostic.Code);
            Assert.Equal(FspmDiagnosticSeverity.Error, diagnostic.Severity);
        }
    }

    [Fact]
    public async Task AmbiguousEntity_BareUser_ListsAllCandidates_NeverGuesses()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = EntityBinder.Bind(Decl("User"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Ambiguous, result.Status);
            Assert.Null(result.Symbol);
            Assert.Null(result.SymbolId);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousEntity, diagnostic.Code);
            Assert.Contains("SemanticGolden.Domain.User", diagnostic.Message);
            Assert.Contains("SemanticGolden.NamespaceA.User", diagnostic.Message);
            Assert.Contains("SemanticGolden.NamespaceB.User", diagnostic.Message);
            Assert.Contains("SemanticGolden.Contracts.User", diagnostic.Message);
        }
    }

    [Fact]
    public async Task CrossProjectEntity_FqnContractsUser_BindsToContractsAssembly()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = EntityBinder.Bind(Decl("SemanticGolden.Contracts.User"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            var symbol = Assert.IsAssignableFrom<INamedTypeSymbol>(result.Symbol);
            Assert.Equal("SemanticGolden.Contracts", symbol.ContainingAssembly.Name);
            Assert.Equal(
                "SemanticGolden.Contracts|T:SemanticGolden.Contracts.User",
                result.SymbolId!.Value.ToString());
        }
    }

    [Fact]
    public async Task NamespaceCollision_FqnDisambiguates_NamespaceAUser()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = EntityBinder.Bind(Decl("SemanticGolden.NamespaceA.User"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            var symbol = Assert.IsAssignableFrom<INamedTypeSymbol>(result.Symbol);
            Assert.Equal("SemanticGolden.NamespaceA", symbol.ContainingNamespace.ToDisplayString());
        }
    }

    [Fact]
    public async Task UnknownFqn_DottedButMissing_YieldsFSPM101()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = EntityBinder.Bind(Decl("SemanticGolden.Domain.Nope"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Unknown, result.Status);
            Assert.Equal(FspmDiagnosticCodes.EntityNotFound, Assert.Single(result.Diagnostics).Code);
        }
    }
}
