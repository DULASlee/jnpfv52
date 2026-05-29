using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Permission.OrganizeAdministrator;

/// <summary>
/// 分级管理 - 系统和主系统菜单 输出.
/// </summary>
[SuppressSniffer]
public class OrganizeAdminSystemModuleSelectorOutput
{
    /// <summary>
    /// 组织权限集合.
    /// </summary>
    public List<OrganizeAdministratorSelectorOutput> orgAdminList { get; set; }

    /// <summary>
    /// 应用权限集合.
    /// </summary>
    public List<SystemSelectorOutput> systemPermissionList { get; set; }

    /// <summary>
    /// 菜单权限集合.
    /// </summary>
    public List<ModuleSelectorOutput> modulePermissionList { get; set; }

    /// <summary>
    /// 有菜单权限集合.
    /// </summary>
    public List<string> moduleIds { get; set; }

    /// <summary>
    /// 有应用权限集合.
    /// </summary>
    public List<string> systemIds { get; set; }

    /// <summary>
    /// 管理组.
    /// </summary>
    public string managerGroup { get; set; }
}

/// <summary>
/// 应用权限列表输出.
/// </summary>
public class SystemSelectorOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 系统名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 系统编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 是否显示.
    /// </summary>
    public bool disabled { get; set; }
}

/// <summary>
/// 主系统菜单列表输出.
/// </summary>
public class ModuleSelectorOutput : TreeModel
{
    /// <summary>
    /// 菜单名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long sortCode { get; set; }

    /// <summary>
    /// 类目.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string propertyJson { get; set; }

    /// <summary>
    /// 系统Id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 是否菜单.
    /// </summary>
    public bool hasModule { get; set; }

    /// <summary>
    /// 是否显示.
    /// </summary>
    public bool disabled { get; set; }
}