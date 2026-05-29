using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Province;

/// <summary>
/// 行政区划修改输入.
/// </summary>
[SuppressSniffer]
public class ProvinceUpInput : ProvinceCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}