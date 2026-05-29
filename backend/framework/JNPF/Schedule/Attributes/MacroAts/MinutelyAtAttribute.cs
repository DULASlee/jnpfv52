namespace JNPF.Schedule;

/// <summary>
/// 每分钟特定秒开始作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MinutelyAtAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fields">字段值</param>
    public MinutelyAtAttribute(params object[] fields)
        : base("@minutely", fields)
    {
    }
}