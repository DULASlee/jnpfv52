namespace JNPF.Logging.Attributes;

/// <summary>
/// 操作日记特性.
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class OperateLogAttribute : Attribute
{
    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="moduleName">模块名称.</param>
    /// <param name="action">动作.</param>
    public OperateLogAttribute(string moduleName, string action)
    {
        ModuleName = moduleName;
        Action = action;
    }

    /// <summary>
    /// 模块名称.
    /// </summary>
    public string ModuleName { get; private set; }

    /// <summary>
    /// 动作.
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// 是否在线开发.
    /// </summary>
    public bool IsVisualDev { get; set; }
}