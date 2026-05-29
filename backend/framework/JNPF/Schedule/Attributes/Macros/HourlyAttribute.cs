namespace JNPF.Schedule;

/// <summary>
/// 每小时开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class HourlyAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public HourlyAttribute()
        : base("@hourly")
    {
    }
}