using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 在线开发模型数据表模型.
/// </summary>
[SuppressSniffer]
public class TableModel : DbTableRelationModel
{
    /// <summary>
    /// 控件key.
    /// </summary>
    public string ControlKey { get; set; }

    /// <summary>
    /// 列字段.
    /// </summary>
    public List<EntityFieldModel> fields { get; set; }
}