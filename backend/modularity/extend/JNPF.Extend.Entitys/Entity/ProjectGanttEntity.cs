using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 项目计划
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.yinmaisoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("EXT_PROJECT_GANTT")]
public class ProjectGanttEntity : CLDSEntityBase
{
    /// <summary>
    /// 项目上级.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 项目主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROJECT_ID")]
    public string? ProjectId { get; set; }

    /// <summary>
    /// 项目类型：【1-项目、2-任务】.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 项目编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 项目名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 项目工期.
    /// </summary>
    [SugarColumn(ColumnName = "F_TIME_LIMIT")]
    public decimal? TimeLimit { get; set; }

    /// <summary>
    /// 项目标记.
    /// </summary>
    [SugarColumn(ColumnName = "F_SIGN")]
    public string? Sign { get; set; }

    /// <summary>
    /// 标记颜色.
    /// </summary>
    [SugarColumn(ColumnName = "F_SIGN_COLOR")]
    public string? SignColor { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_TIME")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_END_TIME")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 当前进度.
    /// </summary>
    [SugarColumn(ColumnName = "F_SCHEDULE")]
    public int? Schedule { get; set; }

    /// <summary>
    /// 负责人.
    /// </summary>
    [SugarColumn(ColumnName = "F_MANAGER_IDS")]
    public string? ManagerIds { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
