using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 匿名端点降级解析（ADR-004）.
/// 四级降级：JWT Claims → Header → QueryString → 默认值.
/// </summary>
public sealed class FallbackTenantResolver : ITenantResolver
{
    public string? ResolveTenantId(HttpContext httpContext)
    {
        // Level 1: JWT Claims
        var claimValue = httpContext.User?.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrEmpty(claimValue))
            return claimValue;

        // Level 2: Header X-Tenant-Id
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue)
            && !string.IsNullOrEmpty(headerValue))
            return headerValue.ToString();

        // Level 3: QueryString tenantId
        if (httpContext.Request.Query.TryGetValue("tenantId", out var queryValue)
            && !string.IsNullOrEmpty(queryValue))
            return queryValue.ToString();

        // Level 4: Default
        return "default";
    }
}
