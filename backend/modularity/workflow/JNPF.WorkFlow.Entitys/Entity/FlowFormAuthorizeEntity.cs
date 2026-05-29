using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程表单权限.
/// </summary>
[SugarTable("FLOW_FORM_AUTHORIZE")]
public class FlowFormAuthorizeEntity : CLDEntityBase
{
    /// <summary>
    /// 流程任务id.
    /// </summary>
    [SugarColumn(ColumnName = "F_Task_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }

    /// <summary>
    /// 字段权限.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_OPERATE")]
    public string? FormOperate { get; set; }
}
