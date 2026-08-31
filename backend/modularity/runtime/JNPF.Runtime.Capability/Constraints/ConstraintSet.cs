using System.Collections.Generic;
using JNPF.Runtime.Capability.Capabilities;

namespace JNPF.Runtime.Capability.Constraints;

/// <summary>
/// 不可变 Mode 约束集合。
///
/// 约束本身可被 <see cref="IsSatisfiedBy"/> 验证，用于 Capability 矩阵 Test-2 / Gate-9-3。
/// </summary>
public sealed class ConstraintSet
{
    private readonly IReadOnlyList<IConstraint> _constraints;

    public ConstraintSet(IEnumerable<IConstraint> constraints)
    {
        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        _constraints = constraints.ToList();
    }

    public IReadOnlyList<IConstraint> Items => _constraints;

    public int Count => _constraints.Count;

    /// <summary>
    /// 验证给定 Capability Set 是否满足本约束集。
    /// </summary>
    public bool IsSatisfiedBy(ModeCapabilitySet capabilities)
    {
        if (capabilities is null)
        {
            throw new ArgumentNullException(nameof(capabilities));
        }

        foreach (var constraint in _constraints)
        {
            if (constraint is CapabilityConstraint cap)
            {
                if (!cap.IsSatisfiedBy(capabilities))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
