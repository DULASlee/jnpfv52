using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.TaskScheduler.Entitys;

/// <summary>
/// 定时任务日志
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("BASE_TIME_TASK_LOG")]
public class TimeTaskLogEntity : CLDSEntityBase
{
    /// <summary>
    /// 定时任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string TaskId { get; set; }

    /// <summary>
    /// 执行时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_RUN_TIME")]
    public DateTime? RunTime { get; set; }

    /// <summary>
    /// 执行结果.
    /// </summary>
    [SugarColumn(ColumnName = "F_RUN_RESULT")]
    public int? RunResult { get; set; }

    /// <summary>
    /// 执行说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}
