namespace JNPF.Message.Entitys.Dto.MessageMonitor;

public class MessageMonitorListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    public string? messageType { get; set; }

    /// <summary>
    /// 消息来源.
    /// </summary>
    public string? messageSource { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    /// 发送时间.
    /// </summary>
    public DateTime? sendTime { get; set; }

    /// <summary>
    /// 接收人.
    /// </summary>
    public string? receiveUser { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public string? content { get; set; }
}
