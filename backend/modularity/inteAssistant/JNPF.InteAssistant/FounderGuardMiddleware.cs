using System.Net;
using System.Text.Json;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.InteAssistant.Entitys.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 创始人认证守卫中间件 (Phase 6 Enhanced).
/// 拦截所有 /api/founder/* 请求.
/// Phase 0 → 404（功能未开放）
/// Phase 3 → 403（需创始人认证，仅地桩）
/// Phase 4+ → 验证 X-Founder-Token（真实 TOTP 认证）
/// 每次拦截写入 BASE_FOUNDER_AUTH_LOG.
/// </summary>
public sealed class FounderGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    private const string FounderPathPrefix = "/api/founder";
    private const string ConfigKey = "App:FounderPhase";

    // TOTP setup/verify endpoints 即使是 founder 路径也需要允许无 token 访问
    private static readonly string[] AnonymousFounderPaths = new[]
    {
        "/api/founder/auth/setup-totp",
        "/api/founder/auth/verify-totp",
    };

    public FounderGuardMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISqlSugarRepository<FounderAuthLogEntity> logRepository,
        FounderAuthService founderAuthService)
    {
        if (!context.Request.Path.StartsWithSegments(FounderPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var phase = _configuration.GetValue<int>(ConfigKey, 0);
        var action = $"{context.Request.Method} {context.Request.Path}";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        switch (phase)
        {
            case 0:
                await WriteLog(logRepository, action, "not_found", ipAddress, userAgent);
                await WriteJsonResponse(context, HttpStatusCode.NotFound, new
                {
                    code = 404,
                    message = "Founder API not available in current phase"
                });
                break;

            case 3:
                await WriteLog(logRepository, action, "deny", ipAddress, userAgent);
                await WriteJsonResponse(context, HttpStatusCode.Forbidden, new
                {
                    code = 403,
                    message = "Founder authentication required"
                });
                break;

            default:
                // Phase 4+: 真实 TOTP 认证
                // 匿名端点（setup-totp, verify-totp）放行
                if (IsAnonymousFounderPath(context.Request.Path))
                {
                    await _next(context);
                    return;
                }

                // 验证 X-Founder-Token
                var token = context.Request.Headers["X-Founder-Token"].FirstOrDefault();
                if (string.IsNullOrEmpty(token))
                {
                    await WriteLog(logRepository, action, "missing_token", ipAddress, userAgent);
                    await WriteJsonResponse(context, HttpStatusCode.Unauthorized, new
                    {
                        code = 401,
                        message = "Missing X-Founder-Token header. Use POST /api/founder/auth/verify-totp to obtain a token."
                    });
                    return;
                }

                if (!founderAuthService.ValidateFounderToken(token))
                {
                    await WriteLog(logRepository, action, "invalid_token", ipAddress, userAgent);
                    await WriteJsonResponse(context, HttpStatusCode.Forbidden, new
                    {
                        code = 403,
                        message = "Invalid or expired founder token. Please re-authenticate with TOTP."
                    });
                    return;
                }

                // Token 有效，记录日志并放行
                var email = founderAuthService.ExtractEmailFromToken(token) ?? "unknown";
                await WriteLog(logRepository, action, $"allow:{email}", ipAddress, userAgent);
                await _next(context);
                break;
        }
    }

    private static bool IsAnonymousFounderPath(PathString path)
    {
        foreach (var prefix in AnonymousFounderPaths)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static async Task WriteJsonResponse(HttpContext context, HttpStatusCode statusCode, object body)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static async Task WriteLog(
        ISqlSugarRepository<FounderAuthLogEntity> logRepository,
        string action,
        string result,
        string ipAddress,
        string userAgent)
    {
        try
        {
            var log = new FounderAuthLogEntity
            {
                Action = action,
                Result = result,
                IpAddress = ipAddress,
                UserAgent = userAgent,
            };
            log.Create();

            await logRepository.AsInsertable(log).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        }
        catch
        {
            // 日志写入失败不阻塞请求
        }
    }
}
