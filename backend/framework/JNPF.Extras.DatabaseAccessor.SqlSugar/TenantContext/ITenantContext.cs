using Microsoft.AspNetCore.Http;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 租户上下文接口 — 提供当前请求的租户/系统/用户信息.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// 当前租户 ID.
    /// </summary>
    string TenantId { get; }

    /// <summary>
    /// 当前系统 ID.
    /// </summary>
    string SystemId { get; }

    /// <summary>
    /// 当前用户 ID.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// 当前租户的数据库连接配置.
    /// </summary>
    TenantConnectionInfo? ConnectionInfo { get; }

    /// <summary>
    /// 隔离类型（0=默认, 1=字段, 2=Schema）.
    /// </summary>
    int IsolationType { get; }

    /// <summary>
    /// 隔离字段值.
    /// </summary>
    string IsolationFieldValue { get; }

    /// <summary>
    /// 是否启用多租户.
    /// </summary>
    bool IsMultiTenant { get; }

    /// <summary>
    /// 是否启用多系统.
    /// </summary>
    bool IsMultiSystem { get; }

    /// <summary>
    /// 是否跳过系统过滤.
    /// </summary>
    bool ShouldSkipSystemFilter { get; }

    /// <summary>
    /// 从 HTTP 请求上下文设置租户信息.
    /// </summary>
    void SetFromHttpContext(HttpContext httpContext);

    /// <summary>
    /// 显式设置租户信息（后台任务入口）.
    /// </summary>
    void SetExplicit(string tenantId, string systemId = null);

    /// <summary>
    /// 从 EventBus 事件源设置租户信息.
    /// </summary>
    void SetFromEvent(object eventSource);

    /// <summary>
    /// 创建临时租户作用域（using 语句结束时自动恢复）.
    /// </summary>
    IDisposable BeginScope(TenantInfo info);

    /// <summary>
    /// 清除当前租户上下文（防 AsyncLocal 污染）.
    /// </summary>
    void ClearScope();

    /// <summary>
    /// 是否为默认租户.
    /// </summary>
    bool IsDefaultTenant();
}
