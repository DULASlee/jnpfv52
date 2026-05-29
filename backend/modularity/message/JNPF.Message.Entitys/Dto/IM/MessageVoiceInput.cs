using JNPF.DependencyInjection;

namespace JNPF.Message.Entitys.Dto.IM;

/// <summary>
/// 消息语言输入.
/// </summary>
[SuppressSniffer]
public class MessageVoiceInput
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 时长.
    /// </summary>
    public string length { get; set; }
}