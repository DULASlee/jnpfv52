namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-02: reference resolution states. Valid/Stale resolve (IsResolved);
/// the rest do not. Stale means the target keeps its logical identity
/// but its fingerprint changed — never merged into NotFound. Invalid
/// means the reference itself is malformed (unknown record kind,
/// fingerprint pin on an unfingerprintable target).
/// </summary>
public enum FspmReferenceStatus
{
    Valid,
    Missing,
    Ambiguous,
    WrongOwner,
    WrongKind,
    Stale,
    Invalid,
}
