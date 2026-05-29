using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenComponent;

/// <summary>
/// 大屏组件创建输入.
/// </summary>
[SuppressSniffer]
public class ScreenComponentCrInput
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public string content { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public string img { get; set; }
}
