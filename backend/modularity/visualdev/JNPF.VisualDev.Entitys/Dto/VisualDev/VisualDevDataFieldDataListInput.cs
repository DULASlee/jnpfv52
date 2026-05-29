using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 表单和弹窗 分页查询 输入和选中回写 输入.
/// </summary>
[SuppressSniffer]
public class VisualDevDataFieldDataListInput : PageInputBase
{
    /// <summary>
    /// 查询 字段名.
    /// </summary>
    public string? relationField { get; set; }

    /// <summary>
    /// 弹窗选中 值.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 弹窗选中 字段名.
    /// </summary>
    public string? propsValue { get; set; }

    /// <summary>
    /// 设定显示的所有列  以 , 号隔开.
    /// </summary>
    public string? columnOptions { get; set; }
}
