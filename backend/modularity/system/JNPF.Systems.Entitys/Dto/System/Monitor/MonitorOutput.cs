using JNPF.Common.Model.Machine;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Monitor;

/// <summary>
/// 系统监控输出.
/// </summary>
[SuppressSniffer]
public class MonitorOutput
{
    /// <summary>
    /// 系统信息.
    /// </summary>
    public SystemInfoModel system { get; set; }

    /// <summary>
    /// CPU信息.
    /// </summary>
    public CpuInfoModel cpu { get; set; }

    /// <summary>
    /// 内存信息.
    /// </summary>
    public MemoryInfoModel memory { get; set; }

    /// <summary>
    /// 硬盘信息.
    /// </summary>
    public DiskInfoModel disk { get; set; }

    /// <summary>
    /// 服务器当时时间戳.
    /// </summary>
    public DateTime? time { get; set; }
}