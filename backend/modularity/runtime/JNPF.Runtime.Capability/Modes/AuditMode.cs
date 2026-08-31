using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Constraints;

namespace JNPF.Runtime.Capability.Modes;

/// <summary>
/// AuditMode：最小特权 Mode，默认开启 (M9)。
///
/// Capability：Observe / Evaluate / Reflect / ReadEvidence。
/// 不允许 Build / Test / WriteEvidence / ApplyApprovedPatch / ModifyState (M11)。
/// </summary>
public sealed class AuditMode : IMode
{
    public static readonly ModeCapabilitySet DefaultCapabilities = new(new[]
    {
        Capability.Observe,
        Capability.Evaluate,
        Capability.Reflect,
        Capability.ReadEvidence
    });

    public static readonly ConstraintSet DefaultConstraints = new(Array.Empty<IConstraint>());

    public const string DefaultName = "Audit";

    public const string DefaultDescriptionText =
        "最小特权 Mode：仅允许观察、评估、反思与读取 Evidence。Section 9 默认开启 (M9)。";

    public ModeType Type => ModeType.Audit;

    public ModeCapabilitySet Capabilities => DefaultCapabilities;

    public ConstraintSet Constraints => DefaultConstraints;

    public string DisplayName => DefaultName;

    public string Description => DefaultDescriptionText;
}
