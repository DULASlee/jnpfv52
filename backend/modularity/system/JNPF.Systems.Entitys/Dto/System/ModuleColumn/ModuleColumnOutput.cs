using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Dto.Module;

namespace JNPF.Systems.Entitys.Dto.ModuleColumn;

/// <summary>
/// 功能列输出.
/// </summary>
[SuppressSniffer]
public class ModuleColumnOutput : ModuleAuthorizeBase
{
    /// <summary>
    /// 按钮编码.
    /// </summary>
    public string enCode { get; set; }
}