namespace JNPF.Schedule;

/// <summary>
/// 取消作业执行 Token 器
/// </summary>
public interface IJobCancellationToken
{
    /// <summary>
    /// 获取或创建取消作业执行 Token
    /// </summary>
    /// <param name="jobId">作业 Id</param>
    /// <param name="runId">作业运行唯一标识</param>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="CancellationToken"/></returns>
    CancellationTokenSource GetOrCreate(string jobId, Guid runId, CancellationToken stoppingToken);

    /// <summary>
    /// 取消（完成）本次作业执行
    /// </summary>
    /// <param name="jobId">作业 Id</param>
    /// <param name="runId">作业运行唯一标识</param>
    void Cancel(string jobId, Guid runId);

    /// <summary>
    /// 取消（完成）所有作业执行
    /// </summary>
    /// <param name="jobId"></param>
    void Cancel(string jobId);
}