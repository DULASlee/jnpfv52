using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.InterfaceOauth;

[SuppressSniffer]
public class InterfaceOauthListInput : PageInputBase
{
    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }
}
