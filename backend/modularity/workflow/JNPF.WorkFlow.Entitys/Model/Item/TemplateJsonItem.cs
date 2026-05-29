using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.Item;

[SuppressSniffer]
public class TemplateJsonItem
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string? field { get; set; }

    /// <summary>
    /// 字段名.
    /// </summary>
    public string? fieldName { get; set; }

    /// <summary>
    /// 关联字段.
    /// </summary>
    public string? relationField { get; set; }

    /// <summary>
    /// 是否字表.
    /// </summary>
    public bool isSubTable { get; set; }
}
