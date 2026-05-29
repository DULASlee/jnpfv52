using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 系统集成队列
/// 版 本：v3.5.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2023-9-4.
/// </summary>
[SugarTable("BASE_INTEGRATE_QUEUE", TableDescription = "系统集成队列")]
public class IntegrateQueueEntity : CLDSEntityBase
{
    /// <summary>
    /// 集成名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 集成主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTEGRATE_ID")]
    public string IntegrateId { get; set; }

    /// <summary>
    /// 执行时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXECUTION_TIME")]
    public DateTime? ExecutionTime { get; set; }

    /// <summary>
    /// 状态
    /// 0-等待,1-执行中.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATE")]
    public int State { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}