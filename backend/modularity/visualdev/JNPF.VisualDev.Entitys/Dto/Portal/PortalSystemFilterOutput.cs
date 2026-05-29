using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 门户获取系统下拉输出.
/// </summary>
[SuppressSniffer]
public class PortalSystemFilterOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    /// <returns></returns>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 是否禁用.
    /// </summary>
    public bool disabled { get; set; }
}
