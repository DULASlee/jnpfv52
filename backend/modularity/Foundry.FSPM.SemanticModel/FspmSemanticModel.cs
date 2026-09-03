namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01 STEP 6-K: build provenance of one assembled model.
/// </summary>
public sealed record FspmSemanticModelMetadata(
    string SnapshotId,
    string SourceAssembly,
    int FactCount);

/// <summary>
/// P14-01 STEP 6-K: the assembled semantic model root. All collections
/// are pure records; Parameters is the flattened view of every
/// operation's parameter list (computed at assembly, stored once).
/// Diagnostics are binder notes (projection-time observations), not
/// P14-04 rule verdicts.
/// </summary>
public sealed record FspmSemanticModel(
    IReadOnlyList<FspmSemanticType> Types,
    IReadOnlyList<FspmSemanticMember> Members,
    IReadOnlyList<FspmSemanticOperation> Operations,
    IReadOnlyList<FspmSemanticParameter> Parameters,
    IReadOnlyList<FspmSemanticRelation> Relations,
    IReadOnlyList<string> Diagnostics,
    FspmSemanticModelMetadata Metadata);
