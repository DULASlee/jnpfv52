using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 功能.
/// </summary>
[SuppressSniffer]
public class ModuleOutput
{
    /// <summary>
    /// 菜单ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 菜单名称.
    /// </summary>
    public string fullName { get; set; }
}