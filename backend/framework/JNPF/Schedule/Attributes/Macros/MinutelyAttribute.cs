namespace JNPF.Schedule;

/// <summary>
/// 每分钟开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MinutelyAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public MinutelyAttribute()
        : base("@minutely")
    {
    }
}