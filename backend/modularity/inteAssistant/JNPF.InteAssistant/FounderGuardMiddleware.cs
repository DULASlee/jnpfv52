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
/// 创始人认证守卫中间件
/// 拦截所有 /api/founder/* 请求
/// Phase 0 → 404（功能未开放）
/// Phase 3 → 403（需创始人认证）
/// 每次拦截写入 BASE_FOUNDER_AUTH_LOG
/// </summary>
public sealed class FounderGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    private const string FounderPathPrefix = "/api/founder";
    private const string ConfigKey = "App:FounderPhase";

    public FounderGuardMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, ISqlSugarRepository<FounderAuthLogEntity> logRepository)
    {
        if (!context.Request.Path.StartsWithSegments(FounderPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var phase = _configuration.GetValue<int>(ConfigKey, 0); // 默认 Phase 0
        var action = $"{context.Request.Method} {context.Request.Path}";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        switch (phase)
        {
            case 0:
                await WriteLog(logRepository, action, "not_found", ipAddress, userAgent);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    message = "Founder API not available in current phase"
                }));
                break;

            case 3:
                await WriteLog(logRepository, action, "deny", ipAddress, userAgent);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    message = "Founder authentication required"
                }));
                break;

            default:
                // 未识别的 phase，放行
                await _next(context);
                break;
        }
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
