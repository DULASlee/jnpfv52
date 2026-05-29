using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenGlobal;

/// <summary>
/// 全局变量更新输入.
/// </summary>
[SuppressSniffer]
public class ScreenGlobalUpInput : ScreenGlobalCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}
