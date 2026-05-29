using JNPF.DependencyInjection;

namespace JNPF.Message.Entitys.Dto.Message;

/// <summary>
/// 消息列表输出.
/// </summary>
[SuppressSniffer]
public class MessageListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 正文内容.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 发送人员.
    /// </summary>
    public string releaseUser { get; set; }

    /// <summary>
    /// 发送时间.
    /// </summary>
    public DateTime? releaseTime { get; set; }

    /// <summary>
    /// 是否已读(0-未读，1-已读).
    /// </summary>
    public int? isRead { get; set; }

    /// <summary>
    /// 流程消息类型(1-审批，2-委托).
    /// </summary>
    public int? flowType { get; set; }
}