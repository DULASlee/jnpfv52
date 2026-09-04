namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-03: a reference paired with its validation verdict. Produced by
/// validation; carries the full resolution so no Reason is ever lost
/// between Reference → Rule → Diagnostic → Evidence layers.
/// </summary>
public sealed record FspmReferenceBinding(
    FspmSemanticReference Reference,
    FspmReferenceStatus Status,
    bool IsValid,
    string Reason,
    string TargetIdentity,
    string TargetFingerprint,
    string TargetKind,
    string Owner);
