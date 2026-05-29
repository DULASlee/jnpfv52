using JNPF.AspNetCore;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// 集成助手列表查询输入.
/// </summary>
[SuppressSniffer]
public class InteAssistantTaskListQueryInput : PageInputBase
{
    /// <summary>
    /// 集成id.
    /// </summary>
    public string? integrateId { get; set; }

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
    /// 执行结果
    /// 0-失败,1-成功.
    /// </summary>
    public int? resultType { get; set; }
}