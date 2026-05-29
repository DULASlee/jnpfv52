using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.VisualData.Entity;

/// <summary>
/// 全局变量表.
/// </summary>
[SugarTable("BLADE_VISUAL_GLOB")]
public class VisualGlobalEntity : ITenantFilter
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "ID", ColumnDescription = "主键", IsPrimaryKey = true)]
    public string Id { get; set; }

    /// <summary>
    /// 分类键值.
    /// </summary>
    [SugarColumn(ColumnName = "GLOBALNAME", ColumnDescription = "变量名称")]
    public string GlobalName { get; set; }

    /// <summary>
    /// 分类键值.
    /// </summary>
    [SugarColumn(ColumnName = "GLOBALKEY", ColumnDescription = "变量Key")]
    public string GlobalKey { get; set; }

    /// <summary>
    /// 分类键值.
    /// </summary>
    [SugarColumn(ColumnName = "GLOBALVALUE", ColumnDescription = "变量值")]
    public string GlobalValue { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    public string TenantId { get; set; }
}
