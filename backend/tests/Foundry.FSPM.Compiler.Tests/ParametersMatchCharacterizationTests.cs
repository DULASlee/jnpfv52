using Foundry.FSPM.Compiler.Semantic;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// FIX-05 characterization (NO implementation change per package ruling):
// ParametersMatch compares Roslyn ToDisplayString() ordinally, so the
// CALLER's spelling must match Roslyn's rendering. This test pins the
// current behavior as the P14-SEM-01 before/after baseline.
[Collection("RoslynWorkspace")]
public sealed class ParametersMatchCharacterizationTests
{
    [Fact]
    public async Task KeywordSpelling_Matches()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveMethodBySignature(
                "SemanticGolden.Domain.User", "Create", new[] { "string" });

            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
        }
    }

    [Fact]
    public async Task FullyQualifiedSpelling_DoesNotMatch_CurrentLimitation()
    {
        // P14-SEM-01: today "System.String" misses while "string" hits,
        // although both denote the same type. Recorded, not fixed.
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var result = compiled.Resolver.ResolveMethodBySignature(
                "SemanticGolden.Domain.User", "Create", new[] { "System.String" });

            Assert.Equal(FspmResolutionStatus.NotFound, result.Status);
        }
    }
}
