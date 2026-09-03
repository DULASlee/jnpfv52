using Foundry.FSPM.Compiler.Symbols;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 13 — outcome of every resolver call. P13 NEVER returns a
/// partially-resolved value: callers must inspect <see cref="Status"/>
/// first. <see cref="Candidates"/> is populated for both Resolved
/// (single, the winner) and Ambiguous (the full candidate set) so audit
/// tooling can replay decisions.
/// <para>P13-H7 extends the original four states. Semantic states
/// (Resolved/NotFound/Ambiguous/Invalid/Unsupported/Degraded) describe
/// the resolution itself; execution states (Cancelled/
/// InfrastructureFailure) describe the run. See
/// <see cref="FspmResolutionStatusExtensions.Classify"/> — P15 must
/// never mistake an infrastructure failure for a semantic verdict.
/// </para>
/// </summary>
public enum FspmResolutionStatus
{
    Resolved,
    NotFound,
    Ambiguous,
    Invalid,
    Unsupported,
    Degraded,
    Cancelled,
    InfrastructureFailure,
}

/// <summary>
/// P13-H7: semantic vs execution classification of resolution states.
/// </summary>
public enum ResolutionOutcomeClass
{
    Semantic,
    Execution,
}

/// <summary>
/// P13-H7: classifies every <see cref="FspmResolutionStatus"/> so
/// downstream phases cannot confuse infrastructure failures with
/// semantic verdicts.
/// </summary>
public static class FspmResolutionStatusExtensions
{
    public static ResolutionOutcomeClass Classify(this FspmResolutionStatus status) => status switch
    {
        FspmResolutionStatus.Cancelled => ResolutionOutcomeClass.Execution,
        FspmResolutionStatus.InfrastructureFailure => ResolutionOutcomeClass.Execution,
        _ => ResolutionOutcomeClass.Semantic,
    };
}

public sealed record FspmResolutionResult(
    FspmResolutionStatus Status,
    IReadOnlyList<FspmSymbolRecord> Candidates,
    string Reason,
    FspmSourceLocation? RequestLocation)
{
    public FspmSymbolRecord? Selected => Candidates.Count == 1 ? Candidates[0] : null;

    public FspmSymbolId? SelectedId => Selected?.Identity;

    /// <summary>True only for <see cref="FspmResolutionStatus.Resolved"/>.</summary>
    public bool IsResolved => Status == FspmResolutionStatus.Resolved;

    public static FspmResolutionResult NotFoundResult(string reason, FspmSourceLocation? at) =>
        new(FspmResolutionStatus.NotFound, Array.Empty<FspmSymbolRecord>(), reason, at);

    public static FspmResolutionResult InvalidResult(string reason, FspmSourceLocation? at) =>
        new(FspmResolutionStatus.Invalid, Array.Empty<FspmSymbolRecord>(), reason, at);

    public static FspmResolutionResult AmbiguousResult(
        IReadOnlyList<FspmSymbolRecord> candidates, string reason, FspmSourceLocation? at) =>
        new(FspmResolutionStatus.Ambiguous, candidates, reason, at);

    public static FspmResolutionResult ResolvedResult(
        FspmSymbolRecord record, FspmSourceLocation? at, string reason = "OK") =>
        new(
            FspmResolutionStatus.Resolved,
            new FspmSymbolRecord[] { record },
            reason,
            at);
}
