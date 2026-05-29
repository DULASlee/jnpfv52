using JNPF.ConfigurableOptions;

namespace JNPF.Common.Options;

/// <summary>
/// 单点登录配置.
/// </summary>
public sealed class OauthOptions : IConfigurableOptions
{
    /// <summary>
    /// 是否启用.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 前端登录页面访问登录接口进行单点登录页面跳转, 需要与身份管理系统中的 JNPF-Auth2、JNPF-CAS中的认证地址一致.
    /// </summary>
    public string LoginPath { get; set; }

    /// <summary>
    /// 从单点登录中心直接访问JNPF时登录成功后跳转的前端页面.
    /// </summary>
    public string SucessFrontUrl { get; set; }

    /// <summary>
    /// 默认接口.
    /// </summary>
    public string DefaultSSO { get; set; }

    /// <summary>
    /// 缓存过期时间 / 分钟.
    /// </summary>
    public int TicketTimeout { get; set; }

    /// <summary>
    /// 是否前端输出消息.
    /// </summary>
    public bool TicketOutMessage { get; set; }

    /// <summary>
    /// 登录模式.
    /// </summary>
    public SSO SSO { get; set; }

    /// <summary>
    /// 用户推送.
    /// </summary>
    public Pull Pull { get; set; }
}

public class SSO
{
    public Auth2 Auth2 { get; set; }

    public Cas Cas { get; set; }
}

public class Auth2
{
    public bool Enabled { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string AuthorizeUrl { get; set; }

    public string AccessTokenUrl { get; set; }

    public string UserInfoUrl { get; set; }
}

public class Cas
{
    public bool Enabled { get; set; }

    public string ServerLoginUrl { get; set; }

    public string ServerValidateUrl { get; set; }
}

public class Pull
{
    public bool Enabled { get; set; }

    public string CreateRestAddress { get; set; }

    public string ReplaceRestAddress { get; set; }

    public string ChangePasswordRestAddress { get; set; }

    public string DeleteRestAddress { get; set; }

    public string CredentialType { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

}