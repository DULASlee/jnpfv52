using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.PortalManage;

/// <summary>
/// 门户管理列表输入.
/// </summary>
[SuppressSniffer]
public class PortalManageListQueryInput : PageInputBase
{
    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// WEB:网页端 App:手机端.
    /// </summary>
    public string? platform { get; set; }

    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }
}
