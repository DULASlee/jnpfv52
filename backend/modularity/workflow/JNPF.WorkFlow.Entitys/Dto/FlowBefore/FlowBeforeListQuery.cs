using JNPF.AspNetCore;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.WorkFlow.Entitys.Dto.FlowBefore;

[SuppressSniffer]
public class FlowBeforeListQuery : PageInputBase
{
    /// <summary>
    /// 开始时间.
    /// </summary>
    [ModelBinder(BinderType = typeof(TimestampToDateTimeModelBinder))]
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [ModelBinder(BinderType = typeof(TimestampToDateTimeModelBinder))]
    public DateTime? endTime { get; set; }

    /// <summary>
    /// 所属流程.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 所属流程.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 引擎分类.
    /// </summary>
    public string? flowCategory { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string? creatorUserId { get; set; }

    /// <summary>
    /// 节点id.
    /// </summary>
    public string? nodeCode { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? flowUrgent { get; set; }
}

