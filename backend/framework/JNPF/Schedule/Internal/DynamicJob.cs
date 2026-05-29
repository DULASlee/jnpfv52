namespace JNPF.Schedule;

/// <summary>
/// 动态作业处理程序
/// </summary>
internal sealed class DynamicJob : IJob
{
    /// <summary>
    /// 具体处理逻辑
    /// </summary>
    /// <param name="context">作业执行前上下文</param>
    /// <param name="stoppingToken">取消任务 Token</param>
    /// <returns><see cref="Task"/></returns>
    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var dynamicExecuteAsync = context.JobDetail.DynamicExecuteAsync;
        if (dynamicExecuteAsync == null) return;

        await dynamicExecuteAsync(context, stoppingToken);
    }
}