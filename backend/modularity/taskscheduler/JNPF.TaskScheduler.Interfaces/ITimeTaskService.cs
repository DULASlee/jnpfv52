using JNPF.TaskScheduler.Entitys;

namespace JNPF.TaskScheduler.Interfaces.TaskScheduler;

/// <summary>
/// 定时任务
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
public interface ITimeTaskService
{
    /// <summary>
    /// 根据类型执行任务.
    /// </summary>
    /// <param name="entity">任务实体.</param>
    /// <returns></returns>
    Task<string> PerformJob(TimeTaskEntity entity);
}