using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.Menu;

/// <summary>
/// 功能按钮模型.
/// </summary>
[SuppressSniffer]
public class FunctionalButtonModel : FunctionalBase
{
    /// <summary>
    /// 请求地址.
    /// </summary>
    public string UrlAddress { get; set; }

    /// <summary>
    /// 功能主键.
    /// </summary>
    public string ModuleId { get; set; }
}