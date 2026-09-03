using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// G13-DECISION-01 + G13-REASON-01: every semantically-different resolver
// branch has at least one real-compilation test asserting Status +
// IsResolved + non-empty, decision-matching Reason (never Status alone).
//
// Decision matrix (contracts as implemented in CSharpResolver):
// | # | Resolution case              | Cand | Symbol | Owner | Expected  |
// | 1 | 0 candidate                  |    0 | —      | —     | NotFound  |
// | 2 | valid Symbol                 |    1 | Y      | Y     | Resolved  |
// | 3 | member reference (non-value) |    1 | N      | Y     | Resolved* |
// | 4 | owner mismatch               |    1 | N      | N     | Ambiguous |
// | 5 | 2 candidates (method group)  |    2 | N      | —     | Ambiguous |
// | 6 | exact valid member           |    1 | Y      | Y     | Resolved  |
// | 7 | explicit interface impl      |    1 | Y      | exact | distinct  |
// | 8 | deleted/unknown symbol       |    0 | —      | —     | NotFound  |
// * row 3 carries a MemberReference reason (non-value context), never "OK".
// Row 7's symbol-distinctness is proven in H4RelationshipTests and pinned
// at resolver level below; row 8's compilation-level case in H5 (G13_ID_04).
[Collection("RoslynWorkspace")]
public sealed class DecisionMatrixTests
{
    private static void AssertReason(FspmResolutionResult result, string mustContain)
    {
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        Assert.Contains(mustContain, result.Reason);
    }

    [Fact]
    public async Task Row1_ZeroCandidates_ReturnsNotFoundWithReason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression("User.Phone");

            Assert.Equal(FspmResolutionStatus.NotFound, result.Status);
            Assert.False(result.IsResolved);
            Assert.Empty(result.Candidates);
            AssertReason(result, "No symbol bound");
        }
    }

    [Fact]
    public async Task Row2_ValidSymbol_ReturnsResolvedWithReason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression("User.Create(\"x\")");

            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.True(result.IsResolved);
            Assert.NotNull(result.Selected);
            AssertReason(result, "Expression symbol info");
        }
    }

    [Fact]
    public async Task Row3_MemberReference_ReturnsResolvedWithMemberReason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression("User.PhoneNumber");

            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.True(result.IsResolved);
            Assert.Equal("SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber",
                result.Selected!.Identity.Value);
            AssertReason(result, "MemberReference");
        }
    }

    [Fact]
    public async Task Row4_OwnerMismatch_ReturnsAmbiguousWithAuditReason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            // DerivedUser inherits Name from BaseUser: Roslyn reports the
            // single inherited candidate but the receiver (DerivedUser) is
            // not the candidate's containing type (BaseUser) → audit, no promote.
            var result = compiled.Resolver.ResolveExpression("DerivedUser.Name");

            Assert.Equal(FspmResolutionStatus.Ambiguous, result.Status);
            Assert.False(result.IsResolved);
            Assert.Single(result.Candidates);
            AssertReason(result, "failed to bind");
        }
    }

    [Fact]
    public async Task Row5_TwoCandidates_ReturnsAmbiguousWithCountReason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveExpression("User.Create");

            Assert.Equal(FspmResolutionStatus.Ambiguous, result.Status);
            Assert.False(result.IsResolved);
            Assert.Equal(2, result.Candidates.Count);
            AssertReason(result, "2 candidate");
        }
    }

    [Fact]
    public async Task Row6_ExactMember_ReturnsResolvedWithPropertyReason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveProperty("SemanticGolden.Domain.User", "PhoneNumber");

            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.True(result.IsResolved);
            AssertReason(result, "Property single match");
        }
    }

    [Fact]
    public async Task Row7_PlainLookup_SkipsExplicitImpl()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveProperty("SemanticGolden.Relations.AdminUser", "Name");

            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.True(result.IsResolved);
            Assert.Empty(((IPropertySymbol)result.Selected!.Symbol).ExplicitInterfaceImplementations);
            AssertReason(result, "Property single match");
        }
    }

    [Fact]
    public async Task Row8_UnknownMember_ReturnsNotFoundWithReason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveProperty("SemanticGolden.Domain.User", "Phone");

            Assert.Equal(FspmResolutionStatus.NotFound, result.Status);
            Assert.False(result.IsResolved);
            Assert.Empty(result.Candidates);
            AssertReason(result, "has no property");
        }
    }
}
