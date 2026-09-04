namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-04: reproducible semantic evidence. Answers what/why/where/
/// against-which-version without re-running the pipeline. All identity
/// fields are strings captured at decision time (never live objects).
/// </summary>
public sealed record FspmEvidence(
    string RuleId,
    bool Passed,
    string SubjectIdentity,
    string TargetIdentity,
    string SubjectFingerprint,
    string TargetFingerprint,
    FspmSemanticAnchor? Anchor,
    string Reason,
    string SnapshotVersion);
