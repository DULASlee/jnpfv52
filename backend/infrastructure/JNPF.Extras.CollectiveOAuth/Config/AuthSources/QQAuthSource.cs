using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 腾讯QQ.
/// </summary>
public class QQAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://graph.qq.com/oauth2.0/authorize";
    }

    public string accessToken()
    {
        return "https://graph.qq.com/oauth2.0/token";
    }

    public string userInfo()
    {
        return "https://graph.qq.com/user/get_user_info";
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string refresh()
    {
        return "https://graph.qq.com/oauth2.0/token";
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.QQ.ToString();
    }
}