using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Constraints;

namespace JNPF.Runtime.Capability.Modes;

/// <summary>
/// ExecuteMode：在 Verify 基础上增加 WriteEvidence / ApplyApprovedPatch / ModifyState。
///
/// Capability：Observe / Evaluate / Reflect / ReadEvidence / Build / Test / WriteEvidence / ApplyApprovedPatch / ModifyState。
/// 需显式授权 (M10)。仍禁止 ApplyUnapprovedChange (M11)。
/// </summary>
public sealed class ExecuteMode : IMode
{
    public static readonly ModeCapabilitySet DefaultCapabilities = new(new[]
    {
        Capability.Observe,
        Capability.Evaluate,
        Capability.Reflect,
        Capability.ReadEvidence,
        Capability.Build,
        Capability.Test,
        Capability.WriteEvidence,
        Capability.ApplyApprovedPatch,
        Capability.ModifyState
    });

    public static readonly ConstraintSet DefaultConstraints = new(new IConstraint[]
    {
        new CapabilityConstraint(Capability.ApplyUnapprovedChange),
        new RequiresExplicitAuthorizationConstraint()
    });

    public const string DefaultName = "Execute";

    public const string DefaultDescriptionText =
        "执行 Mode：允许写入 Evidence、应用已审批 Patch 与修改状态。需显式授权 (M10)，仍禁止未审批变更 (M11)。";

    public ModeType Type => ModeType.Execute;

    public ModeCapabilitySet Capabilities => DefaultCapabilities;

    public ConstraintSet Constraints => DefaultConstraints;

    public string DisplayName => DefaultName;

    public string Description => DefaultDescriptionText;
}
