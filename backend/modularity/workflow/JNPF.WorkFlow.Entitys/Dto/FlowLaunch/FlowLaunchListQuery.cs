using JNPF.AspNetCore;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.WorkFlow.Entitys.Dto.FlowLaunch;

[SuppressSniffer]
public class FlowLaunchListQuery : PageInputBase
{
    /// <summary>
    /// 所属分类.
    /// </summary>
    public string? flowCategory { get; set; }

    /// <summary>
    /// 所属流程.
    /// </summary>
    public string? templateId { get; set; }

    /// <summary>
    /// 所属名称.
    /// </summary>
    public string? flowId { get; set; }

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
    /// 流程状态.
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? flowUrgent { get; set; }

    /// <summary>
    /// 是否委托发起.
    /// </summary>
    public string delegateType { get; set; }
}
