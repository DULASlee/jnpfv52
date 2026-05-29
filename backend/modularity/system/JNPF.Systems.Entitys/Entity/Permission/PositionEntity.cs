using JNPF.Common.Contracts;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 岗位信息基类.
/// </summary>
[SugarTable("BASE_POSITION")]
public class PositionEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 获取或设置 角色名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME", ColumnDescription = "角色名称")]
    public string FullName { get; set; }

    /// <summary>
    /// 获取或设置 角色编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE", ColumnDescription = "角色编号")]
    public string EnCode { get; set; }

    /// <summary>
    /// 获取或设置 角色类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE", ColumnDescription = "角色类型")]
    public string Type { get; set; }

    /// <summary>
    /// 获取或设置 扩展属性.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON", ColumnDescription = "扩展属性")]
    public string PropertyJson { get; set; }

    /// <summary>
    /// 获取或设置 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION", ColumnDescription = "描述")]
    public string Description { get; set; }

    /// <summary>
    /// 机构主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORGANIZE_ID", ColumnDescription = "机构主键")]
    public string OrganizeId { get; set; }
}