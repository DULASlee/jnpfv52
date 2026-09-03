namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01 STEP 6-J: pure-data semantic relation. Kind is one of
/// "Inheritance", "Implementation", "Override",
/// "ExplicitInterfaceImplementation". Target is the Roslyn display string
/// of the other end; ResolvedTargetId stays empty in P14-01 — resolving
/// targets to model identities is P14-02 (Reference System) work.
/// Containment (Declares/Contains) is already carried by
/// Member/Operation DeclaringTypeId, not duplicated here.
/// </summary>
public sealed record FspmSemanticRelation(
    string FromId,
    string Kind,
    string Target,
    string ResolvedTargetId);
