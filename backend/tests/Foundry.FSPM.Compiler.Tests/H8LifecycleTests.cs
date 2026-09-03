using Foundry.FSPM.Compiler.Semantic;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P13-H8: Lifecycle / Cache / Invalidation / Concurrency.
// Cache key = (SnapshotId, QueryKind, Input). Snapshot change invalidates
// everything (whole-snapshot invalidation, chief D2). V1 facts must never
// satisfy V2 queries — a correctness property, not a performance one.
[Collection("RoslynWorkspace")]
public sealed class H8LifecycleTests
{
    [Fact]
    public async Task SameSnapshot_SameQuery_HitsCache()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            using var context = CompilationSnapshotContext.From(compiled.Snapshot, "snap-v1");

            var first = context.ResolveType("SemanticGolden.Domain.User");
            var second = context.ResolveType("SemanticGolden.Domain.User");

            Assert.Equal(FspmResolutionStatus.Resolved, first.Status);
            Assert.Equal(first.Selected!.Identity, second.Selected!.Identity);
            Assert.Equal(1, context.CacheStatistics.Misses);
            Assert.Equal(1, context.CacheStatistics.Hits);
        }
    }

    [Fact]
    public async Task V1Cache_Never_Satisfies_V2Query()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            using var v1 = CompilationSnapshotContext.From(compiled.Snapshot, "snap-v1");
            var r1 = v1.ResolveType("SemanticGolden.Domain.User");
            Assert.Equal(FspmResolutionStatus.Resolved, r1.Status);

            // New snapshot id over the same compilation = new cache generation.
            using var v2 = CompilationSnapshotContext.From(compiled.Snapshot, "snap-v2");
            Assert.Equal(0, v2.CacheStatistics.Hits);
            Assert.Equal(0, v2.CacheStatistics.Misses);

            var r2 = v2.ResolveType("SemanticGolden.Domain.User");
            Assert.Equal(FspmResolutionStatus.Resolved, r2.Status);
            Assert.Equal(0, v2.CacheStatistics.Hits);
            Assert.Equal(1, v2.CacheStatistics.Misses);
            Assert.Equal(r1.Selected!.Identity, r2.Selected!.Identity);
        }
    }

    [Fact]
    public async Task Hundred_Concurrent_Resolves_Agree()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            using var context = CompilationSnapshotContext.From(compiled.Snapshot, "snap-v1");

            var results = new FspmResolutionResult[100];
            Parallel.For(0, 100, i =>
            {
                results[i] = context.ResolveType("SemanticGolden.Domain.User");
            });

            var winner = results[0].Selected!.Identity;
            Assert.All(results, r =>
            {
                Assert.Equal(FspmResolutionStatus.Resolved, r.Status);
                Assert.Equal(winner, r.Selected!.Identity);
            });
        }
    }

    [Fact]
    public async Task DisposedContext_Rejects_Resolution_AsInfrastructureFailure()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var context = CompilationSnapshotContext.From(compiled.Snapshot, "snap-v1");
            context.Dispose();

            var result = context.ResolveType("SemanticGolden.Domain.User");
            Assert.Equal(FspmResolutionStatus.InfrastructureFailure, result.Status);
            Assert.Equal(ResolutionOutcomeClass.Execution, result.Status.Classify());
        }
    }

    [Fact]
    public async Task CancelledToken_Yields_CancelledResult()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            using var context = CompilationSnapshotContext.From(compiled.Snapshot, "snap-v1");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = context.ResolveType("SemanticGolden.Domain.User", cts.Token);
            Assert.Equal(FspmResolutionStatus.Cancelled, result.Status);
            Assert.Equal(ResolutionOutcomeClass.Execution, result.Status.Classify());
        }
    }
}
