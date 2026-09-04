namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-04: read-only evaluation context. The evaluator never mutates the
/// model, never resolves symbols, never touches Roslyn: it matches
/// LogicalIds against the already-bound model entries.
/// </summary>
public sealed record FspmRuleContext(
    FspmSemanticModel Model,
    string SnapshotId);
