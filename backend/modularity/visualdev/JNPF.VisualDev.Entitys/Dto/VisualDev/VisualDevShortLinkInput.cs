using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 功能表单外联信息输入.
/// </summary>
[SuppressSniffer]
public class VisualDevShortLinkInput
{
    public string encryption { get; set; }

    public string tenantId { get; set; }

    public string modelId { get; set; }

    public string type { get; set; }
}
