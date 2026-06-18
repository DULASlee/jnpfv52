using Hangfire;
using Hangfire.MemoryStorage;
using JNPF.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace JNPF.InteAssistant;

/// <summary>
/// Pipeline 调度模块 — Hangfire + Quartz.NET 注册
/// 得益于 JnpfModule 自动发现，无需修改 Program.cs
/// 版 本：v5.2.0
/// </summary>
[JNPF.Modules.DependsOn()]
public class PipelineSchedulingModule : JnpfModule
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // SignalR（提供 IHubContext<PipelineHub>，供 Quartz/Hangfire 任务推送实时事件）
        services.AddSignalR();

        // ─── Hangfire 注册 ───
        services.AddHangfire(config =>
        {
            // 当前使用内存存储（开发/测试）
            // 生产环境：安装 Hangfire.SqlServer 包后切换为
            //   config.UseSqlServerStorage(configuration.GetConnectionString("Default"));
            config.UseMemoryStorage();
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Min(Environment.ProcessorCount * 2, 10);
            options.Queues = new[] { "default", "pipeline_validation" };
        });

        // ─── Quartz.NET 注册 ───
        services.AddQuartz(q =>
        {
            // StaleMonitorService — 每小时整点执行
            var staleJobKey = new JobKey("StaleMonitorJob", "Pipeline");
            q.AddJob<StaleMonitorService>(opts => opts
                .WithIdentity(staleJobKey)
                .StoreDurably());

            q.AddTrigger(opts => opts
                .ForJob(staleJobKey)
                .WithIdentity("StaleMonitorTrigger", "Pipeline")
                .WithCronSchedule("0 0 * * * ?", x => x
                    .WithMisfireHandlingInstructionFireAndProceed()));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
            options.AwaitApplicationStarted = true;
        });
    }

    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        // Hangfire Dashboard — 开发环境启用，生产环境需加认证
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DashboardTitle = "JNPF Pipeline 任务调度",
            DisplayStorageConnectionString = false
        });

        // 注册 Pipeline Hub 路由（/hubs/pipeline）
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHubs();
        });
    }
}
