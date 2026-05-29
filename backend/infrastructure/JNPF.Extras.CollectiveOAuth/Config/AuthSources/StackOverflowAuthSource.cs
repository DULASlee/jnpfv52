using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// Stack Overflow.
/// </summary>
public class StackOverflowAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://stackoverflow.com/oauth";
    }

    public string accessToken()
    {
        return "https://stackoverflow.com/oauth/access_token/json";
    }

    public string userInfo()
    {
        return "https://api.stackexchange.com/2.2/me";
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
        return DefaultAuthSourceEnum.STACK_OVERFLOW.ToString();
    }
}