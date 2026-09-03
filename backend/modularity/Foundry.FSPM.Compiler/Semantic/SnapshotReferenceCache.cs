using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H8/perf: caches the emitted snapshot-assembly image per
/// <see cref="Compilation"/> so <c>ResolveExpression</c> does not pay a
/// full emit on every call. Lifetime follows the compilation
/// (ConditionalWeakTable — no unbounded growth, no manual invalidation).
/// Returns null when the snapshot cannot be emitted.
/// </summary>
internal static class SnapshotReferenceCache
{
    private static readonly ConditionalWeakTable<Compilation, StrongBox<MetadataReference?>> Cache = new();
    private static readonly object Gate = new();

    internal static MetadataReference? GetOrEmit(Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        lock (Gate)
        {
            if (Cache.TryGetValue(compilation, out var box))
            {
                return box.Value;
            }

            MetadataReference? reference = null;
            using (var stream = new MemoryStream())
            {
                if (compilation.Emit(stream).Success)
                {
                    reference = MetadataReference.CreateFromImage(stream.ToArray());
                }
            }

            Cache.Add(compilation, new StrongBox<MetadataReference?>(reference));
            return reference;
        }
    }
}
