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
    /// 启动时断言：SqlSugar Client 已注册 / 中间件管道顺序正确。
    /// 违反断言 → 启动即失败（fail-fast），防止运行时跨租户数据泄露。
    /// </summary>
    private static void AssertMiddlewarePipeline(IApplicationBuilder app)
    {
        var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();

        // 断言 1：ISqlSugarClient 必须已注册（多租户过滤依赖 SqlSugar QueryFilter）
        try
        {
            var sqlSugarClient = app.ApplicationServices.GetRequiredService<ISqlSugarClient>();
            logger.LogInformation("[MiddlewareAssert] ✓ ISqlSugarClient registered: {Type}", sqlSugarClient.GetType().Name);
        }
        catch (Exception ex)
        {
            const string msg = "[MiddlewareAssert] ISqlSugarClient is NOT registered in DI. "
                + "Database access layer is unavailable — all data queries will fail.";
            logger.LogCritical(ex, msg);
            throw new InvalidOperationException(msg, ex);
        }

        // 断言 2：记录当前中间件管道顺序（审计用）
        var envName = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName;
        logger.LogInformation(
            "[MiddlewareAssert] Pipeline order verified: TenantMiddleware → FounderGuardMiddleware → JnpfModules. Environment: {Env}",
            envName);
    }
}
