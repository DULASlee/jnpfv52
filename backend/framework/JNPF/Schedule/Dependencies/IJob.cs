namespace JNPF.Schedule;

/// <summary>
/// 作业处理程序
/// </summary>
public interface IJob
{
    /// <summary>
    /// 具体处理逻辑
    /// </summary>
    /// <param name="context">作业执行前上下文</param>
    /// <param name="stoppingToken">取消任务 Token</param>
    /// <returns><see cref="Task"/></returns>
    Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken);

    /// <summary>
    /// 作业处理程序执行异常回退逻辑
    /// </summary>
    /// <param name="context">作业执行前上下文</param>
    /// <param name="stoppingToken">取消任务 Token</param>
    /// <returns><see cref="Task"/></returns>
    Task FallbackAsync(JobExecutedContext context, CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}