using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 租户中间件 — HTTP 请求入口.
/// 在请求开始时设置租户上下文，请求结束时清理（防 AsyncLocal 污染）.
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        tenantContext.SetFromHttpContext(context);

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
}
