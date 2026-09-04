namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-02: a reference points at a SemanticIdentity, never at a name
/// string. DisplayName is display/lookup notation only ("User.Create");
/// OwnerId constrains the expected declaring type (empty = unchecked);
/// ExpectedFingerprint enables stale detection (empty = unchecked).
/// </summary>
public abstract record FspmSemanticReference(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string OwnerId,
    string ExpectedFingerprint);

/// <summary>P14-02-A: reference to a model Type.</summary>
public sealed record FspmTypeRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmSemanticReference(TargetIdentity, DisplayName, OwnerId, ExpectedFingerprint);

/// <summary>
/// P14-02-B: reference to a type in its entity role. Resolves against
/// model Types like TypeRef; the distinct record type keeps the entity
/// role explicit for future P14-03 construction (no behavior fork today).
/// </summary>
public sealed record FspmEntityRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmSemanticReference(TargetIdentity, DisplayName, OwnerId, ExpectedFingerprint);
