using Microsoft.AspNetCore.Http;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 租户解析策略接口.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// 从 HTTP 上下文解析租户 ID.
    /// </summary>
    string? ResolveTenantId(HttpContext httpContext);
}
