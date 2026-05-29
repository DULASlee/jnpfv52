using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.System;

/// <summary>
/// 日程参与用户.
/// </summary>
[SugarTable("BASE_SCHEDULE_USER")]
public class ScheduleUserEntity : CLDSEntityBase
{
    /// <summary>
    /// 日程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SCHEDULE_ID")]
    public string ScheduleId { get; set; }

    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TO_USER_ID")]
    public string ToUserId { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 1.系统创建 2.用户创建.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int Type { get; set; }
}
