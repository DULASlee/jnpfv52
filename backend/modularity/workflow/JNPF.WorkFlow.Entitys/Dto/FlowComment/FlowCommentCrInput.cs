using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowComment;

[SuppressSniffer]
public class FlowCommentCrInput
{
    /// <summary>
    /// 附件.
    /// </summary>
    public string? file { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    public string? image { get; set; }

    /// <summary>
    /// 任务id.
    /// </summary>
    public string? taskId { get; set; }

    /// <summary>
    /// 评论内容.
    /// </summary>
    public string? text { get; set; }
}

