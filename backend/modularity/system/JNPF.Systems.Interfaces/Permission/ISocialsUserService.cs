using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Request;
using JNPF.Systems.Entitys.Dto.Socials;
using JNPF.Systems.Entitys.Model.Permission.SocialsUser;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems.Interfaces.Permission;

/// <summary>
/// 业务契约：第三方登录.
/// </summary>
public interface ISocialsUserService
{
    AuthCallbackNew SetAuthCallback(string code, string state);

    IAuthRequest GetAuthRequest(string authSource, string userId, bool isLogin, string ticket, string tenantId);

    List<SocialsUserListOutput> GetLoginList(string ticket);

    Task<string> Binding([FromQuery] SocialsUserInputModel model);

    Task<dynamic> GetSocialsUserInfo([FromQuery] SocialsUserInputModel model);

    Task<SocialsUserInfo> GetUserInfo(string source, string uuid, string socialName);

    string GetSocialUuid(AuthResponse res);
}