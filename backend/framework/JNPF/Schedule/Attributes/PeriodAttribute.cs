namespace JNPF.Schedule;

/// <summary>
/// 毫秒周期（间隔）作业触发器特性
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PeriodAttribute : TriggerAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="interval">间隔（毫秒）</param>
    public PeriodAttribute(long interval)
        : base(typeof(PeriodTrigger)
            , interval)
    {
    }
}