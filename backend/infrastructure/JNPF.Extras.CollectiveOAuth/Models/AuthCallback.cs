namespace JNPF.Extras.CollectiveOAuth.Models;

/// <summary>
/// 登录回调验证.
/// </summary>
public class AuthCallback
{
    /// <summary>
    /// 访问AuthorizeUrl后回调时带的参数code.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 访问AuthorizeUrl后回调时带的参数auth_code，该参数目前只使用于支付宝登录.
    /// </summary>
    public string auth_code { get; set; }

    /// <summary>
    /// 访问AuthorizeUrl后回调时带的参数state，用于和请求AuthorizeUrl前的state比较，防止CSRF攻击.
    /// </summary>
    public string state { get; set; }

    /// <summary>
    /// 华为授权登录接受code的参数名.
    /// <para>@since 1.10.0.</para>
    /// </summary>
    public string authorization_code { get; set; }

    /// <summary>
    /// Twitter回调后返回的oauth_token.
    /// <para>@since 1.13.0.</para>
    /// </summary>
    public string oauthToken { get; set; }

    /// <summary>
    /// Twitter回调后返回的oauth_verifier.
    /// <para>@since 1.13.0.</para>
    /// </summary>
    public string oauthVerifier { get; set; }
}