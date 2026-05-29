using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowTask;

[SuppressSniffer]
public class FlowTaskInfoOutput
{
    /// <summary>
    /// 引擎id.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? flowUrgent { get; set; } = 1;

    /// <summary>
    /// 表单数据.
    /// </summary>
    public string? data { get; set; }

    /// <summary>
    /// 主键id.
    /// </summary>
    public string? id { get; set; }
}
