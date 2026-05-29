using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.System;

/// <summary>
/// 日程日志.
/// </summary>
[SugarTable("BASE_SCHEDULE_LOG")]
public class ScheduleLogEntity : CLDSEntityBase
{
    /// <summary>
    /// 类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    [SugarColumn(ColumnName = "F_URGENT")]
    public int Urgent { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    [SugarColumn(ColumnName = "F_TITLE")]
    public string Title { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTENT")]
    public string Content { get; set; }

    /// <summary>
    /// 全天.
    /// </summary>
    [SugarColumn(ColumnName = "F_ALL_DAY")]
    public int AllDay { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_DAY")]
    public DateTime StartDay { get; set; }

    /// <summary>
    /// 开始日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_TIME")]
    public string StartTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_END_DAY")]
    public DateTime EndDay { get; set; }

    /// <summary>
    /// 结束日期.
    /// </summary>
    [SugarColumn(ColumnName = "F_END_TIME")]
    public string EndTime { get; set; }

    /// <summary>
    /// 时长.
    /// </summary>
    [SugarColumn(ColumnName = "F_DURATION")]
    public int Duration { get; set; }

    /// <summary>
    /// 颜色.
    /// </summary>
    [SugarColumn(ColumnName = "F_COLOR")]
    public string Color { get; set; }

    /// <summary>
    /// 提醒.
    /// </summary>
    [SugarColumn(ColumnName = "F_REMINDER_TIME")]
    public int ReminderTime { get; set; }

    /// <summary>
    /// 提醒方式.
    /// </summary>
    [SugarColumn(ColumnName = "F_REMINDER_TYPE")]
    public int ReminderType { get; set; }

    /// <summary>
    /// 发送配置.
    /// </summary>
    [SugarColumn(ColumnName = "F_SEND_CONFIG_ID")]
    public string Send { get; set; }

    /// <summary>
    /// 发送配置名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_SEND_CONFIG_NAME")]
    public string SendName { get; set; }

    /// <summary>
    /// 重复提醒.
    /// </summary>
    [SugarColumn(ColumnName = "F_REPETITION")]
    public int Repetition { get; set; }

    /// <summary>
    /// 结束重复.
    /// </summary>
    [SugarColumn(ColumnName = "F_REPEAT_TIME")]
    public DateTime? RepeatTime { get; set; }

    /// <summary>
    /// 推送时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_PUSH_TIME")]
    public DateTime? PushTime { get; set; }

    /// <summary>
    /// 分组id.
    /// </summary>
    [SugarColumn(ColumnName = "F_GROUP_ID")]
    public string GroupId { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_Description")]
    public string Description { get; set; }

    /// <summary>
    /// 操作类型(1:新增，2：修改，3：删除，4：删除参与人).
    /// </summary>
    [SugarColumn(ColumnName = "F_OPERATION_TYPE")]
    public string OperationType { get; set; }

    /// <summary>
    /// 参与用户.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 日程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SCHEDULE_ID")]
    public string ScheduleId { get; set; }
}
