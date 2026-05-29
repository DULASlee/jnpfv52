using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 飞书.
/// </summary>
public class FeiShuAuthSource : IAuthSource
{
    public string authorize()
    {
        return "https://open.feishu.cn/open-apis/authen/v1/index";
    }

    public string accessToken()
    {
        return "https://open.feishu.cn/open-apis/authen/v1/access_token";
    }

    public string userInfo()
    {
        return "https://open.feishu.cn/open-apis/authen/v1/user_info";
    }

    public string revoke()
    {
        return "https://open.feishu.cn/open-apis/authen/v1/refresh_access_token";
    }

    public string refresh()
    {
        return "https://open.feishu.cn/open-apis/authen/v1/refresh_access_token";
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.FEISHU.ToString();
    }
}