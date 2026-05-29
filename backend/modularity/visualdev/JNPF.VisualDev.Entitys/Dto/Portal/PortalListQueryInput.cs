using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 在线开发门户列表查询输入.
/// </summary>
[SuppressSniffer]
public class PortalListQueryInput : PageInputBase
{
    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 类型(0-页面设计,1-自定义路径).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 锁定（0-锁定，1-自定义）.
    /// </summary>
    public int? enabledLock { get; set; }

    /// <summary>
    /// 状态(0-未发步，1-已发布，2-已修改).
    /// </summary>
    public int? isRelease { get; set; }
}