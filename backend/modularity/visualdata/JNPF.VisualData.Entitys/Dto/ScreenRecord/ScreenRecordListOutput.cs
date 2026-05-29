using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenRecord;

/// <summary>
/// 大屏数据源列表输出.
/// </summary>
[SuppressSniffer]
public class ScreenRecordListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public int? dataType { get; set; }
}
