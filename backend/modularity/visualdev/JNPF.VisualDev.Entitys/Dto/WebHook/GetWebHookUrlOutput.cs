using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.WebHook;

/// <summary>
/// 获取WebHookUrl输出文件.
/// </summary>
[SuppressSniffer]
public class GetWebHookUrlOutput
{
    /// <summary>
    /// EnCode字符串.
    /// </summary>
    public string enCodeStr { get; set; }

    /// <summary>
    /// 随机值字符串.
    /// </summary>
    public string randomStr { get; set; }

    /// <summary>
    /// 请求URL.
    /// </summary>
    public string requestUrl { get; set; }

    /// <summary>
    /// WebHookURL.
    /// </summary>
    public string webhookUrl { get; set; }
}