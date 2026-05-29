using JNPF.Common.Filter;

namespace JNPF.Message.Entitys.Dto.MessageAccount;

public class MessageAccountQuery : PageInputBase
{
    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// webhook类型.
    /// </summary>
    public string? webhookType { get; set; }

    /// <summary>
    /// 账号类型.
    /// </summary>
    public string? type { get; set; }

    /// <summary>
    /// 渠道.
    /// </summary>
    public string? channel { get; set; }
}
