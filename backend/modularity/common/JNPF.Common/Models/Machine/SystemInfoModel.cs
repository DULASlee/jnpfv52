using JNPF.DependencyInjection;

namespace JNPF.Common.Model.Machine;

/// <summary>
/// 系统信息模型.
/// </summary>
[SuppressSniffer]
public class SystemInfoModel
{
    /// <summary>
    /// 系统.
    /// </summary>
    public string os { get; set; }

    /// <summary>
    /// 运行时间.
    /// </summary>
    public string day { get; set; }

    /// <summary>
    /// 服务器IP.
    /// </summary>
    public string ip { get; set; }
}