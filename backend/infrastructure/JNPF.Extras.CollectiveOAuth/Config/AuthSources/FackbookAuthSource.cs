using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// Facebook.
/// </summary>
public class FackbookAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://www.facebook.com/v3.3/dialog/oauth";
    }

    public string accessToken()
    {
        return "https://graph.facebook.com/v3.3/oauth/access_token";
    }

    public string userInfo()
    {
        return "https://graph.facebook.com/v3.3/me";
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string refresh()
    {
        throw new System.NotImplementedException();
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.FACEBOOK.ToString();
    }
}