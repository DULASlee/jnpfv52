namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-03: one attached reference inside a construction. ParentId is the
/// owning node id; Role is the slot name ("entity", "field", "submit",
/// …); Reference is the P14-02 reference; TargetIdentity/TargetFingerprint
/// are resolved snapshots carried for audit (never re-resolved silently).
/// </summary>
public sealed record FspmConstructionEdge(
    string ParentId,
    string Role,
    FspmSemanticReference Reference,
    string TargetIdentity,
    string TargetFingerprint);
