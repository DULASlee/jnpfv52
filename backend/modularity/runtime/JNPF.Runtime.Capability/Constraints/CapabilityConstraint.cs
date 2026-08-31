using JNPF.Runtime.Capability.Capabilities;

namespace JNPF.Runtime.Capability.Constraints;

/// <summary>
/// 声明 Mode 不得拥有指定 Capability 的硬约束。
///
/// 示例：ExecuteMode 必须不包含 ApplyApprovedPatch 时由显式授权拦截。
/// </summary>
public sealed class CapabilityConstraint : IConstraint
{
    public CapabilityConstraint(Capability forbidden)
    {
        Forbidden = forbidden;
        Id = $"capability:forbidden:{forbidden}";
    }

    public Capability Forbidden { get; }

    public string Id { get; }

    public bool IsSatisfiedBy(ModeCapabilitySet capabilities)
    {
        if (capabilities is null)
        {
            throw new ArgumentNullException(nameof(capabilities));
        }

        return !capabilities.Allows(Forbidden);
    }
}
