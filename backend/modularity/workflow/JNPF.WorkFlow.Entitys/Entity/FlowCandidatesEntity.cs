using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程候选人.
/// </summary>
[SugarTable("FLOW_CANDIDATES")]
public class FlowCandidatesEntity : CLDEntityBase
{
    /// <summary>
    /// 任务id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 节点id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_NODE_ID")]
    public string? TaskNodeId { get; set; }

    /// <summary>
    /// 审批人id.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_ID")]
    public string? HandleId { get; set; }

    /// <summary>
    /// 审批人账号.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACCOUNT")]
    public string? Account { get; set; }

    /// <summary>
    /// 候选人.
    /// </summary>
    [SugarColumn(ColumnName = "F_CANDIDATES")]
    public string? Candidates { get; set; }

    /// <summary>
    /// 经办id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_OPERATOR_ID")]
    public string? TaskOperatorId { get; set; }

    /// <summary>
    /// 审批类型(1-候选人 2-异常处理人).
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }
}
