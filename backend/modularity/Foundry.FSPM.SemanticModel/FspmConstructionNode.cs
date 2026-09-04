namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-03: one node in a semantic construction (e.g. a Form, a field
/// slot, a submit slot). Id is path-derived at compose time
/// ("parentId/kind:name"); roots use "kind:name". Owner is the parent
/// node id (empty for roots). All plain data.
/// </summary>
public sealed record FspmConstructionNode(
    string Id,
    string Kind,
    string Name,
    string Owner,
    FspmConstructionState State);
