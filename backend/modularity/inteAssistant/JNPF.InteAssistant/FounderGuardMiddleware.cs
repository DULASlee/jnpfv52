using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.InteAssistant.Entitys.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 创始人认证守卫中间件 (Phase 6 Enhanced).
/// 拦截所有 /api/founder/* 请求.
/// Phase 0 → 404（功能未开放）
/// Phase 3 → 403（需创始人认证，仅地桩）
/// Phase 4+ → 验证 X-Founder-Token（真实 TOTP 认证）+ 设备指纹校验
/// 每次拦截写入 BASE_FOUNDER_AUTH_LOG.
/// </summary>
public sealed class FounderGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    private const string FounderPathPrefix = "/api/founder";
    private const string ConfigKey = "App:FounderPhase";
    private const string FingerprintSalt = "jnpf-foundry-v5.2";

    // TOTP setup/verify endpoints 即使是 founder 路径也需要允许无 token 访问
    // 流水线执行 API 已迁移至 /api/studio/pipeline/execute（普通 JWT 鉴权，非 founder 专属）
    private static readonly string[] AnonymousFounderPaths = new[]
    {
        "/api/founder/auth/setup-totp",
        "/api/founder/auth/verify-totp",
        "/api/founder/ai/test",
        "/api/founder/ai/health",
    };

    public FounderGuardMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISqlSugarRepository<FounderAuthLogEntity> logRepository,
        FounderAuthService founderAuthService,
        ILogger<FounderGuardMiddleware> logger)
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
        var deviceFingerprint = ComputeDeviceFingerprint(ipAddress, userAgent);

        switch (phase)
        {
            case 0:
                await WriteLog(logRepository, action, "not_found", ipAddress, userAgent, deviceFingerprint);
                await WriteJsonResponse(context, HttpStatusCode.NotFound, new
                {
                    code = 404,
                    message = "Founder API not available in current phase"
                });
                break;

            case 3:
                await WriteLog(logRepository, action, "deny", ipAddress, userAgent, deviceFingerprint);
                await WriteJsonResponse(context, HttpStatusCode.Forbidden, new
                {
                    code = 403,
                    message = "Founder authentication required"
                });
                break;

            default:
                // Phase 4+: 真实 TOTP 认证 + 设备指纹校验
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
                    await WriteLog(logRepository, action, "missing_token", ipAddress, userAgent, deviceFingerprint);
                    await WriteJsonResponse(context, HttpStatusCode.Unauthorized, new
                    {
                        code = 401,
                        message = "Missing X-Founder-Token header. Use POST /api/founder/auth/verify-totp to obtain a token."
                    });
                    return;
                }

                if (!founderAuthService.ValidateFounderToken(token))
                {
                    await WriteLog(logRepository, action, "invalid_token", ipAddress, userAgent, deviceFingerprint);
                    await WriteJsonResponse(context, HttpStatusCode.Forbidden, new
                    {
                        code = 403,
                        message = "Invalid or expired founder token. Please re-authenticate with TOTP."
                    });
                    return;
                }

                // Token 有效 → 设备指纹校验
                var email = founderAuthService.ExtractEmailFromToken(token) ?? "unknown";
                var fingerprintResult = await VerifyDeviceFingerprint(logRepository, email, deviceFingerprint, logger);

                if (fingerprintResult == FingerprintStatus.Mismatch)
                {
                    logger.LogWarning(
                        "[FounderGuard] Device fingerprint mismatch for {Email}. IP={Ip}, NewFP={NewFP}",
                        email, ipAddress, deviceFingerprint);
                    // 指纹不匹配不阻断请求，但记录告警日志
                    await WriteLog(logRepository, action, $"allow:{email}:fp_mismatch", ipAddress, userAgent, deviceFingerprint);
                }
                else
                {
                    await WriteLog(logRepository, action, $"allow:{email}", ipAddress, userAgent, deviceFingerprint);
                }

                await _next(context);
                break;
        }
    }

    /// <summary>
    /// 计算设备指纹：SHA256(IP + UserAgent + Salt)。
    /// 同一设备/网络环境产生相同指纹，用于跨 Session 设备识别。
    /// </summary>
    private static string ComputeDeviceFingerprint(string ipAddress, string userAgent)
    {
        var raw = $"{ipAddress}|{userAgent}|{FingerprintSalt}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 校验设备指纹：比对当前指纹与该创始人最后一次成功认证的指纹。
    /// 首次认证（无历史记录）→ 视为匹配。
    /// </summary>
    private static async Task<FingerprintStatus> VerifyDeviceFingerprint(
        ISqlSugarRepository<FounderAuthLogEntity> logRepository,
        string email,
        string currentFingerprint,
        ILogger logger)
    {
        try
        {
            // 查询该创始人最后一次 allow 的认证记录
            var lastAllowLog = await logRepository.AsQueryable()
                .Where(x => x.Result!.StartsWith($"allow:{email}"))
                .OrderByDescending(x => x.CreatorTime)
                .FirstAsync();

            if (lastAllowLog == null || string.IsNullOrEmpty(lastAllowLog.DeviceFingerprint))
            {
                // 首次认证，无基线指纹
                return FingerprintStatus.FirstUse;
            }

            if (!string.Equals(lastAllowLog.DeviceFingerprint, currentFingerprint, StringComparison.Ordinal))
            {
                return FingerprintStatus.Mismatch;
            }

            return FingerprintStatus.Match;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[FounderGuard] Fingerprint verification failed, falling back to allow");
            // 指纹校验异常不阻断请求
            return FingerprintStatus.Error;
        }
    }

    private enum FingerprintStatus
    {
        Match,
        Mismatch,
        FirstUse,
        Error,
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
        string userAgent,
        string deviceFingerprint)
    {
        try
        {
            var log = new FounderAuthLogEntity
            {
                Action = action,
                Result = result,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
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
