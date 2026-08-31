using JNPF.Runtime.Capability;
using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Constraints;
using JNPF.Runtime.Capability.Loading;
using JNPF.Runtime.Capability.Modes;
using JNPF.Runtime.Capability.Registry;
using Xunit;

namespace JNPF.Tests.Section9.Modes;

/// <summary>
/// Section 9 Mode Contract Purity 验证 (M16 + M14 + LOCK-H02)。
///
/// 验证目标：
///   - IMode 接口不含 Think / Prompt / Plan / Step / DAG 成员；
///   - Default Modes 不含 LLM / Intelligence 引用；
///   - Capability 集合不可包含 ApplyUnapprovedChange。
/// </summary>
public sealed class ModeContractTests
{
    [Fact]
    public void ModeCapabilitySet_RejectsApplyUnapprovedChange()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ModeCapabilitySet(new[] { Capability.ApplyUnapprovedChange }));
    }

    [Fact]
    public void ModeCapabilitySet_AcceptsValidSubset()
    {
        var set = new ModeCapabilitySet(new[] { Capability.Observe, Capability.Evaluate });
        Assert.Equal(2, set.Count);
        Assert.True(set.Allows(Capability.Observe));
        Assert.True(set.Allows(Capability.Evaluate));
        Assert.False(set.Allows(Capability.Build));
    }

    [Fact]
    public void ModeCapabilitySet_NullInputThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ModeCapabilitySet(null!));
    }

    [Fact]
    public void AuditMode_HasExpectedCapabilities()
    {
        var provider = new DefaultModeProvider();
        var audit = provider.Resolve(ModeType.Audit);

        Assert.Equal(ModeType.Audit, audit.Type);
        Assert.Equal("Audit", audit.DisplayName);
        Assert.True(audit.Capabilities.Allows(Capability.Observe));
        Assert.True(audit.Capabilities.Allows(Capability.Evaluate));
        Assert.True(audit.Capabilities.Allows(Capability.Reflect));
        Assert.True(audit.Capabilities.Allows(Capability.ReadEvidence));
        Assert.False(audit.Capabilities.Allows(Capability.Build));
        Assert.False(audit.Capabilities.Allows(Capability.ModifyState));
    }

    [Fact]
    public void ExecuteMode_RequiresExplicitAuthorization()
    {
        var provider = new DefaultModeProvider();
        var execute = provider.Resolve(ModeType.Execute);

        var hasAuthConstraint = execute.Constraints.Items
            .OfType<RequiresExplicitAuthorizationConstraint>()
            .Any();

        Assert.True(hasAuthConstraint, "ExecuteMode must declare RequiresExplicitAuthorizationConstraint (M10).");
    }

    [Fact]
    public void VerifyMode_ConstraintsForbidWritingCapabilities()
    {
        var provider = new DefaultModeProvider();
        var verify = provider.Resolve(ModeType.Verify);

        var forbidden = verify.Constraints.Items.OfType<CapabilityConstraint>().ToList();

        Assert.Contains(forbidden, c => c.Forbidden == Capability.WriteEvidence);
        Assert.Contains(forbidden, c => c.Forbidden == Capability.ApplyApprovedPatch);
        Assert.Contains(forbidden, c => c.Forbidden == Capability.ModifyState);
    }

    [Fact]
    public void ModeContract_HasNoIntelligenceMembers()
    {
        // M16 Purity Boundary：IMode 接口不得包含 Think/Prompt/Plan/Step/DAG 成员。
        var forbiddenMembers = new[]
        {
            "Think", "Prompt", "Plan", "Step", "Dag",
            "Reason", "Reasoning", "Llm", "LlmCall"
        };

        var iModeMembers = typeof(IMode)
            .GetMembers()
            .Select(m => m.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var forbidden in forbiddenMembers)
        {
            Assert.DoesNotContain(iModeMembers, m =>
                m.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Registry_ExposesAllFourDefaultModes()
    {
        var registry = new DefaultModeRegistry();

        Assert.Equal(4, registry.All.Count);
        Assert.True(registry.Contains(ModeType.Audit));
        Assert.True(registry.Contains(ModeType.Verify));
        Assert.True(registry.Contains(ModeType.Execute));
        Assert.True(registry.Contains(ModeType.Assist));
    }

    [Fact]
    public void Registry_GetDescriptor_KnownType_ReturnsDescriptor()
    {
        var registry = new DefaultModeRegistry();

        var descriptor = registry.GetDescriptor(ModeType.Audit);

        Assert.Equal(ModeType.Audit, descriptor.Type);
        Assert.Equal("Audit", descriptor.Name);
        Assert.NotNull(descriptor.Capabilities);
        Assert.NotNull(descriptor.Constraints);
    }

    [Fact]
    public void Registry_GetDescriptor_UnknownType_Throws()
    {
        var registry = new DefaultModeRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.GetDescriptor((ModeType)999));
    }

    [Fact]
    public void CapabilityConstraint_IsSatisfiedBy_AbsentCapability()
    {
        var constraint = new CapabilityConstraint(Capability.ApplyApprovedPatch);
        var capabilities = new ModeCapabilitySet(new[] { Capability.Observe });

        Assert.True(constraint.IsSatisfiedBy(capabilities));
    }

    [Fact]
    public void CapabilityConstraint_IsNotSatisfiedBy_PresentCapability()
    {
        var constraint = new CapabilityConstraint(Capability.ApplyApprovedPatch);
        var capabilities = new ModeCapabilitySet(new[] { Capability.ApplyApprovedPatch });

        Assert.False(constraint.IsSatisfiedBy(capabilities));
    }
}
