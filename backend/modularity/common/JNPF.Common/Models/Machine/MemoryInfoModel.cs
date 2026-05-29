using JNPF.DependencyInjection;

namespace JNPF.Common.Model.Machine;

/// <summary>
/// 内存信息模型.
/// </summary>
[SuppressSniffer]
public class MemoryInfoModel
{
    /// <summary>
    /// 总内存.
    /// </summary>
    public string total { get; set; }

    /// <summary>
    /// 空闲内存.
    /// </summary>
    public string available { get; set; }

    /// <summary>
    /// 已使用内存.
    /// </summary>
    public string used { get; set; }

    /// <summary>
    /// 已使用百分比.
    /// </summary>
    public string usageRate { get; set; }
}