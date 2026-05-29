namespace JNPF.Schedule;

/// <summary>
/// 作业处理程序监视器
/// </summary>
public interface IJobMonitor
{
    /// <summary>
    /// 作业处理程序执行前
    /// </summary>
    /// <param name="context">作业处理程序执行前上下文</param>
    /// <param name="stoppingToken">取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    Task OnExecutingAsync(JobExecutingContext context, CancellationToken stoppingToken);

    /// <summary>
    /// 作业处理程序执行后
    /// </summary>
    /// <param name="context">作业处理程序执行后上下文</param>
    /// <param name="stoppingToken">取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    Task OnExecutedAsync(JobExecutedContext context, CancellationToken stoppingToken);
}