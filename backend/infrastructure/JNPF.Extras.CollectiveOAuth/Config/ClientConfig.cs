namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// CollectiveOAuth配置类
/// <para>@author wei.fu (wei.fu@rthinkingsoft.cn).</para>
/// <para>@since 1.8.</para>
/// </summary>
public class ClientConfig
{
    /// <summary>
    /// 客户端id：对应各平台的appKey.
    /// </summary>
    public string clientId { get; set; }

    /// <summary>
    /// 客户端Secret：对应各平台的appSecret.
    /// </summary>
    public string clientSecret { get; set; }

    /// <summary>
    /// 登录成功后的回调地址.
    /// </summary>
    public string redirectUri { get; set; }

    /// <summary>
    /// 支付宝公钥：当选择支付宝登录时，该值可用
    /// 对应“RSA2(SHA256)密钥”中的“支付宝公钥”.
    /// </summary>
    public string alipayPublicKey { get; set; }

    /// <summary>
    /// 是否需要申请unionid，目前只针对qq登录.
    /// <para>注：qq授权登录时，获取unionid需要单独发送邮件申请权限。如果个人开发者账号中申请了该权限，可以将该值置为true，在获取openId时就会同步获取unionId.</para>
    /// <para>参考链接：http://wiki.connect.qq.com/unionid%E4%BB%8B%E7%BB%8D.</para>
    /// <para>1.7.1版本新增参数.</para>
    /// </summary>
    public string unionId { get; set; }

    /// <summary>
    /// <para>Stack Overflow Key.</para>
    /// <para>@since 1.9.0.</para>
    /// </summary>
    public string stackOverflowKey { get; set; }

    /// <summary>
    /// 企业微信，授权方的网页应用ID.
    /// <para>@since 1.10.0.</para>
    /// </summary>
    public string agentId { get; set; }

    /// <summary>
    /// 企业微信，授权方的网页应用ID.
    /// <para>@since 1.10.0.</para>
    /// </summary>
    public string scope { get; set; }
}