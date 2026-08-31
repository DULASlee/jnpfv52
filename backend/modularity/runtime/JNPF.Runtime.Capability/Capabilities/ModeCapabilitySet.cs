using System.Collections.Generic;

namespace JNPF.Runtime.Capability.Capabilities;

/// <summary>
/// 不可变 Capability 集合（Section 9 M5 / M11）。
///
/// 设计要点：
///   - 构造后内部集合不可修改；
///   - <see cref="Allows"/> 是 Capability Whitelist 检查的唯一入口；
///   - <see cref="IsStrictSubsetOf"/> 用于验证 M11 "严格递增"。
///
/// 不持有 Runtime 引用，不引入 State，可安全跨 Session 传递。
/// </summary>
public sealed class ModeCapabilitySet
{
    private readonly HashSet<Capability> _capabilities;

    public ModeCapabilitySet(IEnumerable<Capability> capabilities)
    {
        if (capabilities is null)
        {
            throw new ArgumentNullException(nameof(capabilities));
        }

        _capabilities = new HashSet<Capability>(capabilities);

        // M11 + Constraint-14：任何 Mode Capability 集合严禁包含未审批变更能力。
        if (_capabilities.Contains(Capability.ApplyUnapprovedChange))
        {
            throw new InvalidOperationException(
                "ModeCapabilitySet cannot contain Capability.ApplyUnapprovedChange. " +
                "ApplyUnapprovedChange is reserved for Governance-only paths (M11).");
        }
    }

    public IReadOnlyCollection<Capability> Items => _capabilities;

    public int Count => _capabilities.Count;

    public bool Allows(Capability capability) => _capabilities.Contains(capability);

    public bool IsStrictSubsetOf(ModeCapabilitySet other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        return _capabilities.IsProperSubsetOf(other._capabilities);
    }

    public bool IsStrictSupersetOf(ModeCapabilitySet other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        return _capabilities.IsProperSupersetOf(other._capabilities);
    }

    public bool IsSubsetOf(ModeCapabilitySet other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        return _capabilities.IsSubsetOf(other._capabilities);
    }

    public override bool Equals(object? obj)
        => obj is ModeCapabilitySet other && _capabilities.SetEquals(other._capabilities);

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var cap in _capabilities)
        {
            hash ^= (int)cap;
        }
        return hash;
    }

    public override string ToString()
        => "{" + string.Join(", ", _capabilities) + "}";
}
