using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 企业微信扫码.
/// </summary>
public class WechatEnterpriseScanAuthSource : IAuthSource
{
    public string accessToken()
    {
        return "https://qyapi.weixin.qq.com/cgi-bin/gettoken";
    }

    public string authorize()
    {
        return "https://open.work.weixin.qq.com/wwopen/sso/qrConnect";
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.WECHAT_ENTERPRISE_SCAN.ToString();
    }

    public string refresh()
    {
        throw new System.NotImplementedException();
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string userInfo()
    {
        return "https://qyapi.weixin.qq.com/cgi-bin/user/getuserinfo";
    }
}