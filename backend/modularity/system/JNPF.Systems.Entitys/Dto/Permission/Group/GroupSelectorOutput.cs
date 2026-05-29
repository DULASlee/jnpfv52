using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Group;

/// <summary>
/// 分组下拉框输出.
/// </summary>
[SuppressSniffer]
public class GroupSelectorOutput : TreeModel
{
    /// <summary>
    /// 分组名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public bool enabledMark { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }
}