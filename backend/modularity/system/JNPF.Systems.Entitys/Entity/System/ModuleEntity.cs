using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 系统功能.
/// </summary>
[SugarTable("BASE_MODULE")]
[Tenant(ClaimConst.TENANTID)]
public class ModuleEntity : SystemCLDSEntityBase
{
    /// <summary>
    /// 功能上级.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 功能类别：【1-类别、2-页面 3-功能】.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 功能名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 功能编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 功能地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_URL_ADDRESS")]
    public string UrlAddress { get; set; } = string.Empty;

    /// <summary>
    /// 按钮权限.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_BUTTON_AUTHORIZE")]
    public int? IsButtonAuthorize { get; set; }

    /// <summary>
    /// 列表权限.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_COLUMN_AUTHORIZE")]
    public int? IsColumnAuthorize { get; set; }

    /// <summary>
    /// 数据权限.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_DATA_AUTHORIZE")]
    public int? IsDataAuthorize { get; set; }

    /// <summary>
    /// 表单权限.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_FORM_AUTHORIZE")]
    public int? IsFormAuthorize { get; set; }

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
    /// 菜单分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 菜单图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string Icon { get; set; }

    /// <summary>
    /// 菜单图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_LINK_TARGET")]
    public string LinkTarget { get; set; }

    /// <summary>
    /// 功能设计Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 系统Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SYSTEM_ID")]
    public string SystemId { get; set; }
}