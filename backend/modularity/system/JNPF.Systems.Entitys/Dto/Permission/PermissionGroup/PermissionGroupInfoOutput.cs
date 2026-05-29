using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PermissionGroup;

/// <summary>
/// 选择权限组输出.
/// </summary>
[SuppressSniffer]
public class PermissionGroupInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 分组编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 分组名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }
}

/// <summary>
/// 权限信息输出.
/// </summary>
public class PermissionGroupTreeOutPut : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }
}

/// <summary>
/// 权限门户信息输出.
/// </summary>
public class PermissionGroupPortalTreeOutPut : PermissionGroupTreeOutPut
{
    /// <summary>
    /// 平台.
    /// </summary>
    public string platform { get; set; }
}