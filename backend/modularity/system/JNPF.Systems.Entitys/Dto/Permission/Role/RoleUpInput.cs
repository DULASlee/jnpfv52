using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Role;

/// <summary>
/// 更新角色输入.
/// </summary>
[SuppressSniffer]
public class RoleUpInput : RoleCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}