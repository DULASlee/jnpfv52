using JNPF.Common.Enums;
using SqlSugar;

namespace JNPF.Common.Contracts;

/// <summary>
/// 实体类基类.
/// </summary>
public interface IZxDataTypeEntity
{    

    [SugarColumn(ColumnName = "F_ZX_DATATYPE", ColumnDescription = "数据类型级别")]
    ZxDataTypeEnum ZxDataType { get; set; }
}