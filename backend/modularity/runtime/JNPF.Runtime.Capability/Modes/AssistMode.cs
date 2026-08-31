using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Constraints;

namespace JNPF.Runtime.Capability.Modes;

/// <summary>
/// AssistMode：由 Profile 注入额外 Capability 集合 (M2)。
///
/// 本阶段（Section 9 Phase 1）：
///   - Capability 等同 Execute；
///   - Profile 扩展为 Section 10 职责，本类不依赖 Profile；
///   - 仍受 M11 ApplyUnapprovedChange 禁止约束。
///
/// 注：Profile 注入后，Assist Mode 的实际 Capability Whitelist 在 Phase 2 由 ProfileResolver 决定。
/// 本类作为 Phase 1 默认实现。
/// </summary>
public sealed class AssistMode : IMode
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
        new CapabilityConstraint(Capability.ApplyUnapprovedChange)
    });

    public const string DefaultName = "Assist";

    public const string DefaultDescriptionText =
        "协助 Mode：默认 Capability 等同 Execute；Profile 注入额外能力 (M2)。Profile 扩展由 Section 10 负责。";

    public ModeType Type => ModeType.Assist;

    public ModeCapabilitySet Capabilities => DefaultCapabilities;

    public ConstraintSet Constraints => DefaultConstraints;

    public string DisplayName => DefaultName;

    public string Description => DefaultDescriptionText;
}
