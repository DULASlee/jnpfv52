using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Position;

/// <summary>
/// 岗位下拉框输出.
/// </summary>
[SuppressSniffer]
public class PositionSelectorOutput : TreeModel
{
    /// <summary>
    /// 岗位名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 岗位类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organizeIdTree { get; set; }

    /// <summary>
    /// 组织树.
    /// </summary>
    public string organize { get; set; }
}