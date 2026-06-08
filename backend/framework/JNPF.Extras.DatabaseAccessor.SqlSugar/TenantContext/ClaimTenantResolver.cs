using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 从 JWT Claims 解析租户 ID.
/// </summary>
public sealed class ClaimTenantResolver : ITenantResolver
{
    public string? ResolveTenantId(HttpContext httpContext)
    {
        return httpContext.User?.FindFirst("TenantId")?.Value;
    }
}
