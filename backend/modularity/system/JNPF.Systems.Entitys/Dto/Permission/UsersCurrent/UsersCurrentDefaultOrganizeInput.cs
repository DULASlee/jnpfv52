using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.UsersCurrent;

/// <summary>
/// 用户切换3个默认 输入.
/// </summary>
[SuppressSniffer]
public class UsersCurrentDefaultOrganizeInput
{
    /// <summary>
    /// 默认切换类型：Organize：组织，Position：岗位：Role：角色，System：系统.
    /// </summary>
    public string majorType { get; set; }

    /// <summary>
    /// 默认切换Id（组织Id、岗位Id、角色Id、系统Id）.
    /// </summary>
    public string majorId { get; set; }

    /// <summary>
    /// 菜单类型 (1 代表 APP).
    /// </summary>
    public int menuType { get; set; }
}