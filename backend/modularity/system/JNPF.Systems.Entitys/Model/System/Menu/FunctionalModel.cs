using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.Menu;

/// <summary>
/// 功能模型.
/// </summary>
[SuppressSniffer]
public class FunctionalModel : FunctionalBase
{
    /// <summary>
    /// 功能类别【1-类别、2-页面】..
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 功能地址.
    /// </summary>
    public string UrlAddress { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// 系统Id.
    /// </summary>
    public string systemId { get; set; }
}