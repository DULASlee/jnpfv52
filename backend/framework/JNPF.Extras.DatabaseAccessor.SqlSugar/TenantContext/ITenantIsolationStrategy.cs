using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 租户隔离策略接口.
/// </summary>
public interface ITenantIsolationStrategy
{
    /// <summary>
    /// 应用查询过滤.
    /// </summary>
    void ApplyQueryFilter(ISqlSugarClient db, ITenantContext context);

    /// <summary>
    /// 应用写保护.
    /// </summary>
    void ApplyWriteProtection(ISqlSugarClient db, ITenantContext context);

    /// <summary>
    /// 配置连接.
    /// </summary>
    ConnectionConfig ConfigureConnection(TenantConnectionInfo info);
}
