using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SysCache;

/// <summary>
/// 缓存信息输出.
/// </summary>
[SuppressSniffer]
public class CacheInfoOutput
{
    /// <summary>
    /// 缓存名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 缓存内容.
    /// </summary>
    public string value { get; set; }
}