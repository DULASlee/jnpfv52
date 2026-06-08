namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// AsyncLocal 值对象 — 封装当前请求的租户信息.
/// </summary>
public sealed class TenantInfo
{
    /// <summary>
    /// 租户 ID.
    /// </summary>
    public string TenantId { get; set; } = "";

    /// <summary>
    /// 系统 ID.
    /// </summary>
    public string SystemId { get; set; } = "";

    /// <summary>
    /// 用户 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 租户数据库连接信息.
    /// </summary>
    public TenantConnectionInfo? ConnectionInfo { get; set; }

    /// <summary>
    /// 隔离类型（0=默认, 1=字段, 2=Schema）.
    /// </summary>
    public int IsolationType { get; set; }

    /// <summary>
    /// 隔离字段值.
    /// </summary>
    public string IsolationFieldValue { get; set; } = "";

    /// <summary>
    /// 是否启用多租户.
    /// </summary>
    public bool IsMultiTenant { get; set; }

    /// <summary>
    /// 是否启用多系统.
    /// </summary>
    public bool IsMultiSystem { get; set; }

    /// <summary>
    /// 是否跳过系统过滤（orgSystem 场景）.
    /// </summary>
    public bool ShouldSkipSystemFilter { get; set; }

    /// <summary>
    /// 是否为默认租户.
    /// </summary>
    public bool IsDefaultTenant() =>
        string.IsNullOrEmpty(TenantId) || TenantId == "default";
}
