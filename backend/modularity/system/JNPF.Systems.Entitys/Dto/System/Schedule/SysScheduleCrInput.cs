using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Schedule;

/// <summary>
/// 日程创建输入.
/// </summary>
[SuppressSniffer]
public class SysScheduleCrInput
{
    /// <summary>
    /// 类型.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? urgent { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public string content { get; set; }

    /// <summary>
    /// 全天.
    /// </summary>
    public int allDay { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime startDay { get; set; }

    /// <summary>
    /// 开始日期.
    /// </summary>
    public string startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public DateTime endDay { get; set; }

    /// <summary>
    /// 结束日期.
    /// </summary>
    public string endTime { get; set; }

    /// <summary>
    /// 时长.
    /// </summary>
    public int duration { get; set; }

    /// <summary>
    /// 颜色.
    /// </summary>
    public string color { get; set; }

    /// <summary>
    /// 提醒.
    /// -2:不提醒,-1:开始时,5:提前5分钟,10:提前10分钟,15:提前15分钟,30:提前30分钟,60:提前1小时,120:提前2小时,1440:提前1天.
    /// </summary>
    public int reminderTime { get; set; }

    /// <summary>
    /// 提醒方式.
    /// </summary>
    public int? reminderType { get; set; }

    /// <summary>
    /// 发送配置.
    /// </summary>
    public string send { get; set; }

    /// <summary>
    /// 发送配置名称.
    /// </summary>
    public string sendName { get; set; }

    /// <summary>
    /// 重复提醒.
    /// </summary>
    public string repetition { get; set; }

    /// <summary>
    /// 结束重复.
    /// </summary>
    public DateTime? repeatTime { get; set; }

    /// <summary>
    /// 参与人.
    /// </summary>
    public List<string> toUserIds { get; set; }

    /// <summary>
    /// 获取或设置 创建用户.
    /// </summary>
    public string creatorUserId { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    public string files { get; set; }
}
