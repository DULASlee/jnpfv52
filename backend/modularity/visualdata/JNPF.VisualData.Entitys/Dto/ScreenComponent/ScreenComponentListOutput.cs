using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenComponent;

/// <summary>
/// 大屏组件列表输出.
/// </summary>
[SuppressSniffer]
public class ScreenComponentListOutput
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
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    public string img { get; set; }
}
