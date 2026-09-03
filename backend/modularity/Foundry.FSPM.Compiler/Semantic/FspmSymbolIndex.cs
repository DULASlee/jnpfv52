using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 13 — read-only inventory of every <see cref="FspmSymbolRecord"/>
/// reachable from a single <see cref="FspmCompilationSnapshot"/>. Index
/// keys are stable identities (DocId + assembly); values carry the live
/// Roslyn symbol so P14 can rebind without re-walking the project.
/// </summary>
public sealed class FspmSymbolIndex
{
    private readonly Dictionary<FspmSymbolId, FspmSymbolRecord> _byId = new();
    private readonly List<FspmSymbolRecord> _records = new();

    public FspmSymbolIndex(IEnumerable<FspmSymbolRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        foreach (var r in records)
        {
            _records.Add(r);
            _byId[r.Identity] = r;
        }
    }

    public IReadOnlyList<FspmSymbolRecord> Records => _records;

    public bool TryGet(FspmSymbolId id, out FspmSymbolRecord? record) =>
        _byId.TryGetValue(id, out record);

    public IEnumerable<FspmSymbolRecord> OfKind(FspmSymbolKind kind) =>
        _records.Where(r => r.Kind == kind);

    public int Count => _records.Count;
}
