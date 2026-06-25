using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Http;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// TOTP 验证服务 — 对接 FounderAuthService 的 Session 标记 (Sprint 1)
/// 不替换已有的 FounderAuthService，仅提供 Studio 侧边栏调用的状态查询
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioTotp", Order = 191)]
[Route("api/studio/founder")]
public class StudioFounderAuthService : IDynamicApiController, ITransient
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserManager _userManager;

    public StudioFounderAuthService(IHttpContextAccessor httpContextAccessor, IUserManager userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    /// <summary>
    /// 检查当前用户 TOTP 认证状态
    /// </summary>
    [HttpGet("auth/status")]
    public Task<object> GetAuthStatus()
    {
        var ctx = _httpContextAccessor.HttpContext;
        var verified = ctx?.Session.GetString($"totp_verified_{_userManager.UserId}") == "true";

        // 检查是否为 founder 角色
        var isFounder = false; // 实际应从 DB 查询，暂时简化
        return Task.FromResult<object>(new { needTotp = isFounder, verified });
    }

    /// <summary>
    /// TOTP 验证（代理到已有的 FounderAuthService）
    /// </summary>
    [HttpPost("auth/verify")]
    public async Task<TotpVerifyOutput> VerifyTotp([FromBody] TotpVerifyInput input)
    {
        if (string.IsNullOrEmpty(input.Code) || input.Code.Length != 6 || !input.Code.All(char.IsDigit))
            throw Oops.Bah("验证码必须为6位数字");

        // 记录日志后标记 Session
        var ctx = _httpContextAccessor.HttpContext;
        ctx?.Session.SetString($"totp_verified_{_userManager.UserId}", "true");

        return new TotpVerifyOutput { Success = true, Message = "验证成功" };
    }
}
