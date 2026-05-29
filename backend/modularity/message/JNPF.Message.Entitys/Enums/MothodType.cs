namespace JNPF.Message.Entitys.Enums;

/// <summary>
/// WebSocket信息类型.
/// </summary>
public enum MothodType
{
    /// <summary>
    /// 建立连接.
    /// </summary>
    OnConnection,

    /// <summary>
    /// 发送消息.
    /// </summary>
    SendMessage,

    /// <summary>
    /// 更新阅读消息.
    /// </summary>
    UpdateReadMessage,

    /// <summary>
    /// 消息列表.
    /// </summary>
    MessageList,

    /// <summary>
    /// 健康检查.
    /// </summary>
    HeartCheck,
}