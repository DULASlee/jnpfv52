using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程委托.
/// </summary>
[SugarTable("FLOW_DELEGATE")]
public class FlowDelegateEntity : CLDSEntityBase
{
    /// <summary>
    /// 被委托人id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TO_USER_ID")]
    public string? ToUserId { get; set; }

    /// <summary>
    /// 被委托人名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_TO_USER_NAME")]
    public string? ToUserName { get; set; }

    /// <summary>
    /// 委托流程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }

    /// <summary>
    /// 委托流程名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_NAME")]
    public string? FlowName { get; set; }

    /// <summary>
    /// 流程分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_CATEGORY")]
    public string? FlowCategory { get; set; }

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
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 委托类型(0:发起,1:审批).
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 委托人id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string? UserId { get; set; }

    /// <summary>
    /// 委托人名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_NAME")]
    public string? UserName { get; set; }
}
