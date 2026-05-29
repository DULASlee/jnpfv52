using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.FlowForm;

[SuppressSniffer]
public class FlowFormListInput : PageInputBase
{
    /// <summary>
    /// 表单类型（1：系统表单，2：自定义表单）.
    /// </summary>
    public int? formType { get; set; }

    /// <summary>
    /// 发布状态（0：未发布，1：已发布，2：已修改）.
    /// </summary>
    public int? isRelease { get; set; }
}
