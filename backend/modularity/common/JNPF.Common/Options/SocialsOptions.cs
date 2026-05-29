using JNPF.ConfigurableOptions;

namespace JNPF.Common.Options;

/// <summary>
/// 第三方登录配置.
/// </summary>
public sealed class SocialsOptions : IConfigurableOptions
{
    /// <summary>
    /// 是否启用.
    /// </summary>
    public bool SocialsEnabled { get; set; }

    /// <summary>
    /// 外网能访问的地址(域名), 回调的时候拼接接口地址用.
    /// </summary>
    public string DoMain { get; set; }

    /// <summary>
    /// 第三方账号密钥配置.
    /// </summary>
    public List<SocialsConfig> Config { get; set; }
}

/// <summary>
/// 第三方账号密钥配置.
/// </summary>
public class SocialsConfig
{
    /// <summary>
    /// 第三方.
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// Id.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// 密钥.
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// 代理Id.
    /// </summary>
    public string AgentId { get; set; }

}