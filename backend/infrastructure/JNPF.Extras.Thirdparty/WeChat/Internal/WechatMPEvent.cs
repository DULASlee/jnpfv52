namespace JNPF.Extras.Thirdparty.WeChat.Internal;

/// <summary>
/// 公众号事件.
/// </summary>
public class WechatMPEvent
{
    /// <summary>
    /// 签名.
    /// </summary>
    public string signature { get; set; }

    /// <summary>
    /// 时间戳.
    /// </summary>
    public string timestamp { get; set; }

    /// <summary>
    /// nonce.
    /// </summary>
    public string nonce { get; set; }

    /// <summary>
    /// 算出的值.
    /// </summary>
    public string echostr { get; set; }

    /// <summary>
    /// 开发者微信号.
    /// </summary>
    public string ToUserName { get; set; }

    /// <summary>
    /// 发送方帐号（一个OpenID）.
    /// </summary>
    public string FromUserName { get; set; }

    /// <summary>
    /// 消息创建时间 （整型）.
    /// </summary>
    public long CreateTime { get; set; }

    /// <summary>
    /// 消息类型，event.
    /// </summary>
    public string MsgType { get; set; }

    /// <summary>
    /// 事件类型，subscribe(订阅)、unsubscribe(取消订阅).
    /// </summary>
    public string Event { get; set; }
}
