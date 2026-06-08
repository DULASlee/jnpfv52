using System.Reflection;

namespace JNPF.Modules;

/// <summary>
/// 模块描述符 — 封装单个模块的元数据.
/// </summary>
public sealed class ModuleDescriptor
{
    /// <summary>
    /// 模块类型.
    /// </summary>
    public Type ModuleType { get; }

    /// <summary>
    /// 模块名称.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 依赖的模块类型列表.
    /// </summary>
    public IReadOnlyList<Type> Dependencies { get; }

    public ModuleDescriptor(Type moduleType)
    {
        ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
        Name = moduleType.Name;
        Dependencies = moduleType.GetCustomAttribute<DependsOnAttribute>()?.Dependencies
            ?? Array.Empty<Type>();
    }
}
