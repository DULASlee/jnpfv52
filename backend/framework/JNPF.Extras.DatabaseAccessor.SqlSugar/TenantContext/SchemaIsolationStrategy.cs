using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// Schema 隔离策略 — 通过数据库 Schema 隔离.
/// </summary>
public sealed class SchemaIsolationStrategy : ITenantIsolationStrategy
{
    public void ApplyQueryFilter(ISqlSugarClient db, ITenantContext context)
    {
        // Schema 隔离天然隔离，无需额外查询过滤
    }

    public void ApplyWriteProtection(ISqlSugarClient db, ITenantContext context)
    {
        // Schema 隔离天然隔离，无需额外写保护
    }

    public ConnectionConfig ConfigureConnection(TenantConnectionInfo info)
    {
        return new ConnectionConfig
        {
            ConfigId = info.ConfigId,
            ConnectionString = info.ConnectionString,
            DbType = info.DatabaseType,
            IsAutoCloseConnection = true,
            MoreSettings = new ConnMoreSettings
            {
                IsAutoRemoveDataCache = true,
                SqlServerCodeFirstNvarchar = true,
                IsAutoToUpper = false
            }
        };
    }
}
