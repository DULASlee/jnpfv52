using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程引擎.
/// </summary>
[SugarTable("FLOW_TEMPLATE")]
[Tenant(ClaimConst.TENANTID)]
public class FlowTemplateEntity : CLDSEntityBase
{
    /// <summary>
    /// 流程编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 流程类型（0：发起流程，1：功能流程）.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 流程分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string? Category { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string? Icon { get; set; }

    /// <summary>
    /// 图标背景色.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON_BACKGROUND")]
    public string? IconBackground { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
