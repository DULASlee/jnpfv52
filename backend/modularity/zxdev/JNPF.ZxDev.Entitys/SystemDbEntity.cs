using JNPF.Common.CodeGenUpload;
using SqlSugar;

namespace JNPF.ZxDev.Entitys;

/// <summary>
/// 营业厅实体.
/// </summary>
[SugarTable("zx_system_db")]
public class SystemDbEntity
{


    [SugarColumn(ColumnName = "Id", IsPrimaryKey = true)]
    public string? Id { get; set; }

    /// <summary>
    /// .
    /// </summary>
    [SugarColumn(ColumnName = "filename")]
    public string? filename { get; set; }



}