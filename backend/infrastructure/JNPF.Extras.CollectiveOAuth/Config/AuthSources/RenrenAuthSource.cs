using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 人人网.
/// </summary>
public class RenrenAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://graph.renren.com/oauth/authorize";
    }

    public string accessToken()
    {
        return "https://graph.renren.com/oauth/token";
    }

    public string userInfo()
    {
        return "https://api.renren.com/v2/user/get";
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string refresh()
    {
        return "https://graph.renren.com/oauth/token";
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.RENREN.ToString();
    }
}