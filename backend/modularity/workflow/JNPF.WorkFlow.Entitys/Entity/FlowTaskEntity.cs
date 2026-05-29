using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程任务.
/// </summary>
[SugarTable("FLOW_TASK")]
public class FlowTaskEntity : CLDSEntityBase
{
    /// <summary>
    /// 父级id.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 实例进程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROCESS_ID")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// 任务编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 任务标题.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_URGENT")]
    public int? FlowUrgent { get; set; }

    /// <summary>
    /// 流程主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }

    /// <summary>
    /// 流程编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_CODE")]
    public string? FlowCode { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_NAME")]
    public string? FlowName { get; set; }

    /// <summary>
    /// 流程类型（0：发起流程，1：功能流程）.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_TYPE")]
    public int? FlowType { get; set; }

    /// <summary>
    /// 流程分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_CATEGORY")]
    public string? FlowCategory { get; set; }

    /// <summary>
    /// 表单内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_FORM_DATA_JSON")]
    public string? FlowFormContentJson { get; set; }

    /// <summary>
    /// 流程模板.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_TEMPLATE_JSON")]
    public string FlowTemplateJson { get; set; }

    /// <summary>
    /// 流程版本.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_VERSION")]
    public string? FlowVersion { get; set; }

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
    /// 当前节点.
    /// </summary>
    [SugarColumn(ColumnName = "F_CURRENT_NODE_NAME")]
    public string? ThisStep { get; set; }

    /// <summary>
    /// 当前节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_CURRENT_NODE_CODE")]
    public string? ThisStepId { get; set; }

    /// <summary>
    /// 是否能恢复（0：能，1：不能）.
    /// </summary>
    [SugarColumn(ColumnName = "F_RESTORE")]
    public int? Restore { get; set; }

    /// <summary>
    /// 任务状态：【0-草稿、1-处理、2-通过、3-驳回、4-撤销、5-终止】.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int Status { get; set; } = 0;

    /// <summary>
    /// 完成情况(0:未完成，1:完成).
    /// </summary>
    [SugarColumn(ColumnName = "F_COMPLETION")]
    public int? Completion { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 同步异步（0：同步，1：异步）.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_ASYNC")]
    public int? IsAsync { get; set; }

    /// <summary>
    /// 是否批量（0：否，1：是）.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_BATCH")]
    public int? IsBatch { get; set; }

    /// <summary>
    /// 复活节点主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_REVIVE_NODE_ID")]
    public string? ReviveNodeId { get; set; }

    /// <summary>
    /// 流程主表主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_ID")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// 拒绝节点id(当前节点审批).
    /// </summary>
    [SugarColumn(ColumnName = "F_REJECT_DATA_ID")]
    public string? RejectDataId { get; set; }

    /// <summary>
    /// 委托发起人.
    /// </summary>
    [SugarColumn(ColumnName = "F_DELEGATE_USER_ID")]
    public string? DelegateUserId { get; set; }

    /// <summary>
    /// 挂起（0：否，1：是）.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUSPEND")]
    public int? Suspend { get; set; }

}
