using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenGlobal;

/// <summary>
/// 全局变量列表输入.
/// </summary>
[SuppressSniffer]
public class ScreenGlobalListQueryInput
{
    /// <summary>
    /// 当前页码:pageIndex.
    /// </summary>
    public virtual int current { get; set; } = 1;

    /// <summary>
    /// 每页行数.
    /// </summary>
    public virtual int size { get; set; } = 50;
}
