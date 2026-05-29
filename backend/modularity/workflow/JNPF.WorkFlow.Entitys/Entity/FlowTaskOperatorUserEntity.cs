using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程依次审批人表.
/// </summary>
[SugarTable("FLOW_TASK_OPERATOR_USER")]
[Tenant(ClaimConst.TENANTID)]
public class FlowTaskOperatorUserEntity : CLDEntityBase
{
    /// <summary>
    /// 加签处理人.
    /// </summary>
    [SugarColumn(ColumnName = "F_APPEND_HANDLE_ID")]
    public string? AppendHandleId { get; set; }

    /// <summary>
    /// 经办主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_ID")]
    public string? HandleId { get; set; }

    /// <summary>
    /// 处理状态：【0-拒绝、1-同意】.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_STATUS")]
    public int? HandleStatus { get; set; }

    /// <summary>
    /// 处理时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_TIME")]
    public DateTime? HandleTime { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_NAME")]
    public string? NodeName { get; set; }

    /// <summary>
    /// 是否完成【0-未处理、1-已审核】.
    /// </summary>
    [SugarColumn(ColumnName = "F_COMPLETION")]
    public int? Completion { get; set; }

    /// <summary>
    /// 描述(超时时间).
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 节点主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_NODE_ID")]
    public string? TaskNodeId { get; set; }

    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 审批状态(0:正常、1:加签、-1:作废).
    /// </summary>
    [SugarColumn(ColumnName = "F_STATE")]
    public int? State { get; set; }

    /// <summary>
    /// 加签人.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 保存数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_DRAFT_DATA")]
    public string? DraftData { get; set; }

    /// <summary>
    /// 回退id.
    /// </summary>
    [SugarColumn(ColumnName = "F_ROLLBACK_ID")]
    public string? RollbackId { get; set; }
}
