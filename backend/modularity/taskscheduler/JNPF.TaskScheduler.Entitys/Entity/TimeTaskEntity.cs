using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.TaskScheduler.Entitys;

/// <summary>
/// 定时任务
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("BASE_TIME_TASK")]
public class TimeTaskEntity : CLDSEntityBase
{
    /// <summary>
    /// 任务编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 任务名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 执行类型
    /// 1:数据接口 3:本地方法.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXECUTE_TYPE")]
    public int? ExecuteType { get; set; }

    /// <summary>
    /// 执行内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXECUTE_CONTENT")]
    public string ExecuteContent { get; set; }

    /// <summary>
    /// 执行周期.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXECUTE_CYCLE_JSON")]
    public string ExecuteCycleJson { get; set; }

    /// <summary>
    /// 最后运行时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_LAST_RUN_TIME")]
    public DateTime? LastRunTime { get; set; }

    /// <summary>
    /// 下次运行时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_NEXT_RUN_TIME")]
    public DateTime? NextRunTime { get; set; }

    /// <summary>
    /// 运行次数.
    /// </summary>
    [SugarColumn(ColumnName = "F_RUN_COUNT")]
    public int? RunCount { get; set; } = 0;

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}
