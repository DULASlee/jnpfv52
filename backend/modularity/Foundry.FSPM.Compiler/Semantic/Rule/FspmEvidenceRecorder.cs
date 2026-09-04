using Model = Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Semantic.Rule;

/// <summary>
/// P14-04: decision-to-evidence projection. Captures identities,
/// fingerprints, anchor, reason, and the evaluation snapshot version —
/// everything needed to answer what/why/where/against-which-version
/// without re-running the pipeline.
/// </summary>
public static class FspmEvidenceRecorder
{
    public static Model.FspmEvidence Record(Model.FspmRuleDecision decision, string snapshotVersion)
        => new(
            decision.RuleId,
            decision.Passed,
            decision.SubjectIdentity,
            decision.TargetIdentity,
            decision.SubjectFingerprint,
            decision.TargetFingerprint,
            decision.Anchor,
            decision.Reason,
            snapshotVersion);
}
