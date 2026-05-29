using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程驳回数据.
/// </summary>
[SugarTable("FLOW_REJECT_DATA")]
[Tenant(ClaimConst.TENANTID)]
public class FlowRejectDataEntity : CLDEntityBase
{
    /// <summary>
    /// 任务数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_JSON")]
    public string? TaskJson { get; set; }

    /// <summary>
    /// 节点数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_NODE_JSON")]
    public string? TaskNodeJson { get; set; }

    /// <summary>
    /// 经办数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_OPERATOR_JSON")]
    public string? TaskOperatorJson { get; set; }
}
