using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleButton;

/// <summary>
/// 功能按钮修改输入.
/// </summary>
[SuppressSniffer]
public class ModuleButtonUpInput : ModuleButtonCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}