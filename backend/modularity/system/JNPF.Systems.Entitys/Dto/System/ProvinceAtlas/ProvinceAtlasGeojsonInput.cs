using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ProvinceAtlas;

/// <summary>
/// 行政区划地图输入.
/// </summary>
[SuppressSniffer]
public class ProvinceAtlasGeojsonInput
{
    /// <summary>
    /// 区域编码.
    /// </summary>
    public string code { get; set; }
}
