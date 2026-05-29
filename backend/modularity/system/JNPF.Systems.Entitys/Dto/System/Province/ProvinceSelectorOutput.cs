using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Province;

/// <summary>
/// 行政区划下拉框输出.
/// </summary>
[SuppressSniffer]
public class ProvinceSelectorOutput
{
    /// <summary>
    /// 是否为子节点.
    /// </summary>
    public bool isLeaf { get; set; }

    /// <summary>
    /// 区域名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}