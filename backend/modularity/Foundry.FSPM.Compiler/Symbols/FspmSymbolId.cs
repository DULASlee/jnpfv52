namespace Foundry.FSPM.Compiler.Symbols;

/// <summary>
/// Phase 7 — 施工包 §26. Deterministic, comparable, rebuildable FSPM Symbol Identity.
///
/// <para>
/// Canonical form (see <see cref="FspmSymbolIdentity"/> for the decision):
/// <c>{AssemblySimpleName}|{Roslyn DocumentationCommentId}</c>, e.g.
/// <c>SemanticGolden|T:SemanticGolden.Domain.User</c>.
/// </para>
///
/// <para>
/// A <c>readonly record struct</c> gives value equality and stable hashing,
/// so the same real symbol always yields the same ID across runs,
/// compilations, and processes — without relying on object identity.
/// </para>
///
/// <para>
/// Deliberately NOT <c>Foundry.FSPM.Core.Semantic.SemanticIdentity</c>:
/// that frozen shape (INTERFACE_LOCKDOWN §1.3) is not yet implemented on any
/// branch. This type's canonical payload maps 1:1 onto its field set
/// (Assembly / Namespace+MetadataName / ContainingType / GenericArity /
/// Kind), so a future mechanical migration is possible without semantic change.
/// </para>
/// </summary>
public readonly record struct FspmSymbolId(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
