using JNPF.Common.Core.Job;
using JNPF.Common.Extension;
using JNPF.Common.Models.Job;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Schedule;
using JNPF.TaskScheduler.Entitys;
using JNPF.TaskScheduler.Entitys.Enum;
using JNPF.TimeCrontab;
using Microsoft.Extensions.DependencyInjection;

namespace JNPF.Common.Core.Manager.Job;

/// <summary>
/// 任务调度管理.
/// </summary>
public class JobManager : IJobManager, IScoped
{
    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化一个<see cref="JobManager"/>类型的新实例.
    /// </summary>
    /// <param name="serviceProvider">服务提供器.</param>
    public JobManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 获取作业触发器构建器(秒).
    /// </summary>
    /// <returns>作业触发器构建器.</returns>
    public TriggerBuilder CreateTriggerBuilder(JobTriggerModel model)
    {
        TriggerBuilder? triggerBuilder;
        if (model.Cron.IsNotEmptyOrNull())
        {
            triggerBuilder = model.Cron.Split(" ").Length == 7 ? Triggers.Cron(model.Cron, CronStringFormat.WithSecondsAndYears) : Triggers.Cron(model.Cron, CronStringFormat.WithSeconds);
            triggerBuilder.SetTriggerType("JNPF", "JNPF.Schedule.CronTrigger");
        }
        else
        {
            switch (model.intervalType)
            {
                case 1:
                    triggerBuilder = Triggers.PeriodSeconds(model.interval.ParseToLong());
                    break;
                case 2:
                    triggerBuilder = Triggers.PeriodMinutes(model.interval.ParseToLong());
                    break;
                case 3:
                    triggerBuilder = Triggers.PeriodHours(model.interval.ParseToLong());
                    break;
                default:
                    triggerBuilder = Triggers.Period(model.interval.ParseToLong());
                    break;
            }
            triggerBuilder.SetTriggerType("JNPF", "JNPF.Schedule.PeriodTrigger");
        }
        triggerBuilder.SetTriggerId(model.TriggreId);
        if (model.NumberOfRuns != null)
            triggerBuilder.SetNumberOfRuns(model.NumberOfRuns.ParseToLong());
        triggerBuilder.SetDescription(model.Description);
        triggerBuilder.SetStartTime(model.StartTime);
        if (model.EndTime != null)
        {
            triggerBuilder.SetEndTime(model.EndTime);
        }
        return triggerBuilder;
    }

    /// <summary>
    /// 获取作业触发器构建器.
    /// </summary>
    /// <returns>作业触发器构建器.</returns>
    public TriggerBuilder ObtainTriggerBuilder(JobTriggerModel model)
    {
        TriggerBuilder? triggerBuilder = TriggerBuilder.Create(model.TriggreId);

        if (model.NumberOfRuns != null)
            triggerBuilder.SetNumberOfRuns(model.NumberOfRuns.ParseToLong());
        triggerBuilder.SetTriggerType("JNPF", "JNPF.Schedule.CronTrigger");
        triggerBuilder.SetDescription(model.Description);
        triggerBuilder.SetStartTime(model.StartTime);
        if (model.EndTime != null)
            triggerBuilder.SetEndTime(model.EndTime);
        triggerBuilder.SetStartNow(model.StartNow);
        triggerBuilder.SetRunOnStart(model.RunOnStart);
        return triggerBuilder;
    }

    /// <summary>
    /// 获取Job类型为Http的作业信息.
    /// </summary>
    /// <param name="model">作业信息模型.</param>
    /// <returns></returns>
    public JobDetails ObtainJobHttpDetails(JobDetailModel model)
    {
        var server = _serviceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        var localAddress = addresses.FirstOrDefault().Replace("[::]", "localhost");

        var properties = new Dictionary<string, object>();

        var httpJob = new JNPFHttpJobMessage
        {
            RequestUri = string.Format("{0}{1}", localAddress, model.RequestUri),
            HttpMethod = model.HttpMethod,
            Body = model.Body,
            TaskId = model.TaskId,
            TenantId = model.TenantId,
            UserId = model.UserId,
        };

        properties.Add("JNPFHttpJob", httpJob.ToJsonString());

        return new JobDetails
        {
            JobId = model.JobId, // 作业 Id
            Description = model.Description,
            GroupName = model.GroupName, // 作业组名称
            Concurrent = true, // 并行还是串行方式，false 为 串行
            IncludeAnnotations = true, // 是否扫描 IJob 类型的触发器特性，true 为 扫描
            CreateType = RequestTypeEnum.Http,
            TenantId = model.TenantId,
            Properties = properties.ToJsonString()
        };
    }
}