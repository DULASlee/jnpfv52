using JNPF.Common.Manager;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.OAuth;

/// <summary>
/// Refresh Token 端点。
/// 登录返回双 Token（accessToken + refreshToken），
/// 本端点通过 refreshToken + 过期 accessToken 交换新的双 Token。
/// </summary>
[ApiDescriptionSettings(Tag = "OAuth", Name = "Refresh")]
public class RefreshTokenService : IDynamicApiController, ITransient
{
    private readonly ICacheManager _cacheManager;

    public RefreshTokenService(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    /// <summary>
    /// 通过 refreshToken 交换新的 accessToken + refreshToken。
    /// </summary>
    /// <param name="input">包含过期的 accessToken 和 refreshToken</param>
    [HttpPost("/api/oauth/refresh")]
    [AllowAnonymous]
    public dynamic TokenRefresh([FromBody] RefreshTokenInput input)
    {
        // 检查 refreshToken 是否已被使用（旋转机制）
        var blacklistKey = "BLACKLIST_REFRESH_TOKEN:" + input.RefreshToken;
        var blacklisted = _cacheManager.Exists(blacklistKey);
        if (blacklisted)
            throw Oops.Bah("refreshToken 已失效，请重新登录");

        // 交换新 Token
        var newAccessToken = JWTEncryption.Exchange(input.AccessToken, input.RefreshToken);
        if (string.IsNullOrEmpty(newAccessToken))
            throw Oops.Bah("refreshToken 无效或已过期，请重新登录");

        // 生成新 refreshToken
        var newRefreshToken = JWTEncryption.GenerateRefreshToken(newAccessToken, 30 * 24 * 60);

        return new
        {
            accessToken = string.Format("Bearer {0}", newAccessToken),
            refreshToken = newRefreshToken
        };
    }
}

/// <summary>
/// Refresh Token 请求输入。
/// </summary>
public class RefreshTokenInput
{
    /// <summary>过期的 accessToken</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>refreshToken</summary>
    public string RefreshToken { get; set; } = string.Empty;
}
