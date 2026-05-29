using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Database;

/// <summary>
/// 数据库表字段下拉框输出.
/// </summary>
[SuppressSniffer]
public class DatabaseTableFieldsSelectorOutput
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string fieldName { get; set; }
}