using JNPF.AspNetCore;
using JNPF.Common.Filter;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Message.Entitys.Dto.MessageMonitor;

public class MessageMonitorQuery : PageInputBase
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
    /// 消息类型.
    /// </summary>
    public string? messageType { get; set; }

    /// <summary>
    /// 消息来源.
    /// </summary>
    public string? messageSource { get; set; }
}
