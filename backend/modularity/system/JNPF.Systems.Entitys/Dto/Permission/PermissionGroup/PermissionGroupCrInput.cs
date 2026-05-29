using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PermissionGroup;

/// <summary>
/// 创建权限组输入.
/// </summary>
[SuppressSniffer]
public class PermissionGroupCrInput
{
    /// <summary>
    /// 分组编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 分组名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 成员(组织、角色、岗位、分组、用户 Ids).
    /// </summary>
    public string permissionMember { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}