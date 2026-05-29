namespace JNPF.Message.Entitys.Dto.MessageAccount;

public class EmailSendTestQuery : MessageAccountListOutput
{
    /// <summary>
    /// 内容.
    /// </summary>
    public string? testEmailContent { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string? testEmailTitle { get; set; }

    /// <summary>
    /// 接收人.
    /// </summary>
    public List<string>? testSendEmail { get; set; }
}
