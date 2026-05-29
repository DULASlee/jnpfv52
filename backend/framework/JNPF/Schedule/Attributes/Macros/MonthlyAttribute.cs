namespace JNPF.Schedule;

/// <summary>
/// 每月1号（午夜）开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MonthlyAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public MonthlyAttribute()
        : base("@monthly")
    {
    }
}