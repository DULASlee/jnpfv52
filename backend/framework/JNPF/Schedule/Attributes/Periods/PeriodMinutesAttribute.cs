namespace JNPF.Schedule;

/// <summary>
/// 分钟周期（间隔）作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class PeriodMinutesAttribute : PeriodAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="interval">间隔（分钟）</param>
    public PeriodMinutesAttribute(long interval)
        : base(interval * 1000 * 60)
    {
    }
}