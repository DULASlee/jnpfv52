using JNPF.Runtime.Capability;
using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Loading;
using JNPF.Runtime.Capability.Modes;
using Xunit;

namespace JNPF.Tests.Section9.Modes;

/// <summary>
/// Section 9 Capability 严格递增矩阵验证 (M11 + Test-2 + Gate-9-3)。
///
/// 严格递增关系：
///   Audit ⊊ Verify ⊊ Execute ⊊ Assist
///
/// 注：Assist 在 Phase 1 默认实现中 Capability 等同 Execute（Profile 扩展为 Section 10 职责），
/// 因此 Phase 1 阶段 Assist 与 Execute Capability 相等（不是严格递增）。
/// 本测试验证 Audit ⊊ Verify ⊊ Execute 这三段的严格递增关系 + Assist 不再扩张。
/// </summary>
public sealed class CapabilityMatrixTests
{
    private readonly IModeProvider _provider = new DefaultModeProvider();

    [Fact]
    public void Audit_IsStrictSubsetOf_Verify()
    {
        var audit = _provider.Resolve(ModeType.Audit);
        var verify = _provider.Resolve(ModeType.Verify);

        Assert.True(audit.Capabilities.IsStrictSubsetOf(verify.Capabilities),
            "Audit Capability Set must be a strict subset of Verify Capability Set (M11).");
    }

    [Fact]
    public void Verify_IsStrictSubsetOf_Execute()
    {
        var verify = _provider.Resolve(ModeType.Verify);
        var execute = _provider.Resolve(ModeType.Execute);

        Assert.True(verify.Capabilities.IsStrictSubsetOf(execute.Capabilities),
            "Verify Capability Set must be a strict subset of Execute Capability Set (M11).");
    }

    [Fact]
    public void Audit_IsStrictSubsetOf_Execute()
    {
        var audit = _provider.Resolve(ModeType.Audit);
        var execute = _provider.Resolve(ModeType.Execute);

        Assert.True(audit.Capabilities.IsStrictSubsetOf(execute.Capabilities));
    }

    [Fact]
    public void NoDefaultMode_ContainsApplyUnapprovedChange()
    {
        // M11 + Constraint-14：任何 Default Mode 不得包含 ApplyUnapprovedChange。
        foreach (var modeType in Enum.GetValues<ModeType>())
        {
            var mode = _provider.Resolve(modeType);
            Assert.False(mode.Capabilities.Allows(Capability.ApplyUnapprovedChange),
                $"{modeType} Mode must NOT contain ApplyUnapprovedChange (M11 + Constraint-14).");
        }
    }

    [Fact]
    public void Audit_DoesNotAllowBuild()
    {
        var audit = _provider.Resolve(ModeType.Audit);
        Assert.False(audit.Capabilities.Allows(Capability.Build));
    }

    [Fact]
    public void Verify_AllowsBuild_ButNotModifyState()
    {
        var verify = _provider.Resolve(ModeType.Verify);
        Assert.True(verify.Capabilities.Allows(Capability.Build));
        Assert.False(verify.Capabilities.Allows(Capability.ModifyState));
    }

    [Fact]
    public void Execute_AllowsAllStagesExceptUnapprovedChange()
    {
        var execute = _provider.Resolve(ModeType.Execute);

        var stages = new[]
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
        };

        foreach (var stage in stages)
        {
            Assert.True(execute.Capabilities.Allows(stage),
                $"ExecuteMode must allow {stage}.");
        }
    }

    [Fact]
    public void Audit_IsTheMinimalCapabilitySet()
    {
        var audit = _provider.Resolve(ModeType.Audit);
        var expected = new HashSet<Capability>
        {
            Capability.Observe,
            Capability.Evaluate,
            Capability.Reflect,
            Capability.ReadEvidence
        };

        Assert.Equal(expected.Count, audit.Capabilities.Count);
        foreach (var cap in expected)
        {
            Assert.True(audit.Capabilities.Allows(cap));
        }
    }

    [Fact]
    public void CapabilitySet_StrictInclusion_ProducesStrictCountDifference()
    {
        var audit = _provider.Resolve(ModeType.Audit);
        var verify = _provider.Resolve(ModeType.Verify);

        Assert.True(verify.Capabilities.Count > audit.Capabilities.Count);
    }
}
