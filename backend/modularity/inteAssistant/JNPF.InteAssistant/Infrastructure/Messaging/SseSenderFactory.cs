// 文件：Infrastructure/Messaging/SseSenderFactory.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Messaging

using System.Threading.Channels;

namespace JNPF.InteAssistant.Infrastructure.Messaging;

/// <summary>SSE 发送器工厂实现</summary>
public sealed class SseSenderFactory : ISseSenderFactory
{
    public SseSender Create(string pipelineId, int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };
        var channel = Channel.CreateBounded<SseEvent>(options);
        return new SseSender(channel, pipelineId);
    }

    public SseSender Create(string pipelineId, Channel<SseEvent> channel)
    {
        return new SseSender(channel, pipelineId);
    }
}
