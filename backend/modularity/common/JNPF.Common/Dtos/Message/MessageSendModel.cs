namespace JNPF.Common.Dtos.Message;

public class MessageSendModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 接收人.
    /// </summary>
    public List<string> toUser { get; set; }

    /// <summary>
    /// 参数.
    /// </summary>
    public List<MessageSendParam> paramJson { get; set; }

    /// <summary>
    /// 模板名称.
    /// </summary>
    public string msgTemplateName { get; set; }

    /// <summary>
    /// 发送id.
    /// </summary>
    public string sendConfigId { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    public string messageType { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 账号id.
    /// </summary>
    public string accountConfigId { get; set; }
    /// <summary>
    /// 跳转链接
    /// </summary>
    public string gotoLink { get; set; }

    public Dictionary<string,string> linkParams { get; set; }
}
