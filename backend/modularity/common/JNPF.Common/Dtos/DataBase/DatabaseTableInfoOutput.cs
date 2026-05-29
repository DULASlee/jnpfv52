using JNPF.DependencyInjection;

namespace JNPF.Common.Dtos.DataBase;

/// <summary>
/// 数据库表信息输出.
/// </summary>
[SuppressSniffer]
public class DatabaseTableInfoOutput
{
    /// <summary>
    /// 表信息.
    /// </summary>
    public TableInfoOutput tableInfo { get; set; }

    /// <summary>
    /// 表字段.
    /// </summary>
    public List<TableFieldOutput> tableFieldList { get; set; }

    /// <summary>
    /// 表数据.
    /// </summary>
    public bool hasTableData { get; set; }
}