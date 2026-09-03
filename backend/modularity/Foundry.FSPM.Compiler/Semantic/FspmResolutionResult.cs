using Foundry.FSPM.Compiler.Symbols;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 13 — four-state outcome of every resolver call. P13 NEVER
/// returns a partially-resolved value: callers must inspect
/// <see cref="Status"/> first. <see cref="Candidates"/> is populated for
/// both Resolved (single, the winner) and Ambiguous (the full candidate
/// set) so audit tooling can replay decisions.
/// </summary>
public enum FspmResolutionStatus
{
    Resolved,
    NotFound,
    Ambiguous,
    Invalid,
}

public sealed record FspmResolutionResult(
    FspmResolutionStatus Status,
    IReadOnlyList<FspmSymbolRecord> Candidates,
    string Reason,
    FspmSourceLocation? RequestLocation)
{
    public FspmSymbolRecord? Selected => Candidates.Count == 1 ? Candidates[0] : null;

    public FspmSymbolId? SelectedId => Selected?.Identity;

    public static FspmResolutionResult NotFoundResult(string reason, FspmSourceLocation? at) =>
        new(FspmResolutionStatus.NotFound, Array.Empty<FspmSymbolRecord>(), reason, at);

    public static FspmResolutionResult InvalidResult(string reason, FspmSourceLocation? at) =>
        new(FspmResolutionStatus.Invalid, Array.Empty<FspmSymbolRecord>(), reason, at);

    public static FspmResolutionResult AmbiguousResult(
        IReadOnlyList<FspmSymbolRecord> candidates, string reason, FspmSourceLocation? at) =>
        new(FspmResolutionStatus.Ambiguous, candidates, reason, at);

    public static FspmResolutionResult ResolvedResult(FspmSymbolRecord record, FspmSourceLocation? at) =>
        new(
            FspmResolutionStatus.Resolved,
            new FspmSymbolRecord[] { record },
            "OK",
            at);
}
