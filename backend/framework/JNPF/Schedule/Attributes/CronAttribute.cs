using JNPF.TimeCrontab;

namespace JNPF.Schedule;

/// <summary>
/// Cron 表达式作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CronAttribute : TriggerAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="schedule">Cron 表达式</param>
    /// <param name="format">Cron 表达式格式化类型</param>
    public CronAttribute(string schedule, CronStringFormat format = CronStringFormat.Default)
        : base(typeof(CronTrigger)
            , schedule, format)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="schedule">Cron 表达式</param>
    /// <param name="args">动态参数类型，支持 <see cref="int"/>，<see cref="CronStringFormat"/> 和 object[]</param>
    internal CronAttribute(string schedule, object args)
        : base(typeof(CronTrigger)
            , schedule, args)
    {
    }
}