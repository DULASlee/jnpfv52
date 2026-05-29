using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenRecord;

/// <summary>
/// 大屏数据源修改输入.
/// </summary>
[SuppressSniffer]
public class ScreenRecordUpInput : ScreenRecordCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}
