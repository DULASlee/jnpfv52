// 文件：Infrastructure/Messaging/SseEvent.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Messaging

namespace JNPF.InteAssistant.Infrastructure.Messaging;

/// <summary>SSE 事件（Channel 消息单元）</summary>
public sealed record SseEvent(string Type, string? Data = null, string? Stage = null);
