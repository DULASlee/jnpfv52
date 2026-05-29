using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程引擎.
/// </summary>
[SugarTable("FLOW_TEMPLATE_JSON")]
[Tenant(ClaimConst.TENANTID)]
public class FlowTemplateJsonEntity : CLDSEntityBase
{
    /// <summary>
    /// 流程编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_ID")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// 可见类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_VISIBLE_TYPE")]
    public int? VisibleType { get; set; }

    /// <summary>
    /// 流程版本.
    /// </summary>
    [SugarColumn(ColumnName = "F_VERSION")]
    public string? Version { get; set; }

    /// <summary>
    /// 流程模板.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_TEMPLATE_JSON")]
    public string? FlowTemplateJson { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 分组id.
    /// </summary>
    [SugarColumn(ColumnName = "F_GROUP_ID")]
    public string? GroupId { get; set; }

    /// <summary>
    /// 消息配置id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SEND_CONFIG_IDS")]
    public string? SendConfigIds { get; set; }
}
