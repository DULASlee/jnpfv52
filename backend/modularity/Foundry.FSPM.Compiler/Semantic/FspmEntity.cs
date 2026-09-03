using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 9 — 施工包 §30. FSPM Entity (one-to-one with a REAL <see cref="INamedTypeSymbol"/>).
///
/// <para>Identity rule (directive §四 / §十): <see cref="SymbolId"/> is REUSED
/// verbatim from the <see cref="FspmBindingResult"/> that built this entity.
/// No re-encoding, no string→identity side path. The symbol itself is also
/// preserved so consumers can reach back to source location / declaration.</para>
///
/// <para>Failure rule (directive §七): an entity with non-Success status has
/// <c>Symbol == null</c>, carries its diagnostics, and MUST NOT be exposed
/// under any <c>ResolvedEntities</c>-style API. False entities are
/// structurally impossible.</para>
/// </summary>
public sealed record FspmEntity
{
    public required FspmSymbolId SymbolId { get; init; }

    /// <summary>The REAL Roslyn type symbol. Null iff <see cref="Status"/> is not Success.</summary>
    public required INamedTypeSymbol? Symbol { get; init; }

    /// <summary>The original binding result, kept intact for diagnostics and traceability.</summary>
    public required FspmBindingResult Binding { get; init; }

    public FspmBindingStatus Status => Binding.Status;

    /// <summary>Display name — never used as identity.</summary>
    public string? Name => Symbol?.Name;

    /// <summary>Display namespace — never used as identity.</summary>
    public string? Namespace => Symbol?.ContainingNamespace?.ToDisplayString();

    /// <summary>Fully qualified name for display only.</summary>
    public string? QualifiedName => Symbol?.ToDisplayString();

    /// <summary>True only when bound to a real type symbol.</summary>
    public bool IsResolved => Status == FspmBindingStatus.Success;
}
