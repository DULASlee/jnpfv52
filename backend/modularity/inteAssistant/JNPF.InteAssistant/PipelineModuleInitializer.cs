using Hangfire;
using Hangfire.MemoryStorage;
using JNPF.InteAssistant.Job;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace JNPF.InteAssistant;

/// <summary>
/// Pipeline 模块初始化器
/// 注册 Hangfire 后台任务 + Quartz.NET 定时调度
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-20
/// </summary>
public static class PipelineModuleInitializer
{
    /// <summary>
    /// 添加 Pipeline 调度服务（Hangfire + Quartz.NET）
    /// 在 Program.cs 中调用: builder.Services.AddPipelineScheduling(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddPipelineScheduling(
        this IServiceCollection services, IConfiguration configuration)
    {
        // SignalR（提供 IHubContext<PipelineHub>，供后台任务推送事件）
        services.AddSignalR();

        // ─── Hangfire 注册 ───
        services.AddHangfire(config =>
        {
            // 当前使用内存存储（开发/测试）
            // 生产环境需安装 Hangfire.SqlServer 包并切换：
            //   config.UseSqlServerStorage(configuration.GetConnectionString("Default"));
            config.UseMemoryStorage();
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Min(Environment.ProcessorCount * 2, 10);
            options.Queues = new[] { "default", "pipeline_validation" };
        });

        // ─── Hangfire 全局过滤器 ───
        // TraceContextJobFilter: W3C TraceContext 传播（分布式追踪）
        GlobalJobFilters.Filters.Add(new TraceContextJobFilter());
        // HangfireExceptionFilter: 异常捕获 + Serilog 日志（错误追踪闭环）
        GlobalJobFilters.Filters.Add(new HangfireExceptionFilter());

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

        return services;
    }
}
