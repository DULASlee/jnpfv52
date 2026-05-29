using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenGlobal;

/// <summary>
/// 全局变量信息输出.
/// </summary>
[SuppressSniffer]
public class ScreenGlobalInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 全局变量名称.
    /// </summary>
    public string globalName { get; set; }

    /// <summary>
    /// 全局变量Key.
    /// </summary>
    public string globalKey { get; set; }

    /// <summary>
    /// 全局变量值.
    /// </summary>
    public string globalValue { get; set; }
}
