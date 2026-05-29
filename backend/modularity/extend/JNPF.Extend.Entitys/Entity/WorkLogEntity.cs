using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 工作日志
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("EXT_WORK_LOG")]
public class WorkLogEntity : CLDSEntityBase
{
    /// <summary>
    /// 日志标题.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_TITLE")]
    public string? Title { get; set; }

    /// <summary>
    /// 今天内容.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_TODAY_CONTENT")]
    public string? TodayContent { get; set; }

    /// <summary>
    /// 明天内容.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_TOMORROW_CONTENT")]
    public string? TomorrowContent { get; set; }

    /// <summary>
    /// 遇到问题.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_QUESTION")]
    public string? Question { get; set; }

    /// <summary>
    /// 发送给谁.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_TO_USER_ID")]
    public string? ToUserId { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
