namespace JNPF.Schedule;

/// <summary>
/// 特定秒开始作业触发器特性
/// </summary>
[SecondlyAtAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SecondlyAtAttribute : CronAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fields">字段值</param>
    public SecondlyAtAttribute(params object[] fields)
        : base("@secondly", fields)
    {
    }
}