using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Binding;

/// <summary>
/// Phase 8.4 — unified lightweight binding outcome.
///
/// <para>Statuses:</para>
/// <list type="bullet">
/// <item><see cref="FspmBindingStatus.Success"/> — exactly one real symbol; <c>Symbol</c>
/// and <c>SymbolId</c> are non-null, <c>Diagnostics</c> is empty.</item>
/// <item><see cref="FspmBindingStatus.Unknown"/> — zero candidates (FSPM101/102/103).</item>
/// <item><see cref="FspmBindingStatus.Ambiguous"/> — more than one candidate (FSPM111/112/113).
/// Never resolved by First().</item>
/// <item><see cref="FspmBindingStatus.Invalid"/> — binding cannot proceed honestly:
/// the owner entity is unresolvable, or the name denotes a member of the wrong
/// kind (FSPM104). Owner diagnostics are propagated verbatim.</item>
/// </list>
///
/// <para>Deliberately NOT a semantic model: no entity/property/operation objects,
/// no aggregation. That is Phase 9.</para>
/// </summary>
public enum FspmBindingStatus
{
    Success,
    Unknown,
    Ambiguous,
    Invalid,
}

/// <summary>
/// Phase 8.4 — the single result type shared by all three binders.
/// On success carries the source declaration node, the real Roslyn symbol,
/// and its symbol ID.
/// </summary>
public sealed record FspmBindingResult(
    FspmBindingStatus Status,
    FspmSyntaxNode Declaration,
    ISymbol? Symbol,
    FspmSymbolId? SymbolId,
    IReadOnlyList<FspmDiagnostic> Diagnostics)
{
    /// <summary>True only when a single real symbol was bound.</summary>
    public bool IsSuccess => Status == FspmBindingStatus.Success;

    /// <summary>Creates a success result. Never called with a guessed symbol.</summary>
    public static FspmBindingResult Success(
        FspmSyntaxNode declaration,
        ISymbol symbol,
        FspmSymbolId symbolId)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(symbol);
        return new FspmBindingResult(
            FspmBindingStatus.Success, declaration, symbol, symbolId,
            Array.Empty<FspmDiagnostic>());
    }

    /// <summary>Creates a failure result. Diagnostics must be non-empty (no silent failure).</summary>
    public static FspmBindingResult Fail(
        FspmBindingStatus status,
        FspmSyntaxNode declaration,
        IReadOnlyList<FspmDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (status == FspmBindingStatus.Success)
        {
            throw new ArgumentException("Use Success() for success results.", nameof(status));
        }

        if (diagnostics.Count == 0)
        {
            throw new ArgumentException("Failure results must carry at least one diagnostic.", nameof(diagnostics));
        }

        return new FspmBindingResult(status, declaration, null, null, diagnostics);
    }
}
