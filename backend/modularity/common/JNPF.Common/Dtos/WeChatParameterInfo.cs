using JNPF.DependencyInjection;

namespace JNPF.Common.Dtos;

/// <summary>
/// 企业号连接参数信息.
/// </summary>
[SuppressSniffer]
public class WeChatParameterInfo
{
    /// <summary>
    /// 应用凭证.
    /// </summary>
    public string qyhAgentId { get; set; }

    /// <summary>
    /// 凭证密钥.
    /// </summary>
    public string qyhAgentSecret { get; set; }

    /// <summary>
    /// 企业号Id.
    /// </summary>
    public string qyhCorpId { get; set; }

    /// <summary>
    /// 同步密钥.
    /// </summary>
    public string qyhCorpSecret { get; set; }
}