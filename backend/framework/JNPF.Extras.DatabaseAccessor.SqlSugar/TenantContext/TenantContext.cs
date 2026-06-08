using JNPF.Common.Manager;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Security.Claims;
using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 租户上下文实现（ADR-003 最终版）.
/// 使用 AsyncLocal 实现跨 async/await 的租户信息传播.
/// 提供静态访问点供 DataExecuting 等启动时编译的委托使用.
/// </summary>
public sealed class TenantContextImpl : ITenantContext
{
    /// <summary>
    /// AsyncLocal 静态访问点（供 DataExecuting 委托使用）.
    /// </summary>
    private static readonly AsyncLocal<TenantInfo> _current = new();

    /// <summary>
    /// 静态访问当前租户信息（启动时编译的委托可使用）.
    /// </summary>
    public static TenantInfo? Current
    {
        get => _current.Value;
        internal set => _current.Value = value;
    }

    /// <summary>
    /// 静态方法：显式设置租户信息（供非 HTTP 入口如 EventBus/Schedule 使用）.
    /// </summary>
    public static void SetTenant(string tenantId, string systemId = null)
    {
        _current.Value = new TenantInfo
        {
            TenantId = tenantId ?? "",
            SystemId = systemId ?? "",
            IsMultiTenant = true
        };
    }

    /// <summary>
    /// 静态方法：清除当前租户上下文（防 AsyncLocal 污染）.
    /// </summary>
    public static void ClearCurrent()
    {
        _current.Value = null;
    }

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEnumerable<ITenantResolver> _resolvers;
    private readonly ICacheManager _cache;
    private readonly TenantOptions _tenantOptions;
    private readonly global::SqlSugar.ConnectionStringsOptions _connectionOptions;

    public TenantContextImpl(
        IHttpContextAccessor httpContextAccessor,
        IEnumerable<ITenantResolver> resolvers,
        ICacheManager cache,
        IOptions<TenantOptions> tenantOptions,
        IOptions<global::SqlSugar.ConnectionStringsOptions> connectionOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _resolvers = resolvers;
        _cache = cache;
        _tenantOptions = tenantOptions.Value;
        _connectionOptions = connectionOptions.Value;
    }

    // ─── ITenantContext properties (from AsyncLocal or fallback) ───

    public string TenantId => Current?.TenantId ?? ResolveFromHttpContext()?.TenantId ?? "";
    public string SystemId => Current?.SystemId ?? ResolveFromHttpContext()?.SystemId ?? "";
    public string UserId => Current?.UserId ?? GetUserId() ?? "";
    public TenantConnectionInfo? ConnectionInfo => Current?.ConnectionInfo;
    public int IsolationType => Current?.IsolationType ?? 0;
    public string IsolationFieldValue => Current?.IsolationFieldValue ?? TenantId;
    public bool IsMultiTenant => Current?.IsMultiTenant ?? (_tenantOptions?.MultiTenancy ?? false);
    public bool IsMultiSystem => Current?.IsMultiSystem ?? false;
    public bool ShouldSkipSystemFilter => Current?.ShouldSkipSystemFilter ?? false;

    // ─── ITenantContext methods ───

    public void SetFromHttpContext(HttpContext httpContext)
    {
        if (httpContext == null) return;

        var tenantId = ResolveTenantId(httpContext);
        var systemId = httpContext.User?.FindFirst("ZxSystemId")?.Value ?? "";
        var userId = GetUserId() ?? "";

        var connectionInfo = ResolveConnectionInfo(tenantId);

        var info = new TenantInfo
        {
            TenantId = tenantId ?? "",
            SystemId = systemId,
            UserId = userId,
            ConnectionInfo = connectionInfo,
            IsolationType = connectionInfo?.IsolationType ?? 0,
            IsolationFieldValue = connectionInfo?.IsolationField ?? tenantId ?? "",
            IsMultiTenant = _tenantOptions?.MultiTenancy ?? false,
            IsMultiSystem = !string.IsNullOrEmpty(systemId),
            // orgSystem 管理所有子系统，不进行系统过滤
            ShouldSkipSystemFilter = "orgSystem".Equals(systemId)
        };

        _current.Value = info;
    }

