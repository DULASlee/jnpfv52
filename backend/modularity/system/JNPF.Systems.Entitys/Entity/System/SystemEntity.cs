using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 系统功能.
/// </summary>
[SugarTable("BASE_SYSTEM")]
public class SystemEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 系统名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 系统编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 系统图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 是否是主系统（0-不是，1-是）.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_MAIN")]
    public int? IsMain { get; set; }

    /// <summary>
    /// 扩展属性.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON")]
    public string PropertyJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 导航图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_NAVIGATION_ICON")]
    public string NavigationIcon { get; set; }

    /// <summary>
    /// logo图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_WORK_LOGO_ICON")]
    public string WorkLogoIcon { get; set; }

    /// <summary>
    /// 协同办公（0-关闭，1-开启）.
    /// </summary>
    [SugarColumn(ColumnName = "F_WORKFLOW_ENABLED")]
    public int? WorkflowEnabled { get; set; }


    [SugarColumn(ColumnName = "f_system_api")]
    public string SystemApi { get; set; }
}
