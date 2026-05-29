using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PermissionGroup;

/// <summary>
/// 权限组列表输出.
/// </summary>
[SuppressSniffer]
public class PermissionGroupListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 分组名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 分组编号.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 成员(组织、角色、岗位、分组、用户 Ids).
    /// </summary>
    public string permissionMember { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }
}