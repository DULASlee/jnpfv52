using JNPF.Common.Core.Manager.Job;
using JNPF.Common.Models.Job;
using JNPF.Schedule;
using JNPF.TaskScheduler.Entitys;

namespace JNPF.Common.Core.Manager;

/// <summary>
/// 任务调度管理抽象.
/// </summary>
public interface IJobManager
{
    /// <summary>
    /// 获取作业触发器构建器.
    /// </summary>
    TriggerBuilder ObtainTriggerBuilder(JobTriggerModel model);

    /// <summary>
    /// 获取Job类型为Http的作业信息.
    /// </summary>
    /// <param name="model">作业信息模型.</param>
    JobDetails ObtainJobHttpDetails(JobDetailModel model);

    TriggerBuilder CreateTriggerBuilder(JobTriggerModel model);
}