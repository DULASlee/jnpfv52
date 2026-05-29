namespace JNPF.Schedule;

/// <summary>
/// 小时周期（间隔）作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class PeriodHoursAttribute : PeriodAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="interval">间隔（小时）</param>
    public PeriodHoursAttribute(long interval)
        : base(interval * 1000 * 60 * 60)
    {
    }
}