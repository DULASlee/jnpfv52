using JNPF.DependencyInjection;
using JNPF.Message.Entitys.Enums;
using System.Text.Json.Serialization;

namespace JNPF.Message.Entitys.Dto.IM;

/// <summary>
/// 消息基类类.
/// </summary>
[SuppressSniffer]
public class MessageBase
{
    /// <summary>
    /// 方法.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MessageSendType method { get; set; }
}