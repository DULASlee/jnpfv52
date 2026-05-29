using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PermissionGroup;

/// <summary>
/// 修改权限组输入.
/// </summary>
[SuppressSniffer]
public class PermissionInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// permissionId.
    /// </summary>
    public string permissionId { get; set; }

    /// <summary>
    /// itemType.
    /// </summary>
    public string itemType { get; set; }

    /// <summary>
    /// objectType.
    /// </summary>
    public string objectType { get; set; }

}
