namespace JNPF.Schedule;

/// <summary>
/// 每年特定月1号（午夜）开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class YearlyAtAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fields">字段值</param>
    public YearlyAtAttribute(params object[] fields)
        : base("@yearly", fields)
    {
    }
}