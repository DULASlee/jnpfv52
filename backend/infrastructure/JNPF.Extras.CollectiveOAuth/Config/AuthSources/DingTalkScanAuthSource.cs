using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 钉钉扫码.
/// </summary>
public class DingTalkScanAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://login.dingtalk.com/oauth2/auth";
        //return "https://oapi.dingtalk.com/connect/qrconnect";
    }

    public string accessToken()
    {
        return "https://api.dingtalk.com/v1.0/oauth2/userAccessToken";
        //throw new System.NotImplementedException(AuthResponseStatus.UNSUPPORTED.GetDesc());
    }

    public string userInfo()
    {
        return "https://api.dingtalk.com/v1.0/contact/users/me";
        //return "https://oapi.dingtalk.com/sns/getuserinfo_bycode"; ;
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
        return DefaultAuthSourceEnum.DINGTALK.ToString();
    }
}