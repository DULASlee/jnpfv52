using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 列表字段权限输入.
/// </summary>
[SuppressSniffer]
public class ColumnsPurviewDataUpInput
{
    /// <summary>
    /// 模块ID.
    /// </summary>
    public string moduleId { get; set; }

    /// <summary>
    /// 列表字段数组.
    /// </summary>
    public string fieldList { get; set; }

}

public class FieldList
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