using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 小米开放平台.
/// </summary>
public class XiaoMiAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://account.xiaomi.com/oauth2/authorize";
    }

    public string accessToken()
    {
        return "https://account.xiaomi.com/oauth2/token";
    }

    public string userInfo()
    {
        return "https://open.account.xiaomi.com/user/profile";
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string refresh()
    {
        return "https://account.xiaomi.com/oauth2/token";
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.XIAOMI.ToString();
    }
}