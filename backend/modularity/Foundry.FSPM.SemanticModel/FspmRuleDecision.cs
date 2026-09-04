namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-04: one rule decision. Never a bare bool: Outcome plus Reason plus
/// the subject/target identities and fingerprints that justify it, plus
/// the anchor when the subject is addressable.
/// </summary>
public sealed record FspmRuleDecision(
    string RuleId,
    bool Passed,
    string Reason,
    string SubjectIdentity,
    string SubjectFingerprint,
    string TargetIdentity,
    string TargetFingerprint,
    FspmSemanticAnchor? Anchor);
