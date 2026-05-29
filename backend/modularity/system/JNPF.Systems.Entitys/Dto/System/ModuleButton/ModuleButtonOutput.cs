using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Dto.Module;

namespace JNPF.Systems.Entitys.Dto.ModuleButton;

/// <summary>
/// 功能按钮输出.
/// </summary>
[SuppressSniffer]
public class ModuleButtonOutput : ModuleAuthorizeBase
{
    /// <summary>
    /// 按钮编码.
    /// </summary>
    public string enCode { get; set; }
}