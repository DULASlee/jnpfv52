namespace JNPF.Schedule;

/// <summary>
/// 每周特定星期几（午夜）开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class WeeklyAtAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fields">字段值</param>
    public WeeklyAtAttribute(params object[] fields)
        : base("@weekly", fields)
    {
    }
}