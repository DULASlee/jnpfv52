using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Province;

/// <summary>
/// 行政区划列表输入.
/// </summary>
[SuppressSniffer]
public class ProvinceListInput : KeywordInput
{
    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }
}