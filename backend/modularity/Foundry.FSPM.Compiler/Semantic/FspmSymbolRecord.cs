using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 13 — single inventory entry that pairs a real Roslyn symbol with
/// its <see cref="FspmSymbolId"/> and a primary <see cref="FspmSourceLocation"/>.
/// Caller-facing code never sees the raw Roslyn symbol when describing
/// resolved results, but the symbol instance is preserved so P14 binder
/// can rebind or query members without re-fetching.
/// </summary>
public sealed class FspmSymbolRecord
{
    public FspmSymbolRecord(ISymbol symbol, FspmSymbolId id, FspmSourceLocation location)
    {
        Symbol = symbol;
        Identity = id;
        Location = location;
    }

    public ISymbol Symbol { get; }
    public FspmSymbolId Identity { get; }
    public FspmSourceLocation Location { get; }
    public FspmSymbolKind Kind => FspmSymbolIdentity.GetKind(Identity);

    public override string ToString() => $"{Kind} {Identity.Value} @ {Location.DocumentPath}:{Location.StartLine}:{Location.StartColumn}";
}
