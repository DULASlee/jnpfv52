namespace JNPF.Extras.CollectiveOAuth.Models;

/// <summary>
/// 授权所需的token.
/// <para>@author wei.fu.</para>
/// <para>@since 1.8.</para>
/// </summary>
public class AuthToken
{
    /// <summary>
    /// 访问令牌.
    /// </summary>
    public string accessToken { get; set; }

    /// <summary>
    /// 过期.
    /// </summary>
    public int expireIn { get; set; }

    /// <summary>
    /// 刷新token.
    /// </summary>
    public string refreshToken { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string uid { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string openId { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string accessCode { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string unionId { get; set; }

    /// <summary>
    /// Google附带属性.
    /// </summary>
    public string scope { get; set; }

    public string tokenType { get; set; }

    public string idToken { get; set; }

    /// <summary>
    /// 小米附带属性.
    /// </summary>
    public string macAlgorithm { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string macKey { get; set; }

    /// <summary>
    /// 企业微信附带属性.
    /// <para>@since 1.10.0.</para>
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// Twitter附带属性.
    /// <para>@since 1.1..0.</para>
    /// </summary>
    public string oauthToken { get; set; }

    public string oauthTokenSecret { get; set; }

    public string userId { get; set; }

    public string screenName { get; set; }

    public bool oauthCallbackConfirmed { get; set; }
}