using JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;
using JNPF.Modules;

namespace JNPF.API.Entry.Modules;

/// <summary>
/// DiffLog 发布器替换模块。
/// 当 EnableDiffLog=true 时，将 NoOpDiffLogPublisher 替换为 OutboxDiffLogPublisher。
/// 不修改已封存的 SqlSugarConfigureExtensions.cs，通过 DI 覆盖实现。
/// </summary>
[JNPF.Modules.DependsOn(typeof(DatabaseModule))]
public class DiffLogPublishModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var enableDiffLog = configuration.GetValue<bool>("Database:EnableDiffLog");
        if (!enableDiffLog) return;

        // 移除 NoOp 注册，替换为 Outbox 版本
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDiffLogPublisher));
        if (descriptor != null)
            services.Remove(descriptor);

        services.AddScoped<IDiffLogPublisher, OutboxDiffLogPublisher>();

        // IEventOutboxStore：临时 NoOp，任务 5.3 完成后替换为 SqlSugarEventOutboxStore
        services.AddScoped<IEventOutboxStore, NoOpEventOutboxStore>();
    }
}
