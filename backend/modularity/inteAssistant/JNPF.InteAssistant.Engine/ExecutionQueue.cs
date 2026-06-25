using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.DatabaseAccessor;
using JNPF.EventBus;
using JNPF.EventHandler;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.Schedule;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Engine;

/// <summary>
/// 集成助手-执行队列
/// 不可删除！.
/// </summary>
[JobDetail("job_builtIn_ExecutionQueue", Description = "集成助手-执行队列", GroupName = "Integrate", Concurrent = false)]
public class ExecutionQueue : IJob
{
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<ExecutionQueue> _logger;

    /// <summary>
    /// 作业计划工厂服务.
    /// </summary>
    private readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// 事件总线.
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public ExecutionQueue(
        ISchedulerFactory schedulerFactory,
        ILogger<ExecutionQueue> logger,
        ISqlSugarClient sqlSugarClient,
        ICacheManager cacheManager,
        IEventPublisher eventPublisher,
        ITenantManager tenantManager)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
        _sqlSugarClient = sqlSugarClient;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _tenantManager = tenantManager;
    }

    [UnitOfWork]
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var sqlSugarClient = _sqlSugarClient;

        string cacheKey = string.Empty;

        // 多租户场景 需要获取到 租户ID
        var tenantId = context.TriggerId.Match("((?<=_trigger_schedule_).+)");

        var cacheManager = _cacheManager;

        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId) && !sqlSugarClient.AsTenant().IsAnyConnection(tenantId))
        {
            await _tenantManager.ChangTenant(sqlSugarClient, tenantId);
        }

        sqlSugarClient = sqlSugarClient.CopyNew();

        cacheKey = string.Format("{0}:{1}", CommonConst.INTEASSISTANT, tenantId);
        List<string> caCheIntegrateList = await _cacheManager.GetAsync<List<string>>(cacheKey);
        caCheIntegrateList ??= new List<string>();

        // 程序重启 之前任务未完成
        var entity = await sqlSugarClient.Queryable<IntegrateQueueEntity>().OrderBy(it => it.CreatorTime, OrderByType.Desc).FirstAsync(it => caCheIntegrateList.Contains(it.Id) && it.State == 1 && it.DeleteMark == null);

        // 找未启动任务
        if (entity == null)
            entity = await sqlSugarClient.Queryable<IntegrateQueueEntity>().OrderBy(it => it.CreatorTime, OrderByType.Desc).Where(it => caCheIntegrateList.Contains(it.Id) && it.State == 0 && it.ExecutionTime == null && it.DeleteMark == null).FirstAsync();

        // 能找到数据的时候 暂停调度器 等待执行完成
        if (entity != null)
        {
            var result = await sqlSugarClient.Updateable<IntegrateQueueEntity>().SetColumns(it => new IntegrateQueueEntity()
            {
                State = 1,
                ExecutionTime = SqlFunc.GetDate(),
                LastModifyTime = SqlFunc.GetDate()
            }).Where(it => it.Id == entity.Id).ExecuteCommandHasChangeAsync();

            // 执行成功 暂停任务 发送事件总线
            if (result)
            {
                var triggerId = string.Format("Integrate_trigger_schedule_{0}", tenantId);

                var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_ExecutionQueue", out var scheduler);

                // 先暂停触发器
                scheduler.PauseTrigger(triggerId);

                // 添加集成助手 任务方法 订阅
                await _eventPublisher.PublishAsync(new InteAssistantWayEventSource("Inte:ExecutiveInte", tenantId, entity.Id));
            }
        }
    }
}