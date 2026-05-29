using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// OSChina开源中国.
/// </summary>
public class OschinaAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://www.oschina.net/action/oauth2/authorize";
    }

    public string accessToken()
    {
        return "https://www.oschina.net/action/openapi/token";
    }

    public string userInfo()
    {
        return "https://www.oschina.net/action/openapi/user";
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
        return DefaultAuthSourceEnum.OSCHINA.ToString();
    }
}