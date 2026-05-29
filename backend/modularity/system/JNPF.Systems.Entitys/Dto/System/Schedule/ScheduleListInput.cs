using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Schedule;

/// <summary>
/// 日程列表输入.
/// </summary>
[SuppressSniffer]
public class ScheduleListInput
{
    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public DateTime endTime { get; set; }
}
