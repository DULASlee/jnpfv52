using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Constraints;

namespace JNPF.Runtime.Capability.Modes;

/// <summary>
/// VerifyMode：在 Audit 基础上增加 Build / Test。Capability 严格递增 (M11)。
///
/// Capability：Observe / Evaluate / Reflect / ReadEvidence / Build / Test。
/// 不允许 WriteEvidence / ApplyApprovedPatch / ModifyState (M11)。
/// </summary>
public sealed class VerifyMode : IMode
{
    public static readonly ModeCapabilitySet DefaultCapabilities = new(new[]
    {
        Capability.Observe,
        Capability.Evaluate,
        Capability.Reflect,
        Capability.ReadEvidence,
        Capability.Build,
        Capability.Test
    });

    public static readonly ConstraintSet DefaultConstraints = new(new IConstraint[]
    {
        new CapabilityConstraint(Capability.WriteEvidence),
        new CapabilityConstraint(Capability.ApplyApprovedPatch),
        new CapabilityConstraint(Capability.ModifyState)
    });

    public const string DefaultName = "Verify";

    public const string DefaultDescriptionText =
        "审计 + 验证 Mode：在 Audit 基础上允许 Build 与 Test，但不允许写入或修改状态 (M11)。";

    public ModeType Type => ModeType.Verify;

    public ModeCapabilitySet Capabilities => DefaultCapabilities;

    public ConstraintSet Constraints => DefaultConstraints;

    public string DisplayName => DefaultName;

    public string Description => DefaultDescriptionText;
}
