using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 配置模型.
/// </summary>
[SuppressSniffer]
public class OptionsModel
{
    /// <summary>
    /// id.
    /// </summary>
    public int? id { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public string value { get; set; }

    /// <summary>
    /// 标签.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// 子级.
    /// </summary>
    public List<OptionsModel> children { get; set; }
}