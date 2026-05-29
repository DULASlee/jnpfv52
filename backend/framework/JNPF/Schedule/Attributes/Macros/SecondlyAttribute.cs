namespace JNPF.Schedule;

/// <summary>
/// 每秒开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SecondlyAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SecondlyAttribute()
        : base("@secondly")
    {
    }
}