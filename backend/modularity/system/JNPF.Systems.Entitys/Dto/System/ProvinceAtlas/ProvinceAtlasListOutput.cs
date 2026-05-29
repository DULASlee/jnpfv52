using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ProvinceAtlas;

/// <summary>
/// 行政区划列表输出.
/// </summary>
[SuppressSniffer]
public class ProvinceAtlasListOutput : TreeModel
{
    /// <summary>
    /// 区域编号.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int enabledMark { get; set; }

    /// <summary>
    /// 区域名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}