using JNPF.Common.Const;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.VisualData.Entity;

/// <summary>
/// 可视化组件表.
/// </summary>
[SugarTable("BLADE_VISUAL_COMPONENT")]
[Tenant(ClaimConst.TENANTID)]
public class VisualComponentEntity : ITenantFilter
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "ID", ColumnDescription = "主键", IsPrimaryKey = true)]
    public string Id { get; set; }

    /// <summary>
    /// 组件名称.
    /// </summary>
    [SugarColumn(ColumnName = "NAME", ColumnDescription = "组件名称")]
    public string Name { get; set; }

    /// <summary>
    /// 组件内容.
    /// </summary>
    [SugarColumn(ColumnName = "CONTENT", ColumnDescription = "组件内容")]
    public string Content { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    [SugarColumn(ColumnName = "TYPE", ColumnDescription = "类型")]
    public int? Type { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    [SugarColumn(ColumnName = "IMG", ColumnDescription = "图片")]
    public string Img { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    public string TenantId { get; set; }
}
