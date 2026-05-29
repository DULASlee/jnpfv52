using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Schedule;

/// <summary>
/// 日程信息输入.
/// </summary>
[SuppressSniffer]
public class ScheduleDetailInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 分组id.
    /// </summary>
    public string groupId { get; set; }
}
