using JNPF.DependencyInjection;

namespace JNPF.Common.Models.InteAssistant;

/// <summary>
/// 集成助手数据模型.
/// </summary>
[SuppressSniffer]
public class InteAssiDataModel
{
    /// <summary>
    /// 数据主键.
    /// </summary>
    public string DataId { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public string Data { get; set; }
}