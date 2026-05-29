namespace JNPF.Schedule;

/// <summary>
/// 每天特定小时开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DailyAtAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fields">字段值</param>
    public DailyAtAttribute(params object[] fields)
        : base("@daily", fields)
    {
    }
}