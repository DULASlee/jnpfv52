using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.Menu;

/// <summary>
/// 功能列表模型.
/// </summary>
[SuppressSniffer]
public class FunctionalViewModel : FunctionalBase
{

    /// <summary>
    /// 绑定表格.
    /// </summary>
    public string BindTable { get; set; }

    /// <summary>
    /// 绑定表格.
    /// </summary>
    public string BindTableName { get; set; }

    /// <summary>
    /// 功能主键.
    /// </summary>
    public string ModuleId { get; set; }
}