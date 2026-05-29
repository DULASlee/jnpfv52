namespace JNPF.Message.Entitys.Enums;

/// <summary>
/// WebSocket 发送信息类型.
/// </summary>
public enum MessageSendType
{
    /// <summary>
    /// 上线.
    /// </summary>
    online,

    /// <summary>
    /// 初始化消息.
    /// </summary>
    initMessage,

    /// <summary>
    /// 发送消息.
    /// </summary>
    sendMessage,

    /// <summary>
    /// 接收消息.
    /// </summary>
    receiveMessage,

    /// <summary>
    /// 消息列表.
    /// </summary>
    messageList,

    /// <summary>
    /// 退出.
    /// </summary>
    logout,

    /// <summary>
    /// 错误.
    /// </summary>
    error,

    /// <summary>
    /// 关闭 Socket.
    /// </summary>
    closeSocket,


}