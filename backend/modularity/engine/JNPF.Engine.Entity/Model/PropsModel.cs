using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 配置选项模型.
/// </summary>
[SuppressSniffer]
public class PropsModel
{
    /// <summary>
    /// 配置选项.
    /// </summary>
    public PropsBeanModel props { get; set; }
}