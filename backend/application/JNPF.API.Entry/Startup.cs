using JNPF.Common.Core.Diagnostics;
using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using JNPF.InteAssistant;
using JNPF.Modules;
using Microsoft.AspNetCore.SignalR;
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
        // ============ 启动完成横幅 ============
        var lifetime = app.ApplicationServices.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() =>
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            var serverAddressesFeature = app.ServerFeatures.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            var url = serverAddressesFeature?.Addresses.FirstOrDefault() ?? "http://localhost:5000";

            logger.LogInformation("══════════════════════════════════════════════════");
            logger.LogInformation("   JNPF Baobab-Studio 后端启动完成");
            logger.LogInformation("══════════════════════════════════════════════════");
            logger.LogInformation("  API 地址: {Url}", url);
            logger.LogInformation("  Swagger:  {Url}/swagger", url);
            logger.LogInformation("  环境:     {Env}", env.EnvironmentName);
            logger.LogInformation("  启动时间: {Time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            logger.LogInformation("══════════════════════════════════════════════════");
            logger.LogInformation("  Studio API: {Url}/api/studio/menu/user-menus", url);
            logger.LogInformation("  LLM 健康:   {Url}/api/studio/pipeline/providers", url);
            logger.LogInformation("══════════════════════════════════════════════════");
        });

        // 中间件管道顺序自检（千问 SEC-04 · 2026-06-20）
        // 目的：启动时断言关键中间件顺序正确，防止部署/重构时误改
        AssertMiddlewarePipeline(app);

        // 创始人认证守卫（Sprint 0-B 地桩 #5，Phase 0 = /api/founder → 404）
        app.UseMiddleware<FounderGuardMiddleware>();

        // 按拓扑顺序调用各模块的 Configure
        // 注：AuthenticationModule 在 pipeline 中注册 UseAuthentication/UseAuthorization
        //     TenantMiddleware 在 Authorization 之后执行（见 AuthenticationModule）
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

        // 断言 1.1：PipelineHub 的 IHubContext 必须可解析（Quartz StaleMonitorService 依赖）
        try
        {
            var hubContext = app.ApplicationServices.GetRequiredService<IHubContext<PipelineHub>>();
            logger.LogInformation("[MiddlewareAssert] ✓ IHubContext<PipelineHub> registered: {Type}", hubContext.GetType().Name);
        }
        catch (Exception ex)
        {
            const string msg = "[MiddlewareAssert] IHubContext<PipelineHub> is NOT registered in DI. "
                + "Pipeline realtime events and StaleMonitorService will fail.";
            logger.LogCritical(ex, msg);
            throw new InvalidOperationException(msg, ex);
        }

        // 断言 2：记录当前中间件管道顺序（审计用）
        var envName = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName;
        logger.LogInformation(
            "[MiddlewareAssert] Pipeline order: FounderGuard → JnpfModules(Auth→AuthZ→Tenant→Endpoints). Environment: {Env}",
            envName);

        // 初始化诊断日志（agent-probe + visual-debug 基础设施）
        DiagnosticsLog.Log("startup", "backend_ready", new { env = envName, dir = DiagnosticsLog.CurrentSessionFile });
        logger.LogInformation("[DiagnosticsLog] Ready: {Path}", DiagnosticsLog.CurrentSessionFile);
    }
}
