using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.Models;

/// <summary>
/// 实体类基类.
/// </summary>
public interface IZxSystemFilter
{
    /// <summary>
    /// 系统id.
    /// </summary>
    [SugarColumn(ColumnName = "F_ZX_SYSTEM_ID", ColumnDescription = "系统id")]
    string ZxSystemId { get; set; }

}