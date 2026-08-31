using JNPF.Runtime.Capability;
using JNPF.Runtime.Capability.Loading;
using JNPF.Runtime.Capability.Modes;
using JNPF.Runtime.Capability.Registry;
using Xunit;

namespace JNPF.Tests.Section9.Modes;

/// <summary>
/// Section 9 Provider Lifetime 验证 (Gate-9-5 + Iron Law-05 + §3.4)。
///
/// 验证目标：
///   - 多次 Resolve 同一 ModeType 返回不同实例（Transient 行为）；
///   - Provider 无 Singleton / Static Cache；
///   - Registry 与 Provider 互不污染。
/// </summary>
public sealed class ModeLifetimeTests
{
    [Fact]
    public void Provider_Resolve_TwiceSameType_ReturnsDifferentInstances()
    {
        var provider = new DefaultModeProvider();

        var first = provider.Resolve(ModeType.Audit);
        var second = provider.Resolve(ModeType.Audit);

        Assert.False(ReferenceEquals(first, second),
            "Provider.Resolve must return a new IMode instance per call (Transient lifetime).");
    }

    [Fact]
    public void Provider_Resolve_DifferentTypes_ReturnsDifferentInstances()
    {
        var provider = new DefaultModeProvider();

        var audit = provider.Resolve(ModeType.Audit);
        var verify = provider.Resolve(ModeType.Verify);

        Assert.False(ReferenceEquals(audit, verify));
    }

    [Fact]
    public void Provider_Resolve_AllFourDefaultTypes_Work()
    {
        var provider = new DefaultModeProvider();

        Assert.Equal(ModeType.Audit, provider.Resolve(ModeType.Audit).Type);
        Assert.Equal(ModeType.Verify, provider.Resolve(ModeType.Verify).Type);
        Assert.Equal(ModeType.Execute, provider.Resolve(ModeType.Execute).Type);
        Assert.Equal(ModeType.Assist, provider.Resolve(ModeType.Assist).Type);
    }

    [Fact]
    public void Provider_Resolve_UnknownType_Throws()
    {
        var provider = new DefaultModeProvider();
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.Resolve((ModeType)999));
    }

    [Fact]
    public void Provider_RepeatedCalls_DoNotShareState()
    {
        var provider = new DefaultModeProvider();

        var firstExecute = provider.Resolve(ModeType.Execute);
        var secondExecute = provider.Resolve(ModeType.Execute);

        // 两个实例虽然 Capability Set 不可变且语义相同，但必须是不同对象
        Assert.False(ReferenceEquals(firstExecute, secondExecute));
        Assert.NotSame(firstExecute, secondExecute);

        // Capability Set 内容相等（Equals 重载 SetEquals）
        Assert.Equal(firstExecute.Capabilities, secondExecute.Capabilities);
    }

    [Fact]
    public void Provider_HasNoStaticCache()
    {
        // Iron Law-05 + Gate-9-5：Provider 不得维护 Static / Singleton Cache。
        var providerType = typeof(DefaultModeProvider);
        var staticFields = providerType.GetFields(
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        Assert.Empty(staticFields.Where(f => f.FieldType == typeof(IMode)));
        Assert.Empty(staticFields.Where(f => f.FieldType == typeof(IModeProvider)));
    }

    [Fact]
    public void Registry_RepeatedQueries_ReturnSameDescriptorReference()
    {
        // Registry 是元数据，可共享 Descriptor（不可变）。
        var registry = new DefaultModeRegistry();

        var first = registry.GetDescriptor(ModeType.Audit);
        var second = registry.GetDescriptor(ModeType.Audit);

        Assert.Same(first, second);
    }

    [Fact]
    public void Provider_And_Registry_AreIndependent()
    {
        var provider = new DefaultModeProvider();
        var registry = new DefaultModeRegistry();

        var providerInstance = provider.Resolve(ModeType.Audit);
        var registryDescriptor = registry.GetDescriptor(ModeType.Audit);

        Assert.False(ReferenceEquals(providerInstance, registryDescriptor));
        Assert.Equal(ModeType.Audit, providerInstance.Type);
        Assert.Equal(ModeType.Audit, registryDescriptor.Type);
    }
}
