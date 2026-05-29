namespace JNPF.Schedule;

/// <summary>
/// 作业计划事件参数
/// </summary>
[SuppressSniffer]
public sealed class SchedulerEventArgs : EventArgs
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="jobDetail">作业信息</param>
    public SchedulerEventArgs(JobDetail jobDetail)
    {
        JobDetail = jobDetail;
    }

    /// <summary>
    /// 作业信息
    /// </summary>
    public JobDetail JobDetail { get; }
}