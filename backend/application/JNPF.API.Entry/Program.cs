using JNPF.API.Entry.Infrastructure;
using Serilog;

// Bootstrap logger: 捕获 Serilog 正式配置之前的启动阶段异常
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/startup.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Serve.Run(RunOptions.Default
        .AddWebComponent<WebComponent>().WithArgs(args));
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public class WebComponent : IWebComponent
{
    public void Load(WebApplicationBuilder builder, ComponentContext componentContext)
    {
        // Configure Serilog — 统一由 SerilogBootstrap 管理全部 Sink + 过滤
        SerilogBootstrap.Configure(builder.Configuration);
        builder.Host.UseSerilog();

        // 捕获未观察的 Task 异常，防止静默丢失
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Log.Warning(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        };

        builder.WebHost.ConfigureKestrel(options =>
        {
            // 长度最好不要设置 null
            options.Limits.MaxRequestBodySize = 52428800;
        });

        // ═══ Infrastructure 架构组件注册 ═══
        // W4: Common.Core → InteAssistant 依赖反转桥（组合根显式注册；实现亦标 ISingleton 供扫描）
        builder.Services.AddSingleton<JNPF.Bridges.IInteAssistantBridge, JNPF.InteAssistant.Bridges.InteAssistantBridge>();

        // 后台任务执行器（Singleton——全局唯一，追踪所有任务）
        builder.Services.AddSingleton<JNPF.InteAssistant.Infrastructure.Background.IBackgroundTaskRunner, JNPF.InteAssistant.Infrastructure.Background.BackgroundTaskRunner>();

        // 优雅关闭服务
        builder.Services.AddHostedService<JNPF.InteAssistant.Infrastructure.Background.BackgroundTaskShutdownService>();

        // SSE 发送器工厂（Singleton——工厂无状态）
        builder.Services.AddSingleton<JNPF.InteAssistant.Infrastructure.Messaging.ISseSenderFactory, JNPF.InteAssistant.Infrastructure.Messaging.SseSenderFactory>();

        // 多租户守卫（Transient——依赖 IHttpContextAccessor）
        builder.Services.AddTransient<JNPF.InteAssistant.Infrastructure.Security.ITenantGuard, JNPF.InteAssistant.Infrastructure.Security.TenantGuard>();

        // 门控管道配置（支持热重载）
        builder.Services.Configure<JNPF.InteAssistant.Gates.GatePipelineOptions>(
            builder.Configuration.GetSection(JNPF.InteAssistant.Gates.GatePipelineOptions.SectionName));

        builder.Services.Configure<JNPF.InteAssistant.Sa.SaPipelineOptions>(
            builder.Configuration.GetSection(JNPF.InteAssistant.Sa.SaPipelineOptions.SectionName));
    }
}
