namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01 STEP 6-G: pure-data semantic member (Property/Field/Event).
/// MemberKind carries the classifier name. DeclaringTypeId is the bound
/// owner type's LogicalId, resolved batch-scoped (empty when the owner
/// Type fact is absent from the batch — reported, never invented).
/// </summary>
public sealed record FspmSemanticMember(
    FspmSemanticIdentity Identity,
    string Name,
    string DeclaringTypeId,
    string MemberKind,
    string Type,
    string Fingerprint,
    FspmSemanticAnchor Anchor,
    FspmSemanticState State);
