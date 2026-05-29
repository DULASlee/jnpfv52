using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PermissionGroup;

/// <summary>
/// 修改权限组输入.
/// </summary>
[SuppressSniffer]
public class PermissionGroupUpInput : PermissionGroupCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}

/// <summary>
/// 更新权限成员输入.
/// </summary>
public class PermissionMemberUpInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// ids.
    /// </summary>
    public List<string> ids { get; set; }
}