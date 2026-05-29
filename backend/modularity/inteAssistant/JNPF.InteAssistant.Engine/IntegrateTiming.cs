using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.Job;
using JNPF.Common.Security;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.Schedule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Engine;

/// <summary>
/// 集成助手-定时触发
/// 不可删除！.
/// </summary>
[JobDetail("job_builtIn_IntegrateTiming", Description = "集成助手-定时触发", GroupName = "Integrate", Concurrent = true)]
public class IntegrateTiming : IJob, IDisposable
{
    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceScope _serviceScope;

    private readonly ILogger<IntegrateTiming> _logger;

    /// <summary>
    /// 作业计划工厂服务.
    /// </summary>
    private readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// 调度管理.
    /// </summary>
    private readonly IJobManager _jobManager;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public IntegrateTiming(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<IntegrateTiming> logger,
        IJobManager jobManager,
        ISchedulerFactory schedulerFactory,
        ITenantManager tenantManager)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _logger = logger;
        _jobManager = jobManager;
        _schedulerFactory = schedulerFactory;
        _tenantManager = tenantManager;
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var sqlSugarClient = _serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        string cacheKey = string.Empty;

        // 多租户场景 需要获取到 租户ID
        var tenantId = context.TriggerId.Match("(.+(?=_trigger_schedule_))");
        var taskId = context.TriggerId.Match("((?<=_trigger_schedule_).+)");

        var _cacheManager = _serviceScope.ServiceProvider.GetService<ICacheManager>();

        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId) && !sqlSugarClient.AsTenant().IsAnyConnection(tenantId))
        {
            await _tenantManager.ChangTenant(sqlSugarClient, tenantId);
        }

        sqlSugarClient = sqlSugarClient.CopyNew();

        var id = SnowflakeIdHelper.NextId();
        sqlSugarClient.Queryable<IntegrateEntity>().Where(it => it.Id.Equals(taskId)).Select(it => new { F_ID = id, F_INTEGRATE_ID = it.Id, F_FULL_NAME = it.FullName, F_STATE = 0, F_ENABLED_MARK = 1, F_TENANT_ID = it.TenantId }).IntoTable<IntegrateQueueEntity>();

        cacheKey = string.Format("{0}:{1}", CommonConst.INTEASSISTANT, tenantId);
        List<string> caCheIntegrateList = await _cacheManager.GetAsync<List<string>>(cacheKey);
        caCheIntegrateList ??= new List<string>();
        caCheIntegrateList?.Add(id);
        await _cacheManager.SetAsync(cacheKey, caCheIntegrateList);

        // 判断该租户的集成助手执行队列调度器是否存在
        var triggerId = string.Format("Integrate_trigger_schedule_{0}", tenantId);

        // 判断该租户的集成助手执行队列调度器是否存在
        var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_ExecutionQueue", out var scheduler);

        // 集成助手-执行队列 触发器只有在用户创建定时触发或者触发`事件触发`后创建
        if (!scheduler.ContainsTrigger(triggerId))
        {
            TriggerBuilder? triggerBuilder = _jobManager.ObtainTriggerBuilder(new JobTriggerModel
            {
                TriggreId = triggerId,
                Description = string.Format("租户`{0}`集成助手-执行队列调度器", tenantId),
                StartTime = DateTime.Now,
                EndTime = null,
            });

            triggerBuilder.AlterToSecondly();

            scheduler.AddTrigger(triggerBuilder);
        }
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}