using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.Portal;

/// <summary>
/// 门户同步输入.
/// </summary>
[SuppressSniffer]
public class PortalSyncInput
{
    /// <summary>
    /// 同步至pc门户（0：不同步，1：同步）.
    /// </summary>
    public int? pcPortal { get; set; }

    /// <summary>
    /// 同步至app门户（0：不同步，1：同步）.
    /// </summary>
    public int? appPortal { get; set; }

    /// <summary>
    /// pc系统id.
    /// </summary>
    public List<string> pcPortalSystemId { get; set; }

    /// <summary>
    /// app系统id.
    /// </summary>
    public List<string> appPortalSystemId { get; set; }

    /// <summary>
    /// 同步至pc端菜单（0：不同步，1：同步）.
    /// </summary>
    public int? pc { get; set; }

    /// <summary>
    /// 同步至app端菜单（0：不同步，1：同步）.
    /// </summary>
    public int? app { get; set; }

    /// <summary>
    /// 同步菜单pc上级id.
    /// </summary>
    public List<string> pcModuleParentId { get; set; }

    /// <summary>
    /// 同步菜单app上级id.
    /// </summary>
    public List<string> appModuleParentId { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    public string platformRelease { get; set; }
}
