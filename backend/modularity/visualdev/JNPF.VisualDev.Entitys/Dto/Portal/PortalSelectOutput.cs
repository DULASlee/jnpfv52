using JNPF.Common.Security;
using JNPF.DependencyInjection;
using System.Text.Json.Serialization;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 门户下拉框输出.
/// </summary>
[SuppressSniffer]
public class PortalSelectOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    [JsonIgnore]
    public string sortCode { get; set; }

    /// <summary>
    /// 有效标记.
    /// </summary>
    [JsonIgnore]
    public int enabledMark { get; set; }

    /// <summary>
    /// 删除标记.
    /// </summary>
    [JsonIgnore]
    public string deleteMark { get; set; }

    /// <summary>
    /// 类型(0-页面设计,1-自定义路径).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 静态页面路径.
    /// </summary>
    public string customUrl { get; set; }

    /// <summary>
    /// 链接类型(0-页面,1-外链).
    /// </summary>
    public int? linkType { get; set; }
}
