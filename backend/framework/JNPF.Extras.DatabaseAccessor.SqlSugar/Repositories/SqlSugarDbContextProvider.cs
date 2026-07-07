using JNPF;
using JNPF.Common.Manager;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace SqlSugar;

/// <summary>
/// SqlSugar 数据库上下文提供器实现（阶段 4 重构版）.
/// 职责：根据 ITenantContext 解析正确的数据库连接作用域 + 应用查询过滤器.
/// AOP 配置已迁移到 SqlSugarConfigureExtensions（阶段 1 封存文件）.
/// </summary>
public class SqlSugarDbContextProvider : ISqlSugarDbContextProvider
{
    private readonly SqlSugarScope _rootScope;
    private readonly ITenantContext _tenantContext;

    public SqlSugarDbContextProvider(
        ISqlSugarClient rootContext,
        ITenantContext tenantContext)
    {
        _rootScope = (SqlSugarScope)rootContext;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// 获取当前请求对应的 SqlSugar 客户端.
    /// </summary>
    public ISqlSugarClient GetDbContext()
    {
        // 第一步：根据租户上下文解析正确的数据库连接作用域
        var context = ResolveTenantConnection();

        // 第二步：应用租户字段隔离查询过滤器
        // r4-safe: 超管跨租户管理，豁免 ITenantFilter（方案 A，详见 AdminBypassGuard）
        if (_tenantContext.IsMultiTenant && !_tenantContext.IsDefaultTenant()
            && _tenantContext.IsolationType == 1
            && !AdminBypassGuard.IsAdministrator())
        {
            var fieldValue = _tenantContext.IsolationFieldValue;
            if (!string.IsNullOrEmpty(fieldValue))
            {
                context.QueryFilter.AddTableFilter<ITenantFilter>(
                    it => it.TenantId == fieldValue);
            }
        }

        // 第三步：应用系统级数据过滤（ZxSystemId）
        if (_tenantContext.IsMultiSystem && !_tenantContext.ShouldSkipSystemFilter
            && !string.IsNullOrEmpty(_tenantContext.SystemId))
        {
            var systemId = _tenantContext.SystemId;
            context.QueryFilter.AddTableFilter<IZxSystemFilter>(
                it => it.ZxSystemId == systemId);
        }

        // AOP 配置：已迁移到 SqlSugarConfigureExtensions.SetDbAop()（阶段 1 封存）
        // CopyNew() 继承父级 AOP（ADR-002 情况 B），不需要重复配置

        return context;
    }

    /// <summary>
    /// 根据租户上下文解析正确的数据库连接作用域.
    /// </summary>
    private ISqlSugarClient ResolveTenantConnection()
    {
        var httpContext = App.GetService<IHttpContextAccessor>()?.HttpContext;

        // 匿名端点不进行租户解析
        if (httpContext?.GetEndpoint()?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            return _rootScope;
        }

        // 非多租户或默认租户：使用默认连接
        if (!_tenantContext.IsMultiTenant || _tenantContext.IsDefaultTenant())
        {
            return _rootScope;
        }

        // 租户连接信息未解析（缓存未命中）：降级到默认连接
        var connectionInfo = _tenantContext.ConnectionInfo;
        if (connectionInfo == null)
        {
            return _rootScope;
        }

        try
        {
            // ADR-006：连接字典共享，GetConnectionScope 直接获取已注册的连接
            return _rootScope.AsTenant().GetConnectionScope(connectionInfo.ConfigId);
        }
        catch
        {
            // 租户连接尚未注册到 SqlSugarScope（首次请求该租户）：
            // 动态注册后重试，仅发生一次
            var tenantCache = GetTenantCacheFromMemory(_tenantContext.TenantId);
            if (tenantCache?.connectionConfig != null)
            {
                _rootScope.AsTenant().AddConnection(
                    JNPFTenantExtensions.GetConfig(tenantCache.connectionConfig));
                return _rootScope.AsTenant().GetConnectionScope(connectionInfo.ConfigId);
            }

            // 降级到默认连接
            return _rootScope;
        }
    }

    /// <summary>
    /// 从 IMemoryCache 直接读取租户缓存（避免 ICacheManager 的 Scoped 生命周期问题）.
    /// </summary>
    private static GlobalTenantCacheModel? GetTenantCacheFromMemory(string tenantId)
    {
        try
        {
            var cache = App.GetService<ICacheManager>();
            var tenantCacheList = cache?.Get<List<GlobalTenantCacheModel>>("jnpf:global:tenant");
            return tenantCacheList?.FirstOrDefault(t => t.TenantId == tenantId);
        }
        catch
        {
            return null;
        }
    }
}
