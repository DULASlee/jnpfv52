namespace JNPF.Message.Entitys.Model.IM;

/// <summary>
/// IM中心未读信息.
/// </summary>
public class IMUnreadNumModel
{
    /// <summary>
    /// 发送者Id.
    /// </summary>
    public string sendUserId { get; set; }

    /// <summary>
    /// 接收者Id.
    /// </summary>
    public string receiveUserId { get; set; }

    /// <summary>
    /// 未读数量.
    /// </summary>
    public int unreadNum { get; set; }

    /// <summary>
    /// 默认消息.
    /// </summary>
    public string defaultMessage { get; set; }

    /// <summary>
    /// 默认消息类型.
    /// </summary>
    public string defaultMessageType { get; set; }

    /// <summary>
    /// 默认消息时间.
    /// </summary>
    public string defaultMessageTime { get; set; }
}