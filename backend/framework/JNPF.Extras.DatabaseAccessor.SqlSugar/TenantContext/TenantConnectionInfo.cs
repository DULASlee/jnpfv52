using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 租户连接配置数据类.
/// </summary>
public sealed class TenantConnectionInfo
{
    /// <summary>
    /// 连接配置 ID.
    /// </summary>
    public string ConfigId { get; set; } = "0";

    /// <summary>
    /// 连接字符串.
    /// </summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// 数据库类型.
    /// </summary>
    public global::SqlSugar.DbType DatabaseType { get; set; } = global::SqlSugar.DbType.SqlServer;

    /// <summary>
    /// 隔离类型（0=默认, 1=字段, 2=Schema）.
    /// </summary>
    public int IsolationType { get; set; }

    /// <summary>
    /// 隔离字段名.
    /// </summary>
    public string IsolationField { get; set; } = "";
}
