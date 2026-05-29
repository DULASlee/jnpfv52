using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DbLink;

/// <summary>
/// 数据连接列表查询输入.
/// </summary>
[SuppressSniffer]
public class DbLinkListInput : PageInputBase
{
    /// <summary>
    /// 分类.
    /// </summary>
    public string dbType { get; set; }
}
