using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DbLink;

/// <summary>
/// 数据连接下拉框输出.
/// </summary>
[SuppressSniffer]
public class DbLinkSelectorOutput : TreeModel
{
    /// <summary>
    /// 数据库类型.
    /// </summary>
    public string dbType { get; set; }

    /// <summary>
    /// 库名.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}
