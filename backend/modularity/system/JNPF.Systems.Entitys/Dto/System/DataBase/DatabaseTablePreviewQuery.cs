using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Database;

/// <summary>
/// 数据库表预览查询.
/// </summary>
[SuppressSniffer]
public class DatabaseTablePreviewQuery : PageInputBase
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }
}