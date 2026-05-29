using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 列表展示字段输出.
/// </summary>
[SuppressSniffer]
public class ListDisplayFieldOutput
{
    /// <summary>
    /// 显示名称.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string prop { get; set; }

    /// <summary>
    /// 是否显示.
    /// </summary>
    public bool visible { get; set; }
}