using JNPF.DependencyInjection;

namespace JNPF.Common.Dtos.DataBase;

/// <summary>
/// 表信息输出.
/// </summary>
[SuppressSniffer]
public class TableInfoOutput
{
    /// <summary>
    /// 旧表名称.
    /// </summary>
    public string table { get; set; }

    /// <summary>
    /// 新表名称.
    /// </summary>
    public string newTable { get; set; }

    /// <summary>
    /// 表说明.
    /// </summary>
    public string tableName { get; set; }
}