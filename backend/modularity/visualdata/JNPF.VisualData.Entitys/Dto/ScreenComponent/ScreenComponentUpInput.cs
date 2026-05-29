using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenComponent;

/// <summary>
/// 大屏组件修改输入.
/// </summary>
[SuppressSniffer]
public class ScreenComponentUpInput : ScreenComponentCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}
