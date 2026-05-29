using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// 集成助手信息输出.
/// </summary>
[SuppressSniffer]
public class InteAssistantUpInput : InteAssistantCrInput
{
    public string id { get; set; }
}