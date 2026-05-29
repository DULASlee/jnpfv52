using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 默认排序模型.
/// </summary>
[SuppressSniffer]
public class DefaultSortFieldModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// field.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// sort.
    /// </summary>
    public string sort { get; set; }
}
