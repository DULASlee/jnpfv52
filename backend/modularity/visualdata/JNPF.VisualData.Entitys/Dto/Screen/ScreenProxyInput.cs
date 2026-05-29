using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.Screen;

/// <summary>
/// 大屏代理输入.
/// </summary>
[SuppressSniffer]
public class ScreenProxyInput
{
    /// <summary>
    /// 路径.
    /// </summary>
    public string url { get; set; }

    /// <summary>
    /// 请求方式.
    /// </summary>
    public string method { get; set; }

    /// <summary>
    /// headers.
    /// </summary>
    public Dictionary<string, string> headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// data.
    /// </summary>
    public Dictionary<string, object>? data { get; set; } = null;

    /// <summary>
    /// parms.
    /// </summary>
    public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 每页条数.
    /// </summary>
    public int timeout { get; set; } = 3;
}
