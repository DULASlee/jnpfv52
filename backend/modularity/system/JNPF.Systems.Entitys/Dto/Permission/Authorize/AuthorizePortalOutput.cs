using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 权限数据输出.
/// </summary>
[SuppressSniffer]
public class AuthorizePortalOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 平台.
    /// </summary>
    public string platform { get; set; }

    /// <summary>
    /// 系统Id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 是否置灰 true是置灰.
    /// </summary>
    public bool disabled { get; set; }
}