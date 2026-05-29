using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 门户管理：选择门户输入.
/// </summary>
[SuppressSniffer]
public class PortalManageInput : PageInputBase
{
    /// <summary>
    /// 类型(0-页面设计,1-自定义路径).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// WEB:网页端 APP:手机端.
    /// </summary>
    public string platform { get; set; }
}
