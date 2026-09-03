namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01: logical identity of one semantic node ("logically who").
/// Shape-free: two versions of User.PhoneNumber share it even when the
/// property type changes. Plain strings only.
/// </summary>
public sealed record FspmSemanticIdentity(
    string AssemblyName,
    string Namespace,
    string ContainingTypeName,
    string MemberName,
    string MemberKind);
