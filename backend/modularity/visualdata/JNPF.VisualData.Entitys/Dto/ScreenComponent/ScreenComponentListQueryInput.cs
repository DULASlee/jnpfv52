using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenComponent;

/// <summary>
/// 大屏组件列表查询输入.
/// </summary>
[SuppressSniffer]
public class ScreenComponentListQueryInput
{
    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 当前页码:pageIndex.
    /// </summary>
    public virtual int current { get; set; } = 1;

    /// <summary>
    /// 每页行数.
    /// </summary>
    public virtual int size { get; set; } = 50;
}