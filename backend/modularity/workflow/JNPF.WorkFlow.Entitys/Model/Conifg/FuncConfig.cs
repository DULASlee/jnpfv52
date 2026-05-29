using JNPF.Common.Dtos.Datainterface;
using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.Conifg;

[SuppressSniffer]
public class FuncConfig
{
    /// <summary>
    /// 是否开启.
    /// </summary>
    public bool on { get; set; }

    /// <summary>
    /// 消息id.
    /// </summary>
    public string? interfaceId { get; set; }

    /// <summary>
    /// 消息名称.
    /// </summary>
    public string? interfaceName { get; set; }

    /// <summary>
    /// 模板配置json.
    /// </summary>
    public List<DataInterfaceParameter>? templateJson { get; set; }
}
