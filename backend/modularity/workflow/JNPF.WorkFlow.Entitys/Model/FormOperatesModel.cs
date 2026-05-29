using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model;

[SuppressSniffer]
public class FormOperatesModel
{
    /// <summary>
    /// 可读.
    /// </summary>
    public bool read { get; set; }

    /// <summary>
    /// 控件名.
    /// </summary>
    public string? name { get; set; }

    /// <summary>
    /// 控件id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 可写.
    /// </summary>
    public bool write { get; set; }

    /// <summary>
    /// 必填.
    /// </summary>
    public bool required { get; set; }

    /// <summary>
    /// 控件.
    /// </summary>
    public string? jnpfKey { get; set; }
}
