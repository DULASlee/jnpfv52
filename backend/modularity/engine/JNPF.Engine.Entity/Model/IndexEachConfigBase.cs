using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 列表页各配置基类.
/// </summary>
[SuppressSniffer]
public class IndexEachConfigBase : FieldsModel
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string prop { get; set; }

    /// <summary>
    /// 列名.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// 控件KEY.
    /// </summary>
    public string jnpfKey { get; set; }
}