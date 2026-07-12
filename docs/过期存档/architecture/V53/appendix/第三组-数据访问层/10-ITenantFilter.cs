using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.Models;

/// <summary>
/// 实体类基类.
/// </summary>
public interface ITenantFilter
{
    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    string TenantId { get; set; }
}