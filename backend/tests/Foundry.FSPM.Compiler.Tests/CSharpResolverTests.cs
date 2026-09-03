using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Phase 13 resolver tests: all four states (Resolved/NotFound/Ambiguous/Invalid)
// + source location + identity stability + cross-project scope.
[Collection("RoslynWorkspace")]
public sealed class CSharpResolverTests
{
    [Fact]
    public async Task ResolveType_User_ReturnsResolved()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveType("SemanticGolden.Domain.User");
            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.Equal("SemanticGolden|T:SemanticGolden.Domain.User", result.Selected!.Identity.Value);
            Assert.Equal(FspmSymbolKind.Entity, result.Selected!.Kind);
        }
    }

    [Fact]
    public async Task ResolveType_Unknown_ReturnsNotFound()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveType("SemanticGolden.Domain.NonExistent");
            Assert.Equal(FspmResolutionStatus.NotFound, result.Status);
            Assert.Empty(result.Candidates);
        }
    }

    [Fact]
    public async Task ResolveProperty_PhoneNumber_ReturnsResolved()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveProperty("SemanticGolden.Domain.User", "PhoneNumber");
            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.IsAssignableFrom<IPropertySymbol>(result.Selected!.Symbol);
            Assert.Equal(SpecialType.System_String,
                ((IPropertySymbol)result.Selected!.Symbol).Type.SpecialType);
        }
    }

    [Fact]
    public async Task ResolveProperty_UnknownMember_ReturnsNotFound_NotGuessedToPhoneNumber()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveProperty("SemanticGolden.Domain.User", "Phone");
            Assert.Equal(FspmResolutionStatus.NotFound, result.Status);
            Assert.Empty(result.Candidates);
        }
    }

    [Fact]
    public async Task ResolveMethod_ByNameOnly_Overloaded_ReturnsAmbiguous()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveMethod("SemanticGolden.Domain.User", "Create");
            Assert.Equal(FspmResolutionStatus.Ambiguous, result.Status);
            Assert.True(result.Candidates.Count >= 2);
        }
    }

    [Fact]
    public async Task ResolveMethod_BySignature_String_ReturnsResolved()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveMethodBySignature(
                "SemanticGolden.Domain.User", "Create", new[] { "string" });
            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.Equal("string",
                ((IMethodSymbol)result.Selected!.Symbol).Parameters[0].Type.ToDisplayString());
        }
    }

    [Fact]
    public async Task ResolveMethod_BySignature_Int_ReturnsResolvedDistinctFromString()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveMethodBySignature(
                "SemanticGolden.Domain.User", "Create", new[] { "int" });
            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.Equal("int",
                ((IMethodSymbol)result.Selected!.Symbol).Parameters[0].Type.ToDisplayString());
        }
    }

    [Fact]
    public async Task ResolveMethod_BySignature_NoMatch_ReturnsNotFound()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveMethodBySignature(
                "SemanticGolden.Domain.User", "Create", new[] { "double" });
            Assert.Equal(FspmResolutionStatus.NotFound, result.Status);
        }
    }

    [Theory]
    [InlineData("User.UserName")]
    [InlineData("User.PhoneNumber")]
    [InlineData("User.TenantId")]
    public async Task ResolveExpression_KnownMember_ReturnsResolved(string expression)
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression(expression);
            Assert.True(result.Status == FspmResolutionStatus.Resolved,
                $"Expected Resolved, got {result.Status}: {result.Reason}");
            Assert.NotNull(result.Selected);
        }
    }

    [Fact]
    public async Task ResolveExpression_MethodGroup_WithOverloads_ReturnsAmbiguous()
    {
        // `User.Create` without arguments is a method group over 2
        // overloads: Symbol stays null AND 2 candidates exist → honest
        // Ambiguous (never First-picked).
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression("User.Create");
            Assert.Equal(FspmResolutionStatus.Ambiguous, result.Status);
            Assert.Equal(2, result.Candidates.Count);
        }
    }

    [Fact]
    public async Task ResolveExpression_MemberReference_Records_NonValue_Reason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            // Direct Roslyn proof that the verdict rests on binding
            // failure + verified member reference (not silent promotion).
            var result = compiled.Resolver.ResolveExpression("User.PhoneNumber");
            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.Contains("MemberReference", result.Reason);
        }
    }

    [Fact]
    public async Task ResolveExpression_UnknownMember_ReturnsNotFound_NotGuessed()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression("User.Phone");
            Assert.Equal(FspmResolutionStatus.NotFound, result.Status);
        }
    }

    [Fact]
    public async Task ResolveExpression_InvalidSyntax_ReturnsInvalid()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression("User.[PhoneNumber");
            Assert.Equal(FspmResolutionStatus.Invalid, result.Status);
        }
    }

    [Fact]
    public async Task ResolveExpression_Indexer_PassesThrough_NotInterpreted()
    {
        // P12 produced the verbatim text; P13 forwards to Roslyn verbatim.
        // Roslyn reports the indexer symbol (or CandidateSymbols when the
        // receiver is unknown) — what matters here is that P13 never
        // parsed '[0]' itself.
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression("obj[0].Property");
            Assert.True(result.Status is FspmResolutionStatus.NotFound or FspmResolutionStatus.Resolved or FspmResolutionStatus.Ambiguous);
            // The Resolver never returns Invalid for a syntactically-valid
            // expression it does not recognise — the architecture rule is
            // that P13 only routes to Roslyn, never re-parses.
        }
    }

    [Fact]
    public async Task ResolveProperty_PhoneNumber_SourceLocationPointsToRealFile()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveProperty("SemanticGolden.Domain.User", "PhoneNumber");
            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.EndsWith("User.cs", result.Selected!.Location.DocumentPath);
            Assert.True(result.Selected!.Location.StartLine >= 1);
            Assert.True(result.Selected!.Location.StartColumn >= 1);
        }
    }
}

[Collection("RoslynWorkspace")]
public sealed class ResolverDeterminismTests
{
    [Fact]
    public async Task Ten_resolves_of_same_expression_yield_identical_identity()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var first = compiled.Resolver.ResolveExpression("User.PhoneNumber").Selected!.Identity;
            for (var i = 0; i < 9; i++)
            {
                Assert.Equal(first,
                    compiled.Resolver.ResolveExpression("User.PhoneNumber").Selected!.Identity);
            }
        }
    }
}
