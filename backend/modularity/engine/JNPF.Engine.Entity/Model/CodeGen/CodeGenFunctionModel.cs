using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成功能模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFunctionModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// 是否接口.
    /// </summary>
    public bool IsInterface { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public int orderBy { get; set; }
}