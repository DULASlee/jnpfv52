using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 小程序.
/// </summary>
public class WechatAppletsAuthSource : IAuthSource
{
    public string accessToken()
    {
        return null;
    }

    public string authorize()
    {
        return null;
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.WECHAT_APPLETS.ToString();
    }

    public string refresh()
    {
        return null;
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string userInfo()
    {
        return "https://api.weixin.qq.com/sns/jscode2session";
    }
}