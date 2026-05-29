using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程传阅.
/// </summary>
[SugarTable("FLOW_TASK_CIRCULATE")]
public class FlowTaskCirculateEntity : CLDEntityBase
{
    /// <summary>
    /// 对象类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_TYPE")]
    public string? ObjectType { get; set; }

    /// <summary>
    /// 对象主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID")]
    public string? ObjectId { get; set; }

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
    /// 节点主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_NODE_ID")]
    public string? TaskNodeId { get; set; }

    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }
}
