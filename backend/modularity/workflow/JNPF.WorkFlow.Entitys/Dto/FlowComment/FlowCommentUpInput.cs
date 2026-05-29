using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowComment
{
    [SuppressSniffer]
    public class FlowCommentUpInput : FlowCommentCrInput
    {
        /// <summary>
        /// id.
        /// </summary>
        public string? id { get; set; }
    }
}
