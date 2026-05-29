using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程表单关系表.
/// </summary>
[SugarTable("FLOW_FORM_RELATION")]
[Tenant(ClaimConst.TENANTID)]
public class FlowFormRelationEntity : CLDEntityBase
{
    /// <summary>
    /// 表单id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_ID")]
    public string? FormId { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }
}
