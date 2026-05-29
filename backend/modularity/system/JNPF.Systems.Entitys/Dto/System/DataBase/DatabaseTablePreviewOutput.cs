using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Database;

/// <summary>
/// 数据库表预览输出.
/// </summary>
[SuppressSniffer]
public class DatabaseTablePreviewOutput
{
    /// <summary>
    /// 数据表对应数据列表.
    /// </summary>
    public List<object> myProperty { get; set; }
}