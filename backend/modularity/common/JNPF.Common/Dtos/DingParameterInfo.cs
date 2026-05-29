using JNPF.DependencyInjection;

namespace JNPF.Common.Dtos;

/// <summary>
/// 钉钉连接参数信息.
/// </summary>
[SuppressSniffer]
public class DingParameterInfo
{
    /// <summary>
    /// 企业号.
    /// </summary>
    public string dingAgentId { get; set; }

    /// <summary>
    /// 应用凭证.
    /// </summary>
    public string dingSynAppKey { get; set; }

    /// <summary>
    /// 凭证密钥.
    /// </summary>
    public string dingSynAppSecret { get; set; }
}