using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Province;

/// <summary>
/// 行政区划数据获取输入.
/// </summary>
[SuppressSniffer]
public class ProvinceGetDataInput
{
    /// <summary>
    /// 省市区 二维 数组.
    /// </summary>
    public List<List<string>> idsList { get; set; }
}