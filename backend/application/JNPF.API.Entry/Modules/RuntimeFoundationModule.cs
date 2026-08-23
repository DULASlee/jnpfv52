using JNPF.Modules;
using JNPF.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// 运行时基座特性开关模块（M11）.
/// 绑定 App.json "RuntimeFoundation" 节四布尔位（默认全 false=行为与现状一致），
/// 启动时输出开关状态日志（规格 4.1.6 可观测性契约）.
/// M7~M10 各特性模块注册处以 IOptions&lt;RuntimeFoundationOptions&gt; 只读消费.
/// </summary>
public class RuntimeFoundationModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfigurableOptions<RuntimeFoundationOptions>();
    }

    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<RuntimeFoundationOptions>>().Value;
        var logger = app.ApplicationServices.GetRequiredService<ILogger<RuntimeFoundationModule>>();
        logger.LogInformation(
            "RuntimeFoundation switches: ExceptionBoundary={ExceptionBoundary}, OutboxSweeper={OutboxSweeper}, OutboundResilience={OutboundResilience}, QueryableLogging={QueryableLogging}",
            options.ExceptionBoundary, options.OutboxSweeper, options.OutboundResilience, options.QueryableLogging);
    }
}
