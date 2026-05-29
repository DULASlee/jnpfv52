using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 系统功能按钮.
/// </summary>
[SugarTable("BASE_MODULE_BUTTON")]
public class ModuleButtonEntity : SystemCLDSEntityBase
{
    /// <summary>
    /// 按钮上级.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 按钮名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 按钮编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 按钮图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string Icon { get; set; }

    /// <summary>
    /// 请求地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_URL_ADDRESS")]
    public string UrlAddress { get; set; }

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
    /// 功能主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }
}