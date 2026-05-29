using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 复杂表头模型.
/// </summary>
[SuppressSniffer]
public class ComplexHeaderModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 表头列名.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 子列.
    /// </summary>
    public List<string> childColumns { get; set; }

    /// <summary>
    /// 对齐方式.
    /// </summary>
    public string align { get; set; }
}
