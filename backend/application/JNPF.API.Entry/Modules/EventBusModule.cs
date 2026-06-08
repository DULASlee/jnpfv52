using JNPF.EventBus;
using JNPF.EventHandler;
using JNPF.Extras.EventBus.Idempotency;
using JNPF.Extras.EventBus.Outbox;
using JNPF.Modules;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// 事件总线增强模块。
/// 注册 Polly 重试执行器、幂等处理器、Outbox 调度器。
/// </summary>
[JNPF.Modules.DependsOn(typeof(DatabaseModule))]
public class EventBusModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 替换 RetryEventHandlerExecutor 为 Polly 版本
        var oldExec = services.FirstOrDefault(d => d.ServiceType == typeof(IEventHandlerExecutor));
        if (oldExec != null)
            services.Remove(oldExec);
        services.AddSingleton<IEventHandlerExecutor, PollyRetryHandlerExecutor>();

        // Outbox 存储和调度器
        services.AddScoped<SqlSugarEventOutboxStore>();
        services.AddHostedService<EventOutboxDispatcher>();

        // IEventOutboxStore 实现（替换 DiffLogPublishModule 中的 NoOp）
        var noOpStore = services.FirstOrDefault(d =>
            d.ServiceType == typeof(JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog.IEventOutboxStore));
        if (noOpStore != null)
            services.Remove(noOpStore);
        services.AddScoped<JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog.IEventOutboxStore,
            OutboxStoreAdapter>();
    }
}

/// <summary>
/// IEventOutboxStore 适配器 — 桥接框架层接口与基础设施层实现。
/// </summary>
internal class OutboxStoreAdapter : JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog.IEventOutboxStore
{
    private readonly SqlSugarEventOutboxStore _store;

    public OutboxStoreAdapter(SqlSugarEventOutboxStore store)
    {
        _store = store;
    }

    public Task WriteAsync(string eventName, object payload)
    {
        return _store.WriteAsync(eventName, payload);
    }
}
