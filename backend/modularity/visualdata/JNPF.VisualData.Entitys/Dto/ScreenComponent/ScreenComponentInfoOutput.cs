using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenComponent;

/// <summary>
/// 大屏数据详情输出.
/// </summary>
[SuppressSniffer]
public class ScreenComponentInfoOutput
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
    /// 内容.
    /// </summary>
    public string content { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    public string img { get; set; }
}