    public void SetExplicit(string tenantId, string systemId = null)
    {
        var connectionInfo = ResolveConnectionInfo(tenantId);

        var info = new TenantInfo
        {
            TenantId = tenantId ?? "",
            SystemId = systemId ?? "",
            UserId = "",
            ConnectionInfo = connectionInfo,
            IsolationType = connectionInfo?.IsolationType ?? 0,
            IsolationFieldValue = connectionInfo?.IsolationField ?? tenantId ?? "",
            IsMultiTenant = _tenantOptions?.MultiTenancy ?? false,
            IsMultiSystem = !string.IsNullOrEmpty(systemId)
        };

        _current.Value = info;
    }

    public void SetFromEvent(object eventSource)
    {
        if (eventSource == null) return;

        // 通过反射从事件源提取 TenantId
        var tenantId = ExtractTenantIdFromPayload(eventSource);
        SetExplicit(tenantId);
    }

    public IDisposable BeginScope(TenantInfo info)
    {
        var previous = _current.Value;
        _current.Value = info;
        return new DisposableAction(() => _current.Value = previous);
    }

    public void ClearScope()
    {
        _current.Value = null;
    }

    public bool IsDefaultTenant()
    {
        return string.IsNullOrEmpty(TenantId) || TenantId == "default";
    }

    // ─── Private helpers ───

    private string? ResolveTenantId(HttpContext httpContext)
    {
        foreach (var resolver in _resolvers)
        {
            var tenantId = resolver.ResolveTenantId(httpContext);
            if (!string.IsNullOrEmpty(tenantId))
                return tenantId;
        }
        return null;
    }

    private TenantInfo? ResolveFromHttpContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        var tenantId = ResolveTenantId(httpContext);
        if (string.IsNullOrEmpty(tenantId)) return null;

        var connectionInfo = ResolveConnectionInfo(tenantId);

        return new TenantInfo
        {
            TenantId = tenantId,
            SystemId = httpContext.User?.FindFirst("ZxSystemId")?.Value ?? "",
            UserId = GetUserId() ?? "",
            ConnectionInfo = connectionInfo,
            IsolationType = connectionInfo?.IsolationType ?? 0,
            IsolationFieldValue = connectionInfo?.IsolationField ?? tenantId,
            IsMultiTenant = _tenantOptions?.MultiTenancy ?? false
        };
    }

    private string? GetUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
    }

    private TenantConnectionInfo? ResolveConnectionInfo(string? tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
            return null;

        var cacheResolver = _resolvers.OfType<FallbackTenantResolver>().FirstOrDefault()
            ?? new FallbackTenantResolver();

        var cacheTenantResolver = new CacheTenantResolver(_cache);
        return cacheTenantResolver.ResolveConnection(tenantId);
    }

    private static string ExtractTenantIdFromPayload(object eventSource)
    {
        // 尝试从 Payload 属性提取 TenantId
        var payloadProp = eventSource.GetType().GetProperty("Payload");
        if (payloadProp != null)
        {
            var payload = payloadProp.GetValue(eventSource);
            if (payload != null)
            {
                var tenantIdProp = payload.GetType().GetProperty("TenantId",
                    BindingFlags.Public | BindingFlags.Instance);
                if (tenantIdProp != null)
                {
                    return tenantIdProp.GetValue(payload)?.ToString() ?? "";
                }
            }
        }

        // 尝试直接从 TenantId 属性提取
        var directProp = eventSource.GetType().GetProperty("TenantId",
            BindingFlags.Public | BindingFlags.Instance);
        if (directProp != null)
        {
            return directProp.GetValue(eventSource)?.ToString() ?? "";
        }

        return "";
    }
}
