namespace JNPF.Schedule;

/// <summary>
/// 每周一至周五（午夜）开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class WorkdayAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkdayAttribute()
        : base("@workday")
    {
    }
}