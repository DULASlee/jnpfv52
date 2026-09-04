namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-03: the construction root. Nodes and Edges are insertion-ordered
/// lists; canonical order (by Role, then TargetIdentity) plus the
/// Fingerprint are fixed at Freeze time for determinism.
/// </summary>
public sealed record FspmConstruction(
    string Id,
    string Kind,
    string Name,
    string Owner,
    IReadOnlyList<FspmConstructionNode> Nodes,
    IReadOnlyList<FspmConstructionEdge> Edges,
    FspmConstructionState State,
    string Fingerprint);
