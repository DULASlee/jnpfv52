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
/// role explicit for P14-03 construction (no behavior fork today).
/// </summary>
public sealed record FspmEntityRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmSemanticReference(TargetIdentity, DisplayName, OwnerId, ExpectedFingerprint);

/// <summary>
/// P14-02-C: base of member references. ExpectedMemberKind pins the
/// accepted member kind ("Property"/"Field"/"Event"); a mismatch is
/// WrongKind, never a silent cross-kind hit.
/// </summary>
public abstract record FspmMemberRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string ExpectedMemberKind,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmSemanticReference(TargetIdentity, DisplayName, OwnerId, ExpectedFingerprint);

/// <summary>P14-02-D: reference to a model Property.</summary>
public sealed record FspmPropertyRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmMemberRef(TargetIdentity, DisplayName, "Property", OwnerId, ExpectedFingerprint);

/// <summary>P14-02-D: reference to a model Field.</summary>
public sealed record FspmFieldRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmMemberRef(TargetIdentity, DisplayName, "Field", OwnerId, ExpectedFingerprint);

/// <summary>P14-02-D: reference to a model Event.</summary>
public sealed record FspmEventRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmMemberRef(TargetIdentity, DisplayName, "Event", OwnerId, ExpectedFingerprint);

/// <summary>
/// P14-02-E: reference to a model Operation (Method/Constructor/Indexer).
/// Overloads carry distinct identities; no signature guessing here.
/// </summary>
public sealed record FspmOperationRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmSemanticReference(TargetIdentity, DisplayName, OwnerId, ExpectedFingerprint);

/// <summary>
/// P14-02-F: reference to one operation parameter. Parameters are
/// embedded (no standalone identity): TargetIdentity is the OWNER
/// operation's LogicalId and Position selects within its parameter
/// list. Structural addressing, documented — never a synthesized id.
/// </summary>
public sealed record FspmParameterRef(
    FspmSemanticIdentity TargetIdentity,
    string DisplayName,
    int Position,
    string OwnerId = "",
    string ExpectedFingerprint = "")
    : FspmSemanticReference(TargetIdentity, DisplayName, OwnerId, ExpectedFingerprint);
