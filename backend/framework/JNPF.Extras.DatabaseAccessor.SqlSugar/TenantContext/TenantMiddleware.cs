using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 租户中间件 — HTTP 请求入口 (Phase 6 增强版).
/// 在请求开始时设置租户上下文，请求结束时清理（防 AsyncLocal 污染）.
/// Phase 6 新增：非公开端点缺少 TenantId → 403 拒绝.
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] PublicPathPrefixes = new[]
    {
        "/api/auth/",
        "/api/public/",
        "/api/oauth/",
        "/api/founder/",
        "/api/Sandbox",
        "/api/InteAssistant/",
        "/api/system/",
        "/api/permission/",
        "/api/studio/",
        "/swagger",
        "/.well-known",
        "/health",
    };

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        tenantContext.SetFromHttpContext(context);

        // Phase 6: 非公开端点必须有 TenantId
        if (!IsPublicEndpoint(context.Request.Path))
        {
            var tenantId = tenantContext.TenantId;

            // 直接从 Header 二次确认（因为 SetFromHttpContext 可能降级到 "default"）
            var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            var effectiveTenantId = !string.IsNullOrEmpty(headerTenantId) ? headerTenantId : tenantId;

            if (string.IsNullOrEmpty(effectiveTenantId) || effectiveTenantId == "default")
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    code = 403,
                    message = "Missing TenantId. Please provide X-Tenant-Id header or valid tenant credential."
                }));
                return;
            }
        }

        try
        {
            await _next(context);
        }
        finally
        {
            // 铁律：必须清理 AsyncLocal，防止线程池复用导致的幽灵租户
            tenantContext.ClearScope();
        }
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var pathValue = path.Value ?? "";
        foreach (var prefix in PublicPathPrefixes)
        {
            if (pathValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
