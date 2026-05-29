using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.Menu;

/// <summary>
/// 功能基础.
/// </summary>
[SuppressSniffer]
public class FunctionalBase
{
    /// <summary>
    /// 功能主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 功能上级.
    /// </summary>
    public string ParentId { get; set; }

    /// <summary>
    /// 功能名称.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// 按钮编码.
    /// </summary>
    public string EnCode { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? SortCode { get; set; }
}