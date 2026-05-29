namespace JNPF.Extras.Thirdparty.Email;

/// <summary>
/// 邮件信息.
/// </summary>
public class MailInfo
{
    /// <summary>
    /// 消息标识符.
    /// </summary>
    public string UID { get; set; }

    /// <summary>
    /// 收件人邮箱.
    /// </summary>
    public string To { get; set; }

    /// <summary>
    /// 收件人名称
    /// </summary>
    public string ToName { get; set; }

    /// <summary>
    /// 抄送人.
    /// </summary>
    public string CC { get; set; }

    /// <summary>
    /// 抄送人名称.
    /// </summary>
    public string CCName { get; set; }

    /// <summary>
    /// 密送人.
    /// </summary>
    public string Bcc { get; set; }

    /// <summary>
    /// 密送人名称.
    /// </summary>
    public string BccName { get; set; }

    /// <summary>
    /// 消息主题.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// 文本格式.
    /// </summary>
    public string BodyText { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    public List<MailFileParameterInfo> Attachment { get; set; }

    /// <summary>
    /// 收件时间.
    /// </summary>
    public DateTime Date { get; set; }
}