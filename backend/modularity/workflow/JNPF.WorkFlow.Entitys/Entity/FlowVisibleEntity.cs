using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程可见.
/// </summary>
[SugarTable("FLOW_VISIBLE")]
public class FlowVisibleEntity : CLDEntityBase
{
    /// <summary>
    /// 流程主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }

    /// <summary>
    /// 经办类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_OPERATOR_TYPE")]
    public string? OperatorType { get; set; }

    /// <summary>
    /// 经办主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OPERATOR_ID")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// 可见类型（1：发起 2：协管）.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }
}
