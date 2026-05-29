using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Schedule;
using JNPF.TaskScheduler.Entitys;
using JNPF.TaskScheduler.Entitys.Enum;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JNPF.Common.Core;

/// <summary>
/// 作业持久化（数据库）.
/// </summary>
public class DbJobPersistence : IJobPersistence, IDisposable
{
    private readonly IServiceScope _serviceScope;

    private readonly ITenantManager _tenantManager;

    public DbJobPersistence(
        IServiceScopeFactory serviceScopeFactory, ITenantManager tenantManager)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 作业调度服务启动时.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SchedulerBuilder> Preload()
    {
        var sqlSugarClient = _serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        sqlSugarClient = sqlSugarClient.CopyNew();

        // 获取到对应库连接
        var sqlSugarScope = sqlSugarClient.AsTenant().GetConnectionScopeWithAttr<JobDetails>();
        var dynamicJobCompiler = _serviceScope.ServiceProvider.GetRequiredService<DynamicJobCompiler>();

        // 获取所有定义的作业
        var allJobs = App.EffectiveTypes.ScanToBuilders().ToList();

        // 若数据库不存在任何作业，则直接返回
        if (!sqlSugarScope.Queryable<JobDetails>().Any(u => true)) return allJobs;

        // 遍历所有定义的作业
        foreach (var schedulerBuilder in allJobs)
        {
            // 获取作业信息构建器
            var jobBuilder = schedulerBuilder.GetJobBuilder();

            // 加载数据库数据
            var dbDetail = sqlSugarScope.Queryable<JobDetails>().First(u => u.JobId == jobBuilder.JobId);
            if (dbDetail == null) continue;

            // 同步数据库数据
            jobBuilder.LoadFrom(dbDetail);

            // 获取作业的所有数据库的触发器
            var dbTriggers = sqlSugarScope.Queryable<JobTriggers>().Where(u => u.JobId == jobBuilder.JobId).ToList();

            // 遍历所有作业触发器
            foreach (var (_, triggerBuilder) in schedulerBuilder.GetEnumerable())
            {
                // 加载数据库数据
                var dbTrigger = dbTriggers.FirstOrDefault(u => u.JobId == jobBuilder.JobId && u.TriggerId == triggerBuilder.TriggerId);
                if (dbTrigger == null) continue;

                triggerBuilder.LoadFrom(dbTrigger).Updated(); // 标记更新
            }

            // 遍历所有非编译时定义的触发器加入到作业中
            foreach (var dbTrigger in dbTriggers)
            {
                if (schedulerBuilder.GetTriggerBuilder(dbTrigger.TriggerId)?.JobId == jobBuilder.JobId) continue;
                var triggerBuilder = TriggerBuilder.Create(dbTrigger.TriggerId).LoadFrom(dbTrigger);
                schedulerBuilder.AddTriggerBuilder(triggerBuilder); // 先添加
                triggerBuilder.Updated(); // 再标记更新
            }

            // 标记更新
            schedulerBuilder.Updated();
        }

        // 获取数据库所有通过脚本创建的作业
        var allDbScriptJobs = sqlSugarScope.Queryable<JobDetails>().Where(u => u.CreateType != RequestTypeEnum.BuiltIn).ToList();
        foreach (var dbDetail in allDbScriptJobs)
        {
            // 动态创建作业
            Type jobType;
            switch (dbDetail.CreateType)
            {
                case RequestTypeEnum.Http:
                    jobType = typeof(JNPFHttpJob);
                    break;

                default:
                    throw new NotSupportedException();
            }

            // 动态构建的 jobType 的程序集名称为随机名称，需重新设置
            dbDetail.AssemblyName = jobType.Assembly.FullName!.Split(',')[0];
            var jobBuilder = JobBuilder.Create(jobType).LoadFrom(dbDetail);

            // 强行设置为不扫描 IJob 实现类 [Trigger] 特性触发器，否则 SchedulerBuilder.Create 会再次扫描，导致重复添加同名触发器
            jobBuilder.SetIncludeAnnotations(false);

            // 获取作业的所有数据库的触发器加入到作业中
            var dbTriggers = sqlSugarScope.Queryable<JobTriggers>().Where(u => u.JobId == jobBuilder.JobId).ToArray();
            var triggerBuilders = dbTriggers.Select(u => TriggerBuilder.Create(u.TriggerId).LoadFrom(u).Updated());
            var schedulerBuilder = SchedulerBuilder.Create(jobBuilder, triggerBuilders.ToArray());

            // 标记更新
            schedulerBuilder.Updated();

            allJobs.Add(schedulerBuilder);
        }

        return allJobs;
    }

