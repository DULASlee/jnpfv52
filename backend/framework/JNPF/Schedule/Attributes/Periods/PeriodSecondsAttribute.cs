namespace JNPF.Schedule;

/// <summary>
/// 秒周期（间隔）作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class PeriodSecondsAttribute : PeriodAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="interval">间隔（秒）</param>
    public PeriodSecondsAttribute(long interval)
        : base(interval * 1000)
    {
    }
}