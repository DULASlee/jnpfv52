using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 微信开放平台.
/// </summary>
public class WechatOpenAuthSource : IAuthSource
{
    public string accessToken()
    {
        return "https://api.weixin.qq.com/sns/oauth2/access_token";
    }

    public string authorize()
    {
        return "https://open.weixin.qq.com/connect/qrconnect";
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.WECHAT_OPEN.ToString();
    }

    public string refresh()
    {
        return "https://api.weixin.qq.com/sns/oauth2/refresh_token";
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string userInfo()
    {
        return "https://api.weixin.qq.com/sns/userinfo";
    }
}