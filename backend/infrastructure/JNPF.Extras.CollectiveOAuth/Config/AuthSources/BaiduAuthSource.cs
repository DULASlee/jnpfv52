using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 百度开放平台.
/// </summary>
public class BaiduAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://openapi.baidu.com/oauth/2.0/authorize";
    }

    public string accessToken()
    {
        return "https://openapi.baidu.com/oauth/2.0/token";
    }

    public string userInfo()
    {
        return "https://openapi.baidu.com/rest/2.0/passport/users/getInfo";
    }

    public string revoke()
    {
        return "https://openapi.baidu.com/rest/2.0/passport/auth/revokeAuthorization";
    }

    public string refresh()
    {
        return "https://openapi.baidu.com/oauth/2.0/token";
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.BAIDU.ToString();
    }
}