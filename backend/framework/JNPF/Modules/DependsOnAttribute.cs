namespace JNPF.Modules;

/// <summary>
/// 声明模块依赖关系.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class DependsOnAttribute : Attribute
{
    /// <summary>
    /// 依赖的模块类型列表.
    /// </summary>
    public Type[] Dependencies { get; }

    public DependsOnAttribute(params Type[] dependencies)
    {
        Dependencies = dependencies ?? Array.Empty<Type>();
    }
}
