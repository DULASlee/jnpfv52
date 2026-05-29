using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Schedule;

/// <summary>
/// 日程修改输入.
/// </summary>
[SuppressSniffer]
public class SysScheduleUpInput : SysScheduleCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}
