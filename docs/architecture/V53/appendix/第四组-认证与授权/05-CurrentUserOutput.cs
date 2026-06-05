using JNPF.Common.Models.User;
using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Dto.Module;

namespace JNPF.OAuth.Dto;

/// <summary>
/// 当前客户信息输出.
/// </summary>
[SuppressSniffer]
public class CurrentUserOutput
{
    /// <summary>
    /// 用户信息.
    /// </summary>
    public UserInfoModel userInfo { get; set; }

    /// <summary>
    /// 菜单列表.
    /// </summary>
    public List<ModuleNodeOutput> menuList { get; set; }

    /// <summary>
    /// 权限列表.
    /// </summary>
    public List<PermissionModel> permissionList { get; set; }

    /// <summary>
    /// 系统配置信息.
    /// </summary>
    public SysConfigInfo sysConfigInfo { get; set; }
}

/// <summary>
/// 权限.
/// </summary>
[SuppressSniffer]
public class PermissionModel
{
    /// <summary>
    /// 模块ID.
    /// </summary>
    public string modelId { get; set; }

    /// <summary>
    /// 模块名称.
    /// </summary>
    public string moduleName { get; set; }

    /// <summary>
    /// 列.
    /// </summary>
    public List<FunctionalColumnAuthorizeModel> column { get; set; }

    /// <summary>
    /// 按钮.
    /// </summary>
    public List<FunctionalButtonAuthorizeModel> button { get; set; }

    /// <summary>
    /// 表单.
    /// </summary>
    public List<FunctionalFormAuthorizeModel> form { get; set; }

    /// <summary>
    /// 资源.
    /// </summary>
    public List<FunctionalResourceAuthorizeModel> resource { get; set; }
}

/// <summary>
/// 功能权限基类.
/// </summary>
[SuppressSniffer]
public class FunctionalAuthorizeBase
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }
}

/// <summary>
/// 功能权限列.
/// </summary>
[SuppressSniffer]
public class FunctionalColumnAuthorizeModel : FunctionalAuthorizeBase
{
}

/// <summary>
/// 功能权限按钮.
/// </summary>
[SuppressSniffer]
public class FunctionalButtonAuthorizeModel : FunctionalAuthorizeBase
{
}

/// <summary>
/// 功能权限表单.
/// </summary>
[SuppressSniffer]
public class FunctionalFormAuthorizeModel : FunctionalAuthorizeBase
{
}

/// <summary>
/// 授权模块资源.
/// </summary>
[SuppressSniffer]
public class FunctionalResourceAuthorizeModel : FunctionalAuthorizeBase
{
}

/// <summary>
/// 系统配置信息.
/// </summary>
[SuppressSniffer]
public class SysConfigInfo
{
    /// <summary>
    /// 窗口标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 系统名称.
    /// </summary>
    public string sysName { get; set; }

    /// <summary>
    /// 系统版本.
    /// </summary>
    public string sysVersion { get; set; }

    /// <summary>
    /// 登录图标.
    /// </summary>
    public string loginIcon { get; set; }

    /// <summary>
    /// 版权信息.
    /// </summary>
    public string copyright { get; set; }

    /// <summary>
    /// 公司名称.
    /// </summary>
    public string companyName { get; set; }

    /// <summary>
    /// 导航图标.
    /// </summary>
    public string navigationIcon { get; set; }

    /// <summary>
    /// logo图标.
    /// </summary>
    public string workLogoIcon { get; set; }

    /// <summary>
    /// logo图标.
    /// </summary>
    public string logoIcon { get; set; }

    /// <summary>
    /// App图标.
    /// </summary>
    public string appIcon { get; set; }

    /// <summary>
    /// 项目地址url.
    /// </summary>
    public string jnpfDomain { get; set; }

    /// <summary>
    /// 新用户默认密码.
    /// </summary>
    public string newUserDefaultPassword { get; set; }
}

/// <summary>
/// 获取登录用户信息返回值.
/// </summary>
public class UserAllMenu
{
    /// <summary>
    /// 获取节点id.
    /// </summary>
    /// <returns></returns>
    public string id { get; set; }

    /// <summary>
    /// 获取节点父id.
    /// </summary>
    /// <returns></returns>
    public string parentId { get; set; }

    /// <summary>
    /// 是否有子级.
    /// </summary>
    public bool hasChildren { get; set; }

    /// <summary>
    /// 名称.
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
    /// 菜单地址.
    /// </summary>
    public string urlAddress { get; set; }

    /// <summary>
    /// 链接目标.
    /// </summary>
    public string linkTarget { get; set; }

    /// <summary>
    /// 子集.
    /// </summary>
    public List<UserAllMenu> children { get; set; }

    /// <summary>
    /// 菜单分类【1-类别、2-页面】.
    /// </summary>
    public int type { get; set; }

    public string propertyJson { get; set; }
    public string sortCode { get; set; }
    public string systemId { get; set; }
}