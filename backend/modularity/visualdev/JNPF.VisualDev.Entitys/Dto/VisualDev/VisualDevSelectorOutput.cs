using JNPF.Common.Security;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 在线开发下拉框输出.
/// </summary>
public class VisualDevSelectorOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? SortCode { get; set; }
}
