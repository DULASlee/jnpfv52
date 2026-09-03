using Foundry.FSPM.Compiler.Symbols;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 9 — 抽象引用基类. A reference is a Model-side pointer to a single
/// declared FSPM semantic element, addressable ONLY by its
/// <see cref="FspmSymbolId"/> (its real, reusable identity).
///
/// <para>Three concrete kinds exist (one per FSPM declaration type):</para>
/// <list type="bullet">
/// <item><see cref="FspmEntityReference"/></item>
/// <item><see cref="FspmPropertyReference"/></item>
/// <item><see cref="FspmOperationReference"/></item>
/// </list>
///
/// <para>A reference is RESOLVABLE iff <see cref="FspmSymbolId"/> points at a
/// declaration in the owning <see cref="FspmSemanticModel"/>. Resolution is
/// always <c>O(1)</c> against the model's by-id indexes — no string lookup.</para>
///
/// <para>No reference carries a <c>string Name</c> as identity. The
/// declaration name is preserved for diagnostics only.</para>
/// </summary>
public abstract record FspmSemanticReference(
    FspmSymbolId SymbolId,
    string DeclarationName);

/// <summary>An FSPM entity reference. The DeclarationName is source text, never used as identity.</summary>
public sealed record FspmEntityReference(
    FspmSymbolId SymbolId,
    string DeclarationName) : FspmSemanticReference(SymbolId, DeclarationName);

/// <summary>An FSPM property reference. The DeclarationName is "EntityName.PropertyName" (display only).</summary>
public sealed record FspmPropertyReference(
    FspmSymbolId SymbolId,
    string DeclarationName) : FspmSemanticReference(SymbolId, DeclarationName);

/// <summary>An FSPM operation reference. The DeclarationName is "EntityName.OperationName" (display only).</summary>
public sealed record FspmOperationReference(
    FspmSymbolId SymbolId,
    string DeclarationName) : FspmSemanticReference(SymbolId, DeclarationName);
