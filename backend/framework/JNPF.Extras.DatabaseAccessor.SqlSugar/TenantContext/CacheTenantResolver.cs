using JNPF.Common.Manager;
using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// 从缓存解析租户连接配置.
/// </summary>
public sealed class CacheTenantResolver
{
    private readonly ICacheManager _cache;

    public CacheTenantResolver(ICacheManager cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 从缓存获取租户连接配置.
    /// 使用 SqlSugar 命名空间下的 GlobalTenantCacheModel（与现有缓存写入方兼容）.
    /// </summary>
    public TenantConnectionInfo? ResolveConnection(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
            return null;

        var cacheKey = "jnpf:global:tenant";
        var tenantCacheList = _cache.Get<List<GlobalTenantCacheModel>>(cacheKey);

        if (tenantCacheList == null)
            return null;

        var tenantCache = tenantCacheList.FirstOrDefault(t => t.TenantId == tenantId);
        if (tenantCache?.connectionConfig == null)
            return null;

        var defaultConfig = tenantCache.connectionConfig.DefaultConfig;

        return new TenantConnectionInfo
        {
            ConfigId = tenantCache.connectionConfig.ConfigId ?? "0",
            ConnectionString = defaultConfig?.connectionStr ?? "",
            DatabaseType = defaultConfig?.dbType ?? global::SqlSugar.DbType.SqlServer,
            IsolationType = tenantCache.type ?? 0,
            IsolationField = tenantCache.connectionConfig.IsolationField ?? ""
        };
    }
}
