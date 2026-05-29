namespace JNPF.TimeCrontab;

/// <summary>
/// Cron 表达式格式化类型
/// </summary>
[SuppressSniffer]
public enum CronStringFormat
{
    /// <summary>
    /// 默认格式
    /// </summary>
    /// <remarks>书写顺序：分 时 天 月 周</remarks>
    Default = 0,

    /// <summary>
    /// 带年份格式
    /// </summary>
    /// <remarks>书写顺序：分 时 天 月 周 年</remarks>
    WithYears = 1,

    /// <summary>
    /// 带秒格式
    /// </summary>
    /// <remarks>书写顺序：秒 分 时 天 月 周</remarks>
    WithSeconds = 2,

    /// <summary>
    /// 带秒和年格式
    /// </summary>
    /// <remarks>书写顺序：秒 分 时 天 月 周 年</remarks>
    WithSecondsAndYears = 3
}