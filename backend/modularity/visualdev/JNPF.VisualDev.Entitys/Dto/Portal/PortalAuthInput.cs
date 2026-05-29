using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 门户信息输入.
/// </summary>
[SuppressSniffer]
public class PortalAuthInput
{
    /// <summary>
    /// PC:网页端 APP:手机端 MOD:模板.
    /// </summary>
    public string platform { get; set; }

    /// <summary>
    /// 系统ID.
    /// </summary>
    public string systemId { get; set; }
}
