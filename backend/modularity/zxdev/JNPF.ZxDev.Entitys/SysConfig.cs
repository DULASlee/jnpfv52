using JNPF.Common.CodeGenUpload;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace JNPF.ZxDev.Entitys;

/// <summary>
/// 系统配置信息
/// </summary>
[SugarTable("zx_sys_config")]

public partial class SysConfig
{
    [SugarColumn(ColumnName = "Id", IsPrimaryKey = true)]
    [Key]
    public string? Id {get ; set; }
    public string KeyName { get; set; }
    public string? KeyValue { get; set; }

    [SugarColumn(ColumnName = "F_DELETE_MARK", ColumnDescription = "删除标志")]
    [System.ComponentModel.DataAnnotations.Schema.Column("F_DELETE_MARK")]
    public int? DeleteMark { get; set; }

    public int VersionNum { get; set; }

    public string Name { get; set; }

    public string Comment { get; set; }
    public string FormId { get; set; }

    public string UpdateBy { get; set; }
    public DateTime UpdateDate { get; set; }

    public int? SortCode { get; set; }

}