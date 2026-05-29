namespace JNPF.Schedule;

/// <summary>
/// 每天（午夜）开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DailyAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DailyAttribute()
        : base("@daily")
    {
    }
}