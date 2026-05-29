using JNPF.DependencyInjection;

namespace JNPF.Common.Models.Job;

/// <summary>
/// 任务调度触发器模型.
/// </summary>
[SuppressSniffer]
public class JobTriggerModel
{
    /// <summary>
    /// 作业触发器 Id.
    /// </summary>
    public string TriggreId { get; set; }

    /// <summary>
    /// 描述信息.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 触发次数.
    /// </summary>
    public int? NumberOfRuns { get; set; }

    /// <summary>
    /// cron表达式.
    /// </summary>
    public string Cron { get; set; }

    /// <summary>
    /// 间隔时长.
    /// </summary>
    public long? interval { get; set; }

    /// <summary>
    /// 周期类型(0-毫秒 1-秒 2-分钟 3-小时).
    /// </summary>
    public int? intervalType { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否立即启动.
    /// </summary>
    public bool StartNow { get; set; } = true;

    /// <summary>
    /// 是否启动时执行一次.
    /// </summary>
    public bool RunOnStart { get; set; }
}