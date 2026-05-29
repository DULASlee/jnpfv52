using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.Models;

/// <summary>
/// 实体类基类.
/// </summary>
public interface IZxDataTypeFilter
{
 
    [SugarColumn(ColumnName = "F_ZX_DATATYPE", ColumnDescription = "数据类型级别")]
    int ZxDataType { get; set; }

}