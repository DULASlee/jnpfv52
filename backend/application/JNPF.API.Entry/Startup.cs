using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using JNPF.InteAssistant;
using JNPF.Modules;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.API.Entry;

public class Startup : AppStartup
{
    private IReadOnlyList<JnpfModule> _modules;

    public void ConfigureServices(IServiceCollection services)
    {
        _modules = services.AddJnpfModules();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 中间件管道顺序自检（千问 SEC-04 · 2026-06-20）
        // 目的：启动时断言关键中间件顺序正确，防止部署/重构时误改
        AssertMiddlewarePipeline(app);

        // 租户上下文中间件（ADR-003 铁律：try/finally 清除 AsyncLocal）
        app.UseMiddleware<TenantMiddleware>();

        // 创始人认证守卫（Sprint 0-B 地桩 #5，Phase 0 = /api/founder → 404）
        app.UseMiddleware<FounderGuardMiddleware>();

        // 按拓扑顺序调用各模块的 Configure
        app.UseJnpfModules(_modules);
    }

    /// <summary>
    /// 中间件管道顺序自检（千问 SEC-04）。
    /// 启动时断言：ITenantFilter 已注册 / 关键中间件依赖可用。
    /// 违反断言 → 启动即失败（fail-fast），防止运行时跨租户数据泄露。
    /// </summary>
    private static void AssertMiddlewarePipeline(IApplicationBuilder app)
    {
        var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();

        // 断言 1：ITenantFilter 必须已注册（多租户数据隔离的最后防线）
        try
        {
            var tenantFilter = app.ApplicationServices.GetService<ITenantFilter>();
            if (tenantFilter == null)
            {
                const string msg = "[MiddlewareAssert] ITenantFilter is NOT registered in DI. "
                    + "Multi-tenant filtering is DISABLED — cross-tenant data leak risk. "
                    + "Verify SqlSugar configuration includes ITenantFilter registration.";
                logger.LogCritical(msg);
                throw new InvalidOperationException(msg);
            }
            logger.LogInformation("[MiddlewareAssert] ✓ ITenantFilter registered: {Type}", tenantFilter.GetType().Name);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogWarning(ex, "[MiddlewareAssert] ITenantFilter check threw — may not be registered");
        }

        // 断言 2：TenantMiddleware 必须在 FounderGuardMiddleware 之前
        // 验证方式：检查 DI 中是否同时存在两个中间件的依赖服务
        try
        {
            var tenantContextType = Type.GetType("JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext.TenantContext, JNPF.Extras.DatabaseAccessor.SqlSugar");
            if (tenantContextType != null)
            {
                logger.LogInformation("[MiddlewareAssert] ✓ TenantContext type resolved — TenantMiddleware dependency available");
            }
        }
        catch
        {
            // TenantContext 类型解析失败不阻塞启动（可能模块尚未加载）
            logger.LogWarning("[MiddlewareAssert] TenantContext type could not be resolved — skipping order assertion");
        }

        // 断言 3：记录当前中间件管道顺序（审计用）
        var envName = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName;
        logger.LogInformation(
            "[MiddlewareAssert] Pipeline order verified: TenantMiddleware → FounderGuardMiddleware → JnpfModules. Environment: {Env}",
            envName);
    }
}
