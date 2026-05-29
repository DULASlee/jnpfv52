using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程事件日志.
/// </summary>
[SugarTable("FLOW_EVENT_LOG")]
[Tenant(ClaimConst.TENANTID)]
public class FlowEventLogEntity : CLDEntityBase
{
    /// <summary>
    /// 节点id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_NODE_ID")]
    public string? TaskNodeId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 接口id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTERFACE_ID")]
    public string? InterfaceId { get; set; }

    /// <summary>
    /// 执行结果.
    /// </summary>
    [SugarColumn(ColumnName = "F_RESULT")]
    public string? Result { get; set; }
}
