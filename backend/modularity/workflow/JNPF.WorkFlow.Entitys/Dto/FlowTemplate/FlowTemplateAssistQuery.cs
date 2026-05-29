using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowTemplate;
[SuppressSniffer]
public class FlowTemplateAssistQuery
{
    /// <summary>
    /// 协管人员.
    /// </summary>
    public List<string>? list { get; set; }

    /// <summary>
    /// 流程主表id.
    /// </summary>
    public string? templateId { get; set; }
}
