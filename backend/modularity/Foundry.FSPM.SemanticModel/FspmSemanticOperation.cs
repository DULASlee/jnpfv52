namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01 STEP 6-H: pure-data semantic operation (Method/Constructor).
/// Overloads are distinct entries (distinct LogicalIds); generic arity
/// participates. Indexer facts bind here with empty parameters.
/// </summary>
public sealed record FspmSemanticOperation(
    FspmSemanticIdentity Identity,
    string Name,
    string DeclaringTypeId,
    string OperationKind,
    IReadOnlyList<FspmSemanticParameter> Parameters,
    string ReturnType,
    int GenericArity,
    string Fingerprint,
    FspmSemanticAnchor Anchor,
    FspmSemanticState State);
