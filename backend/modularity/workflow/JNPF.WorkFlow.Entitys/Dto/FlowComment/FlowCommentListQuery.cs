using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowComment;

[SuppressSniffer]
public class FlowCommentListQuery : PageInputBase
{
    /// <summary>
    /// 任务id.
    /// </summary>
    public string? taskId { get; set; }
}

