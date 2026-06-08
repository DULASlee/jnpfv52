namespace JNPF.Modules;

/// <summary>
/// 模块加载异常（循环依赖等）.
/// </summary>
public class ModuleLoadException : Exception
{
    /// <summary>
    /// 涉及的模块名称.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// 循环路径（完整环路）.
    /// </summary>
    public IReadOnlyList<Type> CircularPath { get; }

    public ModuleLoadException(string moduleName, IReadOnlyList<Type> circularPath, string message)
        : base(message)
    {
        ModuleName = moduleName;
        CircularPath = circularPath;
    }

    public ModuleLoadException(string message) : base(message)
    {
        ModuleName = string.Empty;
        CircularPath = Array.Empty<Type>();
    }
}
