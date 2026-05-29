using JNPF.DependencyInjection;
using Newtonsoft.Json;

namespace JNPF.Systems.Entitys.Dto.Schedule;

[SuppressSniffer]
public class ScheduleDetailOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public string urgent { get; set; }

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
    public int? repetition { get; set; }

    /// <summary>
    /// 结束重复.
    /// </summary>
    public DateTime? repeatTime { get; set; }

    /// <summary>
    /// 参与人集合.
    /// </summary>
    [JsonIgnore]
    public List<string> toUserIdList { get; set; }

    /// <summary>
    /// 参与人.
    /// </summary>
    public string toUserIds { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUserId { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    public string files { get; set; }
}
