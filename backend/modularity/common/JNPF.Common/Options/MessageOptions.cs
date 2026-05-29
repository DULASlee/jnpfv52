using JNPF.ConfigurableOptions;

namespace JNPF.Common.Options;

/// <summary>
/// JNPF基本配置.
/// </summary>
public sealed class MessageOptions : IConfigurableOptions
{
    /// <summary>
    /// 前端pc外链.
    /// </summary>
    public string DoMainPc { get; set; }

    /// <summary>
    /// 前端App外链.
    /// </summary>
    public string DoMainApp { get; set; }

    /// <summary>
    /// 前端App个推外链.
    /// </summary>
    public string AppPushUrl { get; set; }
}