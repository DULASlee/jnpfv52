using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Manager;
using JNPF.Common.Models.InteAssistant;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model.Integrate;
using JNPF.EventBus;
using JNPF.EventHandler;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.Schedule;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JNPF.InteAssistant.Engine;

/// <summary>
/// 集成助手方法订阅.
/// </summary>
public class InteAssistantWayEventSubscriber : IEventSubscriber, ISingleton, IDisposable
{
    /// <summary>
    /// 初始化客户端.
    /// </summary>
    private readonly ISqlSugarClient _sqlSugarClient;

    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceScope _serviceScope;

    /// <summary>
    /// 作业计划工厂服务.
    /// </summary>
    private readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// 集成助手运行核心.
    /// </summary>
    private readonly InteAssistantRun _runService;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public InteAssistantWayEventSubscriber(
        IServiceScopeFactory serviceScopeFactory,
        ISqlSugarClient sqlSugarClient,
        InteAssistantRun runService,
        ISchedulerFactory schedulerFactory,
        ITenantManager tenantManager)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _sqlSugarClient = sqlSugarClient;
        _schedulerFactory = schedulerFactory;
        _runService = runService;
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 执行集成.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [EventSubscribe("Inte:ExecutiveInte")]
    public async Task ExecutiveIntegration(EventHandlerExecutingContext context)
    {
        var inte = (InteAssistantWayEventSource)context.Source;
        string cacheKey = string.Empty;

        var _cacheManager = _serviceScope.ServiceProvider.GetService<ICacheManager>();

        // CopyNew() MUST be called BEFORE ChangTenant to isolate tenant state.
        // ChangTenant mutates the SqlSugarScope's internal connections/filters;
        // calling it on the shared singleton would leak tenant state across requests.
        var db = _sqlSugarClient.CopyNew();

        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(inte.TenantId) && !db.AsTenant().IsAnyConnection(inte.TenantId))
        {
            await _tenantManager.ChangTenant(db, inte.TenantId);
        }

        cacheKey = string.Format("{0}:{1}", CommonConst.INTEASSISTANT, inte.TenantId);
        List<string> caCheIntegrateList = await _cacheManager.GetAsync<List<string>>(cacheKey);

        if (await db.Queryable<IntegrateQueueEntity>().AnyAsync(it => it.Id.Equals(inte.QueueId) && it.State == 1 && it.DeleteMark == null) && caCheIntegrateList.Contains(inte.QueueId))
        {
            var dataList = new List<InteAssiDataModel>();
            var queueEntity = await db.Queryable<IntegrateQueueEntity>().Where(it => it.Id.Equals(inte.QueueId)).FirstAsync();
            var inteEntity = await db.Queryable<IntegrateEntity>().Where(it => it.Id.Equals(queueEntity.IntegrateId)).FirstAsync();
            var dataValue = queueEntity.Description?.ToObject<InteAssiDataModel>();
            if (dataValue != null)
                dataList.Add(dataValue);

            var templateJson = inteEntity.TemplateJson.ToObject<DesignModel>();

            // 根据集成助手模版 组装任务纲要
            var inteAssiTaskOutlineModel = await _runService.InteAssiTaskOutline(templateJson, inteEntity.Type, inteEntity.Id);
            var taskId = SnowflakeIdHelper.NextId();

            var taskEntity = new IntegrateTaskEntity
            {
                Id = taskId,
                ProcessId = taskId,
                ExecutionTime = inteAssiTaskOutlineModel.startTime,
                TemplateJson = inteEntity.TemplateJson,
                Data = dataList.ToJsonString(),
                DataId = dataValue?.DataId,
                Type = inteEntity.Type,
                IntegrateId = inteEntity.Id,
                EnabledMark = 1,
                CreatorTime = DateTime.Now,
                CreatorUserId = queueEntity.CreatorUserId,
            };

            var runModel = await _runService.GetIntegrateNodeList(taskId, inteEntity.FormId, queueEntity.CreatorUserId, inte.TenantId, inteEntity.Type, inteEntity.FullName, templateJson, dataList, inteAssiTaskOutlineModel.nodeAttributes,taskEntity);

            // 为重试做准备
            taskEntity.Data = runModel.TaskData;

            if (inteAssiTaskOutlineModel.nodeAttributes.Count.Equals(runModel.NodeEntity.Count))
            {
                taskEntity.ResultType = 1;
            }

            // 操作成功
            var taskResult = await db.Insertable(taskEntity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            var nodeResult = await db.Insertable(runModel.NodeEntity).ExecuteCommandAsync();
            if (taskResult == 1)
            {
                // 删除 队列
                var result1 = await db.Updateable<IntegrateQueueEntity>().SetColumns(it => new IntegrateQueueEntity()
                {
                    DeleteMark = 1,
                    Description = null,
                    DeleteTime = SqlFunc.GetDate()
                }).Where(it => it.Id.Equals(queueEntity.Id) && it.IntegrateId.Equals(queueEntity.IntegrateId) && it.State.Equals(1) && it.DeleteMark == null).ExecuteCommandHasChangeAsync();

                if (result1)
                {
                    // 实时取下
                    caCheIntegrateList = await _cacheManager.GetAsync<List<string>>(cacheKey);

                    // 移除Redis 缓存
                    caCheIntegrateList.RemoveAll(it => it.Equals(queueEntity.Id));
                    await _cacheManager.SetAsync(cacheKey, caCheIntegrateList);

                    var triggerId = string.Format("Integrate_trigger_schedule_{0}", inte.TenantId);

                    var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_ExecutionQueue", out var scheduler);

                    var inteQueueCount = await db.Queryable<IntegrateQueueEntity>().CountAsync(it => it.State == 0 && it.ExecutionTime == null && it.DeleteMark == null);
                    if (inteQueueCount > 0)
                    {
                        // 启动调度
                        scheduler.StartTrigger(triggerId);
                    }
                    else
                    {
                        scheduler.RemoveTrigger(triggerId);
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}