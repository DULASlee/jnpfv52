namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-02: reference resolution states. Valid/Stale resolve (IsResolved);
/// Missing/Ambiguous/WrongOwner/WrongKind do not. Stale means the target
/// keeps its logical identity but its fingerprint changed — never merged
/// into NotFound.
/// </summary>
public enum FspmReferenceStatus
{
    Valid,
    Missing,
    Ambiguous,
    WrongOwner,
    WrongKind,
    Stale,
}
