using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Schedule;
using JNPF.Systems.Interfaces.System;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JNPF.TaskScheduler.Listener;

[JobDetail("job_schedule", Description = "程序启动日程推送", GroupName = "schedule", Concurrent = false)]
[PeriodSeconds(1, TriggerId = "trigger_schedule", Description = "程序启动日程推送", MaxNumberOfRuns = 1, RunOnStart = true)]
[Daily(TriggerId = "trigger_scheduleRefresh", Description = "程序启动每日刷新日程推送")]
public class ScheduleJob : IJob
{
    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="serviceProvider">服务提供器.</param>
    public ScheduleJob(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 程序启动日程推送.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        using var serviceScope = _serviceProvider.CreateScope();
        var _cacheManager = serviceScope.ServiceProvider.GetService<ICacheManager>();
        var _scheduleService = serviceScope.ServiceProvider.GetService<IScheduleService>();

        var tenantList = await _cacheManager.GetAsync<List<GlobalTenantCacheModel>>(CommonConst.GLOBALTENANT);

        // 获取全局租户缓存.
        if (tenantList.IsNotEmptyOrNull())
        {
            foreach (var tenant in tenantList)
            {
                var entityList = await _scheduleService.GetCalendarDayPushList(tenant.TenantId);

                foreach (var item in entityList)
                {
                    await _scheduleService.AddPushTaskQueue(item, "MBXTRC001", "1", tenant.TenantId);
                    await _cacheManager.SetAsync(string.Format("{0}:{1}:{2}", CommonConst.CACHEKEYSCHEDULE, tenant.TenantId, item.Id), item.PushTime.ToString(), TimeSpan.FromDays(1));
                }
            }
        }

        var originColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(string.Format("【{0}】服务当天日程推送加载", DateTime.Now));
        Console.ForegroundColor = originColor;
    }
}