namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-02: the verdict of resolving one reference against one model.
/// IsResolved is true for Valid and Stale (target found); Stale additionally
/// reports the fingerprint drift in Reason. Reason is never empty.
/// </summary>
public sealed record FspmReferenceResolution(
    FspmReferenceStatus Status,
    bool IsResolved,
    string Reason,
    FspmSemanticIdentity? TargetIdentity,
    string TargetFingerprint,
    string TargetKind,
    string Owner);
