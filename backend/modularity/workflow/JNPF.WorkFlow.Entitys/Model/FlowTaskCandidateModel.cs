using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model;

[SuppressSniffer]
public class FlowTaskCandidateModel
{
    /// <summary>
    /// 节点编码.
    /// </summary>
    public string? nodeId { get; set; }

    /// <summary>
    /// 节点名.
    /// </summary>
    public string? nodeName { get; set; }

    /// <summary>
    /// 是否候选人.
    /// </summary>
    public bool isCandidates { get; set; }

    /// <summary>
    /// 是否条件分支.
    /// </summary>
    public bool isBranchFlow { get; set; }

    /// <summary>
    /// 是否有候选人.
    /// </summary>
    public bool hasCandidates { get; set; }
}
