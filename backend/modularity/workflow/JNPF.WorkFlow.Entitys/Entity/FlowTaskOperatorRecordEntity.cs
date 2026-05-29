using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程经办记录.
/// </summary>
[SugarTable("FLOW_TASK_OPERATOR_RECORD")]
[Tenant(ClaimConst.TENANTID)]
public class FlowTaskOperatorRecordEntity : CLDEntityBase
{
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
    /// 经办状态：【0-拒绝、1-同意、2-提交、3-撤回、4-终止、5-指派、6-加签、7-转办、8-变更、9-复活、10-前加签、11-挂起, 12-恢复、13-转向】.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_STATUS")]
    public int HandleStatus { get; set; } = 0;

    /// <summary>
    /// 经办人员.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_ID")]
    public string? HandleId { get; set; }

    /// <summary>
    /// 经办时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_TIME")]
    public DateTime? HandleTime { get; set; }

    /// <summary>
    /// 经办理由.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_OPINION")]
    public string? HandleOpinion { get; set; }

    /// <summary>
    /// 经办主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_OPERATOR_ID")]
    public string TaskOperatorId { get; set; }

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
    /// 电子签名.
    /// </summary>
    [SugarColumn(ColumnName = "F_SIGN_IMG")]
    public string? SignImg { get; set; }

    /// <summary>
    /// 审批标识(1:加签人).
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int? Status { get; set; }

    /// <summary>
    /// 流转操作人.
    /// </summary>
    [SugarColumn(ColumnName = "F_OPERATOR_ID")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILE_LIST")]
    public string? FileList { get; set; }

    /// <summary>
    /// 审批数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_DRAFT_DATA")]
    public string? DraftData { get; set; }

    /// <summary>
    /// 加签类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_APPROVER_TYPE")]
    public string? ApproverType { get; set; }
}
