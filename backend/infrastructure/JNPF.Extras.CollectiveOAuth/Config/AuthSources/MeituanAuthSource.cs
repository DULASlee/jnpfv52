using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 美团.
/// </summary>
public class MeituanAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://openapi.waimai.meituan.com/oauth/authorize";
    }

    public string accessToken()
    {
        return "https://openapi.waimai.meituan.com/oauth/access_token";
    }

    public string userInfo()
    {
        return "https://openapi.waimai.meituan.com/oauth/userinfo";
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string refresh()
    {
        return "https://openapi.waimai.meituan.com/oauth/refresh_token";
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.MEITUAN.ToString();
    }
}