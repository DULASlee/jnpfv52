using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowDelegete;

[SuppressSniffer]
public class FlowDelegeteUpInput : FlowDelegeteCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }
}

