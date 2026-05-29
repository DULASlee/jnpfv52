using JNPF.Common.Filter;

namespace JNPF.Message.Entitys.Dto.MessageTemplate;

public class MessageTemplateQuery : PageInputBase
{
    /// <summary>
    /// 开始时间.
    /// </summary>
    public string? templateType { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    public string? messageType { get; set; }

    /// <summary>
    /// 消息来源.
    /// </summary>
    public string? messageSource { get; set; }
}
