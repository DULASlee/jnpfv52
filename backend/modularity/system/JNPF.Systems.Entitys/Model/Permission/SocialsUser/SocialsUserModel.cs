using JNPF.Common.Models.User;
using JNPF.DependencyInjection;
using JNPF.Extras.CollectiveOAuth.Models;
using System.Text.Json.Nodes;

namespace JNPF.Systems.Entitys.Model.Permission.SocialsUser;

/// <summary>
/// .
/// </summary>
[SuppressSniffer]
public class SocialsUserInputModel
{
    public string source { get; set; }

    public string code { get; set; }

    public string state { get; set; }

    public string userId { get; set; }

    public string tenantId { get; set; }

    public string socialType { get; set; }

    public string socialUnionid { get; set; }

    public string socialName { get; set; }

    public string uuid { get; set; }

    public string authCode { get; set; }

    public string auth_code { get; set; }

    public bool tenantLogin { get; set; }

    public string jnpf_ticket { get; set; }
}

public class SocialsUserInfo
{
    public UserInfoModel userInfo { get; set; }
    public JsonArray tenantUserInfo { get; set; }
    public string socialUnionid { get; set; }
    public string socialName { get; set; }
}

public class AuthCallbackNew : AuthCallback
{
    public string authCode;
}


/// <summary>
/// .
/// </summary>
[SuppressSniffer]
public class SocialsUserCallBackModel
{
    public string userId { get; set; }

    public string account { get; set; }

    public string accountName { get; set; }

    public string socialType { get; set; }

    public string socialId { get; set; }

    public string socialName { get; set; }

    public string tenantId { get; set; }

    public string tenantName { get; set; }

    public bool tenantLogin { get; set; }

}
