using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 实时保存门户设置输入.
/// </summary>
[SuppressSniffer]
public class PortalSaveInput
{
    /// <summary>
    /// 表单JSON.
    /// </summary>
    public string formData { get; set; }

    /// <summary>
    /// 系统ID.
    /// </summary>
    public string systemId { get; set; }
}
