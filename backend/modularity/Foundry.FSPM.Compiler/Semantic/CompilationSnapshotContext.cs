using System.Collections.Concurrent;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H8: cache statistics (hits/misses). Diagnostic only — eviction
/// policy is whole-snapshot invalidation (a new context starts empty).
/// </summary>
public sealed record SemanticCacheStatistics(int Hits, int Misses);

/// <summary>
/// P13-H8: snapshot-bound resolution context. Owns a <see cref="CSharpResolver"/>
/// plus a snapshot-scoped result cache keyed by (query kind, input).
/// <list type="bullet">
/// <item>Immutable facts out: cached <see cref="FspmResolutionResult"/> instances are never mutated.</item>
/// <item>Whole-snapshot invalidation: a new SnapshotId means a new empty cache (chief D2).</item>
/// <item>Concurrency: safe for concurrent resolves on one context.</item>
/// <item>Disposal: after <see cref="Dispose"/>, resolves return InfrastructureFailure — never invalid access.</item>
/// </list>
/// </summary>
public sealed class CompilationSnapshotContext : IDisposable
{
    private readonly ConcurrentDictionary<string, FspmResolutionResult> _cache = new();
    private readonly CSharpResolver _resolver;
    private int _hits;
    private int _misses;
    private bool _disposed;

    private CompilationSnapshotContext(FspmCompilationSnapshot snapshot, string snapshotId)
    {
        Snapshot = snapshot;
        SnapshotId = snapshotId;
        _resolver = new CSharpResolver(snapshot);
    }

    public static CompilationSnapshotContext From(FspmCompilationSnapshot snapshot, string snapshotId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrEmpty(snapshotId);
        return new CompilationSnapshotContext(snapshot, snapshotId);
    }

    public FspmCompilationSnapshot Snapshot { get; }

    public string SnapshotId { get; }

    public SemanticCacheStatistics CacheStatistics =>
        new(Volatile.Read(ref _hits), Volatile.Read(ref _misses));

    public FspmResolutionResult ResolveType(string metadataName, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return new FspmResolutionResult(
                FspmResolutionStatus.InfrastructureFailure,
                Array.Empty<FspmSymbolRecord>(),
                "Context is disposed; snapshot-bound objects must not be used.",
                null);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new FspmResolutionResult(
                FspmResolutionStatus.Cancelled,
                Array.Empty<FspmSymbolRecord>(),
                "Resolution cancelled before start.",
                null);
        }

        var key = "T:" + metadataName;
        if (_cache.TryGetValue(key, out var cached))
        {
            Interlocked.Increment(ref _hits);
            return cached;
        }

        var result = _resolver.ResolveType(metadataName);
        _cache[key] = result;
        Interlocked.Increment(ref _misses);
        return result;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
