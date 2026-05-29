namespace JNPF.Schedule;

/// <summary>
/// 每年1月1号（午夜）开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class YearlyAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public YearlyAttribute()
        : base("@yearly")
    {
    }
}