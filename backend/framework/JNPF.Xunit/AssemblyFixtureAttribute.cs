namespace JNPF.Xunit;

/// <summary>
/// 通过特性方式任何类型
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class AssemblyFixtureAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fixtureType"></param>
    public AssemblyFixtureAttribute(Type fixtureType)
    {
        FixtureType = fixtureType;
    }

    /// <summary>
    /// 单元测试实例构造函数修复类型
    /// </summary>
    public Type FixtureType { get; private set; }
}