using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleForm;

/// <summary>
/// 功能列修改输入.
/// </summary>
[SuppressSniffer]
public class ModuleFormUpInput : ModuleFormCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}