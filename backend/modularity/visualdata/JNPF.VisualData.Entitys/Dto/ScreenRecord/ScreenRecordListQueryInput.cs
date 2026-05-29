using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenRecord;

/// <summary>
/// 大屏数据源列表查询输入.
/// </summary>
[SuppressSniffer]
public class ScreenRecordListQueryInput
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