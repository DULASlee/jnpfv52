using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 配置属性模型.
/// </summary>
[SuppressSniffer]
public class PropsBeanModel
{
    /// <summary>
    /// 指定选项标签为选项对象的某个属性值.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// 指定选项的值为选项对象的某个属性值.
    /// </summary>
    public string value { get; set; }

    /// <summary>
    /// 指定选项的子选项为选项对象的某个属性值.
    /// </summary>
    public string children { get; set; }
}