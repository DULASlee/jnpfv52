namespace JNPF.Schedule;

/// <summary>
/// 作业执行记录事件参数
/// </summary>
[SuppressSniffer]
public sealed class JobExecutionRecordEventArgs : EventArgs
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="timeline">作业触发器运行记录</param>
    public JobExecutionRecordEventArgs(TriggerTimeline timeline)
    {
        Timeline = timeline;
    }

    /// <summary>
    /// 作业触发器运行记录
    /// </summary>
    public TriggerTimeline Timeline { get; }
}