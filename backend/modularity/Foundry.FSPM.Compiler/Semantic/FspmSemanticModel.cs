using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 9 — 施工包 §33. Aggregates the three binders' results into a
/// single, deterministic, by-id-addressable semantic surface.
///
/// <para>Invariants (directive §五 / §七 / §十):</para>
/// <list type="bullet">
/// <item>Every <see cref="FspmSymbolId"/> in the model is REUSED from a binder
/// result. No second identity mechanism exists.</item>
/// <item>Entities / properties / operations with non-Success status have
/// <c>Symbol == null</c> and are KEPT in the model with diagnostics, so the
/// model can never silently invent a fake element.</item>
/// <item>Indexes are <c>Dictionary&lt;FspmSymbolId, T&gt;</c> with
/// <see cref="FspmSymbolId"/> value equality, giving <c>O(1)</c> resolution
/// with no string-lookup fallback.</item>
/// <item>All diagnostics produced during build are collected in
/// <see cref="Diagnostics"/> — including parse diagnostics, binder
/// diagnostics, and any other builder-side error.</item>
/// </list>
///
/// <para>Cross-references between properties/operations and their owning
/// entity are by <see cref="FspmSymbolId"/> lookup, not by Roslyn
/// <see cref="ISymbol"/> identity (which is compilation-scoped, ephemeral).</para>
/// </summary>
public sealed class FspmSemanticModel
{
    private readonly Dictionary<FspmSymbolId, FspmEntity> _entitiesById;
    private readonly Dictionary<FspmSymbolId, FspmProperty> _propertiesById;
    private readonly Dictionary<FspmSymbolId, FspmOperation> _operationsById;

    public FspmSemanticModel(
        IEnumerable<FspmEntity> entities,
        IEnumerable<FspmProperty> properties,
        IEnumerable<FspmOperation> operations,
        IEnumerable<FspmDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Entities = entities.ToArray();
        Properties = properties.ToArray();
        Operations = operations.ToArray();
        Diagnostics = diagnostics.ToArray();

        // Builder guarantees one row per declaration, so a duplicate id is
        // a builder bug — fail fast rather than silently shadow.
        _entitiesById = Entities.ToDictionary(e => e.SymbolId, e => e);
        _propertiesById = Properties.ToDictionary(p => p.SymbolId, p => p);
        _operationsById = Operations.ToDictionary(o => o.SymbolId, o => o);
    }

    public IReadOnlyList<FspmEntity> Entities { get; }

    public IReadOnlyList<FspmProperty> Properties { get; }

    public IReadOnlyList<FspmOperation> Operations { get; }

    public IReadOnlyList<FspmDiagnostic> Diagnostics { get; }

    /// <summary>True iff the model has at least one Error-severity diagnostic.</summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity == FspmDiagnosticSeverity.Error);

    // O(1) by-id lookups (directive §八). Returns null when the id is unknown.
    public FspmEntity? FindEntity(FspmSymbolId id) => _entitiesById.TryGetValue(id, out var e) ? e : null;

    public FspmProperty? FindProperty(FspmSymbolId id) => _propertiesById.TryGetValue(id, out var p) ? p : null;

    public FspmOperation? FindOperation(FspmSymbolId id) => _operationsById.TryGetValue(id, out var o) ? o : null;

    /// <summary>
    /// Resolves a reference against the model. Returns the matched
    /// <see cref="FspmEntity"/>, <see cref="FspmProperty"/>, or
    /// <see cref="FspmOperation"/>; null if no match (the reference is
    /// dangling). Never throws and never falls back to string comparison.
    /// </summary>
    public ISymbol? Resolve(FspmSemanticReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return reference switch
        {
            FspmEntityReference e => FindEntity(e.SymbolId)?.Symbol,
            FspmPropertyReference p => FindProperty(p.SymbolId)?.Symbol,
            FspmOperationReference o => FindOperation(o.SymbolId)?.Symbol,
            _ => throw new ArgumentException($"Unknown reference kind: {reference.GetType()}", nameof(reference)),
        };
    }
}
