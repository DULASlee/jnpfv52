using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.UsersCurrent;

namespace JNPF.Systems.Entitys.Dto.UsersCurrent;

/// <summary>
/// 当前用户权限输出.
/// </summary>
[SuppressSniffer]
public class UsersCurrentAuthorizeOutput
{
    /// <summary>
    /// 模块.
    /// </summary>
    public List<UsersCurrentAuthorizeMoldel> module { get; set; }

    /// <summary>
    /// 列.
    /// </summary>
    public List<UsersCurrentAuthorizeMoldel> column { get; set; }

    /// <summary>
    /// 按钮.
    /// </summary>
    public List<UsersCurrentAuthorizeMoldel> button { get; set; }

    /// <summary>
    /// 资源.
    /// </summary>
    public List<UsersCurrentAuthorizeMoldel> resource { get; set; }

    /// <summary>
    /// 表单.
    /// </summary>
    public List<UsersCurrentAuthorizeMoldel> form { get; set; }

    /// <summary>
    /// 门户管理.
    /// </summary>
    public List<UsersCurrentPortalManageMoldel> portal { get; set; }
}