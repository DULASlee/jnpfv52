namespace JNPF.Schedule;

/// <summary>
/// 每月特定天（午夜）开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MonthlyAtAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fields">字段值</param>
    public MonthlyAtAttribute(params object[] fields)
        : base("@monthly", fields)
    {
    }
}