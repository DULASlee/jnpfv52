using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Message.Entitys.Dto.Message;

/// <summary>
/// 消息列表查询输入.
/// </summary>
[SuppressSniffer]
public class MessageListQueryInput : PageInputBase
{
    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 是否已读(0：未读 ).
    /// </summary>
    public string? isRead { get; set; }
}