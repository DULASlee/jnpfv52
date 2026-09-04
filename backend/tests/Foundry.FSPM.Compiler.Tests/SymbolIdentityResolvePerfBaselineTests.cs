using Foundry.FSPM.Compiler.Compiler;
using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Foundry.FSPM.Compiler.Tests;

// G14-01-POST-PERF-01 — FspmSymbolIdentity.Resolve performance baseline.
//
// Architect directive: characterize the cost of
//   DocumentationCommentId.GetSymbolsForDeclarationId(id, compilation)
//   before committing to a Compilation-scoped cache (P14-PERF-01).
// This file is MEASUREMENT ONLY. It does not modify the production
// code, does not change caching policy, and does not commit to a
// performance fix. The numbers land in .fspm/evidence/.../perf.txt
// and feed the P14-PERF-01 backlog decision (A = real bottleneck →
// implement; B = acceptable → record baseline only).
//
// Workload:
//   - Same id repeatedly: 1 / 10 / 100 / 1000 resolves of one FspmSymbolId
//   - Different ids:       1 / 10 / 100 / 1000 distinct FspmSymbolIds
// For each: elapsed (ms), allocation (bytes), Gen0/1/2 GC counts.
// Compilation size is reported once at the top.
//
// The test does not assert any number; it records them and PASSes
// unless the test infrastructure itself fails. The baseline values
// are persisted by hand into the deliverable report.
[Collection("RoslynWorkspace")]
public sealed class SymbolIdentityResolvePerfBaselineTests
{
    private readonly ITestOutputHelper _output;

    public SymbolIdentityResolvePerfBaselineTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Baseline_SameId_And_DifferentIds_Across_1_10_100_1000()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var compilationSize = compiled.Index.Records.Count;
            _output.WriteLine($"# Compilation size (indexed records): {compilationSize}");

            // Collect N distinct type FspmSymbolIds from the
            // compilation. They must be real, resolvable, and
            // independent so "different ids" exercises the
            // symbol-scan cost rather than the per-id cache.
            var distinctIds = compiled.Index.Records
                .Select(r => IdentityFor(r.Symbol))
                .Where(id => !string.IsNullOrEmpty(id.Value) && id.Value.Contains('|'))
                .Take(1000)
                .ToArray();

            if (distinctIds.Length < 1000)
            {
                _output.WriteLine(
                    $"# WARNING: only {distinctIds.Length} distinct ids available; the 1000-row will use the available pool.");
            }

            var availableCount = distinctIds.Length;

            // Workload rows: count = 1, 10, 100, 1000.
            var counts = new[] { 1, 10, 100, 1000 };

            _output.WriteLine("## Same id repeatedly");
            _output.WriteLine("count | elapsed_ms | alloc_bytes | gen0 | gen1 | gen2");
            var sameId = distinctIds[0];
            foreach (var c in counts)
            {
                var (ms, bytes, g0, g1, g2) = MeasureSameId(sameId, compilation, c);
                _output.WriteLine($"{c,5} | {ms,10:F3} | {bytes,10} | {g0,4} | {g1,4} | {g2,4}");
            }

            _output.WriteLine("## Different ids");
            _output.WriteLine("count | elapsed_ms | alloc_bytes | gen0 | gen1 | gen2");
            foreach (var c in counts)
            {
                var take = Math.Min(c, availableCount);
                var (ms, bytes, g0, g1, g2) = MeasureDifferentIds(distinctIds, compilation, take);
                _output.WriteLine($"{c,5} | {ms,10:F3} | {bytes,10} | {g0,4} | {g1,4} | {g2,4}");
            }
        }
    }

    private static FspmSymbolId IdentityFor(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol t => FspmSymbolIdentity.Create(t),
        IPropertySymbol p => FspmSymbolIdentity.Create(p),
        IFieldSymbol f => FspmSymbolIdentity.Create(f),
        IEventSymbol e => FspmSymbolIdentity.Create(e),
        IMethodSymbol m => FspmSymbolIdentity.Create(m),
        _ => default,
    };

    private static (double ElapsedMs, long AllocBytes, int Gen0, int Gen1, int Gen2) MeasureSameId(
        FspmSymbolId id, Compilation compilation, int iterations)
    {
        // Warm up: one call so JIT / caches stabilise before we time
        // the real workload. Resolve throws on NotFound/Ambiguous
        // here because the real Golden fixture produces exactly one
        // match per id; we suppress with try/catch so a fixture
        // change does not skip the baseline row.
        try { _ = FspmSymbolIdentity.Resolve(id, compilation); } catch { /* measure anyway */ }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var startBytes = GC.GetTotalMemory(forceFullCollection: false);
        var startG0 = GC.CollectionCount(0);
        var startG1 = GC.CollectionCount(1);
        var startG2 = GC.CollectionCount(2);
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            try { _ = FspmSymbolIdentity.Resolve(id, compilation); } catch { /* measure anyway */ }
        }
        sw.Stop();
        var endBytes = GC.GetTotalMemory(forceFullCollection: false);
        var g0 = GC.CollectionCount(0) - startG0;
        var g1 = GC.CollectionCount(1) - startG1;
        var g2 = GC.CollectionCount(2) - startG2;
        return (sw.Elapsed.TotalMilliseconds, endBytes - startBytes, g0, g1, g2);
    }

    private static (double ElapsedMs, long AllocBytes, int Gen0, int Gen1, int Gen2) MeasureDifferentIds(
        FspmSymbolId[] ids, Compilation compilation, int iterations)
    {
        // Warm up: walk once so JIT / caches stabilise.
        for (var i = 0; i < Math.Min(iterations, ids.Length); i++)
        {
            try { _ = FspmSymbolIdentity.Resolve(ids[i % ids.Length], compilation); } catch { /* measure anyway */ }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var startBytes = GC.GetTotalMemory(forceFullCollection: false);
        var startG0 = GC.CollectionCount(0);
        var startG1 = GC.CollectionCount(1);
        var startG2 = GC.CollectionCount(2);
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            try { _ = FspmSymbolIdentity.Resolve(ids[i % ids.Length], compilation); } catch { /* measure anyway */ }
        }
        sw.Stop();
        var endBytes = GC.GetTotalMemory(forceFullCollection: false);
        var g0 = GC.CollectionCount(0) - startG0;
        var g1 = GC.CollectionCount(1) - startG1;
        var g2 = GC.CollectionCount(2) - startG2;
        return (sw.Elapsed.TotalMilliseconds, endBytes - startBytes, g0, g1, g2);
    }
}
