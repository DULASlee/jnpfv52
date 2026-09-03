using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 9 — 施工包 §31. FSPM Property. One-to-one with REAL <see cref="IPropertySymbol"/>.
///
/// <para>Identity rule: <see cref="SymbolId"/> is REUSED verbatim from the
/// <see cref="FspmBindingResult"/> that built it. The model is the
/// index-by-id store; it never recomputes identity.</para>
///
/// <para>Failure rule: a property with non-Success status has
/// <c>Symbol == null</c> and carries diagnostics verbatim. The owning
/// entity is recorded (or null when the owner itself was unresolvable)
/// purely for diagnostic context — it is NOT a model lookup key.</para>
/// </summary>
public sealed record FspmProperty
{
    public required FspmSymbolId SymbolId { get; init; }

    public required IPropertySymbol? Symbol { get; init; }

    public required FspmBindingResult Binding { get; init; }

    /// <summary>The owning entity, if it resolved. May be null (owner was Unknown/Ambiguous).</summary>
    public FspmEntity? Owner { get; init; }

    public FspmBindingStatus Status => Binding.Status;

    public string? Name => Symbol?.Name;

    public string? TypeName => Symbol?.Type.ToDisplayString();

    public bool IsResolved => Status == FspmBindingStatus.Success;
}
