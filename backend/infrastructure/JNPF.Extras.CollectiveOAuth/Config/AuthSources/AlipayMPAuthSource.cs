using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 支付宝服务窗.
/// </summary>
public class AlipayMPAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://openauth.alipay.com/oauth2/publicAppAuthorize.htm";
    }

    public string accessToken()
    {
        return "https://openapi.alipay.com/gateway.do";
    }

    public string userInfo()
    {
        return "https://openapi.alipay.com/gateway.do";
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
        return DefaultAuthSourceEnum.ALIPAY_MP.ToString();
    }
}