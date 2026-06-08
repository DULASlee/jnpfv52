using JNPF.Common.Cache;
using JNPF.Common.Core;
using JNPF.Modules;
using JNPF.API.Entry.Services;
using JNPF.Common.Core.Handlers;
using JNPF.EventHandler;
using JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;
using JNPF.Schedule;
using JNPF.VirtualFileServer;
using SqlSugar;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// 数据库 + 缓存 + 事件总线 + 任务调度 + 文件服务模块.
/// </summary>
public class DatabaseModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // HttpContext 访问器（必须在 TenantMiddleware 之前注册）
        services.AddHttpContextAccessor();

        // 租户上下文（ADR-003）
        services.AddScoped<ITenantContext, TenantContextImpl>();
        services.AddScoped<ITenantResolver, ClaimTenantResolver>();
        services.AddScoped<ITenantResolver, FallbackTenantResolver>();

        // SqlSugar
        services.SqlSugarConfigure();

        // 可配置选项
        services.AddConfigurableOptions<CacheOptions>();
        services.AddConfigurableOptions<EventBusOptions>();
        services.AddConfigurableOptions<ConnectionStringsOptions>();
        services.AddConfigurableOptions<TenantOptions>();

        // 任务队列
        services.AddTaskQueue();

        // 任务调度
        services.AddSchedule(options => options.AddPersistence<DbJobPersistence>());

        // 视图引擎
        services.AddViewEngine();

        // 脱敏词汇检测
        services.AddSensitiveDetection();

        // WebSocket 服务
        services.AddWebSocketManager();

        // OSS 文件服务
        services.OSSServiceConfigure();

        // 日志磁盘空间保护
        services.AddHostedService<LogDiskGuardService>();

        // EventBus
        services.AddEventBus(options =>
        {
            var config = App.GetOptions<EventBusOptions>();

            if (config.EventBusType != EventBusType.Memory)
            {
                switch (config.EventBusType)
                {
                    case EventBusType.RabbitMQ:
                        var factory = new RabbitMQ.Client.ConnectionFactory
                        {
                            HostName = config.HostName,
                            UserName = config.UserName,
                            Password = config.Password,
                        };

                        var rbmqEventSourceStorer = new RabbitMQEventSourceStorer(factory, "eventbus", 3000);

                        options.ReplaceStorer(serviceProvider => rbmqEventSourceStorer);
                        break;
                }
            }

            options.UseUtcTimestamp = false;
            options.LogEnabled = true;
            options.AddExecutor<RetryEventHandlerExecutor>();
        });

    }

    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        app.UseWebSockets();

        app.MapWebSocketManager("/api/message/websocket",
            app.ApplicationServices.GetService<IMHandler>());
    }
}
