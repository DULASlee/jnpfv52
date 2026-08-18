using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 字段隔离策略 — 通过 TenantId 字段过滤.
/// </summary>
public sealed class ColumnIsolationStrategy : ITenantIsolationStrategy
{
    public void ApplyQueryFilter(ISqlSugarClient db, ITenantContext context)
    {
        if (string.IsNullOrEmpty(context.IsolationFieldValue))
            return;

        // r4-safe: 超管跨租户管理，豁免 ITenantFilter（方案 A，详见 AdminBypassGuard）
        if (AdminBypassGuard.IsAdministrator())
            return;

        var fieldValue = context.IsolationFieldValue;
        db.QueryFilter.AddTableFilter<ITenantFilter>(it => it.TenantId == fieldValue);
    }

    public void ApplyWriteProtection(ISqlSugarClient db, ITenantContext context)
    {
        // 写保护由 ConfigureGlobalDataExecuting 在启动时统一处理
        // 此处无需额外操作
    }

    public ConnectionConfig ConfigureConnection(TenantConnectionInfo info)
    {
        return new ConnectionConfig
        {
            ConfigId = info.ConfigId,
            ConnectionString = info.ConnectionString,
            DbType = info.DatabaseType,
            IsAutoCloseConnection = true
        };
    }
}