    /// <summary>
    /// 作业计划初始化通知.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public SchedulerBuilder OnLoading(SchedulerBuilder builder)
    {
        return builder;
    }

    /// <summary>
    /// 作业计划Scheduler的JobDetail变化时.
    /// </summary>
    /// <param name="context"></param>
    public async void OnChanged(PersistenceContext context)
    {
        var sqlSugarClient = _serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        sqlSugarClient = sqlSugarClient.CopyNew();

        // 获取到对应库连接
        var sqlSugarScope = sqlSugarClient.AsTenant().GetConnectionScopeWithAttr<JobDetails>();

        var jobDetail = context.JobDetail.Adapt<JobDetails>();
        switch (context.Behavior)
        {
            case PersistenceBehavior.Appended:
                await sqlSugarScope.Insertable(jobDetail).ExecuteCommandAsync();
                break;

            case PersistenceBehavior.Updated:
                await sqlSugarScope.Updateable(jobDetail).WhereColumns(u => new { u.JobId }).IgnoreColumns(u => new { u.Id, u.CreateType, u.ScriptCode, u.TenantId }).ExecuteCommandAsync();
                break;

            case PersistenceBehavior.Removed:
                await sqlSugarScope.Deleteable<JobDetails>().Where(u => u.JobId == jobDetail.JobId).ExecuteCommandAsync();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// 作业计划Scheduler的触发器Trigger变化时.
    /// </summary>
    /// <param name="context"></param>
    public async void OnTriggerChanged(PersistenceTriggerContext context)
    {
        var sqlSugarClient = _serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        sqlSugarClient = sqlSugarClient.CopyNew();

        // 获取到对应库连接
        var sqlSugarScope = sqlSugarClient.AsTenant().GetConnectionScopeWithAttr<JobDetails>();

        var jobTrigger = context.Trigger.Adapt<JobTriggers>();
        switch (context.Behavior)
        {
            case PersistenceBehavior.Appended:
                await sqlSugarScope.Insertable(jobTrigger).ExecuteCommandAsync();
                break;

            case PersistenceBehavior.Updated:
                await sqlSugarScope.Updateable(jobTrigger).WhereColumns(u => new { u.TriggerId, u.JobId }).IgnoreColumns(u => new { u.Id }).ExecuteCommandAsync();
                break;

            case PersistenceBehavior.Removed:
                await sqlSugarScope.Deleteable<JobTriggers>().Where(u => u.TriggerId == jobTrigger.TriggerId && u.JobId == jobTrigger.JobId).ExecuteCommandAsync();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// 作业触发记录.
    /// </summary>
    /// <param name="timeline"></param>
    public async void OnExecutionRecord(TriggerTimeline timeline)
    {
        var sqlSugarClient = _serviceScope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var timeTask = timeline.Adapt<TimeTaskEntity>();

        // 根据 `作业Id` 获取到租户ID
        var tenantId = timeline.TriggerId.Match("(.+(?=_trigger_schedule_))");
        timeTask.Id = timeline.TriggerId.Match("((?<=_trigger_schedule_).+)");

        if (timeline.JobId.Equals("job_builtIn_ExecutionQueue"))
        {
            tenantId = timeline.TriggerId.Match("((?<=_trigger_schedule_).+)");
            timeTask.Id = null;
        }

        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId) && !sqlSugarClient.AsTenant().IsAnyConnection(tenantId))
        {
            await _tenantManager.ChangTenant(sqlSugarClient, tenantId);
        }

        sqlSugarClient = sqlSugarClient.CopyNew();

        if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(timeTask.Id))
        {
            timeTask.LastModifyTime = DateTime.Now;
            await sqlSugarClient.Updateable(timeTask).WhereColumns(u => new { u.Id }).UpdateColumns(u => new { u.LastRunTime, u.NextRunTime, u.RunCount, u.LastModifyTime }).ExecuteCommandAsync();

            // 执行结果不为空、状态 为就绪时记录日记
            if ((timeline.Status.Equals(TriggerStatus.Ready) || timeline.Status.Equals(TriggerStatus.Archived)) && !string.IsNullOrEmpty(timeline.Result))
            {
                var timeTaskLog = new TimeTaskLogEntity
                {
                    Id = SnowflakeIdHelper.NextId(),
                    TaskId = timeTask.Id,
                    RunTime = timeTask.LastRunTime,
                    RunResult = 0,
                    Description = timeline.Result.ToJsonString(),
                };
                await sqlSugarClient.Insertable(timeTaskLog).ExecuteCommandAsync();
            }
        }
    }

    /// <summary>
    /// 释放服务作用域.
    /// </summary>
    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}