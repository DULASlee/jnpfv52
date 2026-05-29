using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程节点.
/// </summary>
[SugarTable("FLOW_TASK_NODE")]
[Tenant(ClaimConst.TENANTID)]
public class FlowTaskNodeEntity : CLDEntityBase
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
    /// 节点类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_TYPE")]
    public string? NodeType { get; set; }

    /// <summary>
    /// 节点属性Json.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_PROPERTY_JSON")]
    public string? NodePropertyJson { get; set; }

    /// <summary>
    /// 驳回节点(0:驳回发起，1：驳回指定或上一节点).
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_UP")]
    public string? NodeUp { get; set; }

    /// <summary>
    /// 下一节点.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_NEXT")]
    public string? NodeNext { get; set; }

    /// <summary>
    /// 是否完成：【0-未处理、1-已审核、-1-被驳回】.
    /// </summary>
    [SugarColumn(ColumnName = "F_COMPLETION")]
    public int? Completion { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    [SugarColumn(ColumnName = "F_SORT_CODE")]
    public long? SortCode { get; set; }

    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 节点状态（0：正常，1：禁用 -2：作废）.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATE")]
    public int? State { get; set; }

    /// <summary>
    /// 临时候选人.
    /// </summary>
    [SugarColumn(ColumnName = "F_CANDIDATES")]
    public string? Candidates { get; set; }

    /// <summary>
    /// 保存数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_DRAFT_DATA")]
    public string? DraftData { get; set; }

    /// <summary>
    /// 表单id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_ID")]
    public string? FormId { get; set; }
}
