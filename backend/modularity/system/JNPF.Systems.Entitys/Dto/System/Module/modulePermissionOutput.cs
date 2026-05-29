using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 菜单权限组内容输出.
/// </summary>
[SuppressSniffer]
public class modulePermissionOutput
{
    /// <summary>
    /// 按钮权限.
    /// </summary>
    public ModulePermissionModel buttonAuthorize { get; set; }

    /// <summary>
    /// 列表权限.
    /// </summary>
    public ModulePermissionModel columnAuthorize { get; set; }

    /// <summary>
    /// 表单权限.
    /// </summary>
    public ModulePermissionModel formAuthorize { get; set; }

    /// <summary>
    /// 数据权限.
    /// </summary>
    public ModulePermissionModel dataAuthorize { get; set; }

    /// <summary>
    /// 权限成员.
    /// </summary>
    public ModulePermissionModel permissionMember { get; set; }
}

public class ModulePermissionModel
{
    /// <summary>
    /// 权限名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 是否有权限.
    /// 0-未开启、1-有权限、2-无权限.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 权限集合.
    /// </summary>
    public List<ModulePermissionBaseModel> list { get; set; }
}

public class ModulePermissionBaseModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }
}