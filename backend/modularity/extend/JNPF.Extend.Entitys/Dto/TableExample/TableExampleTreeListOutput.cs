using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.TableExample;

/// <summary>
/// 表格树形.
/// </summary>
[SuppressSniffer]
public class TableExampleTreeListOutput : TreeModel
{
    /// <summary>
    /// 
    /// </summary>
    public bool loaded { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool expanded { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, object> ht { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? text { get; set; }
}
