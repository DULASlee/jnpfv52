using System.Collections.Concurrent;
using System.Threading.Channels;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Infrastructure.Messaging;

public interface IPipelineSseChannelHub
{
    Channel<SseEvent> ReplaceChannel(long pipelineId);
    void RemoveChannel(long pipelineId);
    bool TryGetChannel(long pipelineId, out Channel<SseEvent>? channel);
    bool TryPush(long pipelineId, string eventType, string data);
}

/// <summary>
/// 流水线 SSE 通道池——供 PipelineExecute 与 IR 观测台共享
/// </summary>
public sealed class PipelineSseChannelHub : IPipelineSseChannelHub, ISingleton
{
    private readonly ConcurrentDictionary<long, Channel<SseEvent>> _channels = new();
    private readonly ConcurrentDictionary<long, DateTime> _lastPush = new();
    private readonly Timer _orphanSweepTimer;

    public PipelineSseChannelHub()
    {
        _orphanSweepTimer = new Timer(_ => SweepOrphanChannels(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    private void SweepOrphanChannels()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-30);
        foreach (var kvp in _lastPush)
        {
            if (kvp.Value < cutoff)
                RemoveChannel(kvp.Key);
        }
    }

    public Channel<SseEvent> ReplaceChannel(long pipelineId)
    {
        RemoveChannel(pipelineId);
        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        });
        _channels[pipelineId] = channel;
        return channel;
    }

    public void RemoveChannel(long pipelineId)
    {
        _lastPush.TryRemove(pipelineId, out _);
        if (_channels.TryRemove(pipelineId, out var oldChannel))
            oldChannel.Writer.TryComplete();
    }

    public bool TryGetChannel(long pipelineId, out Channel<SseEvent>? channel)
        => _channels.TryGetValue(pipelineId, out channel);

    public bool TryPush(long pipelineId, string eventType, string data)
    {
        if (eventType is "queue_position" or "sandbox_queued")
        {
            if (_lastPush.TryGetValue(pipelineId, out var lastPush)
                && (DateTime.UtcNow - lastPush).TotalMilliseconds < 300)
            {
                return false;
            }

            _lastPush[pipelineId] = DateTime.UtcNow;
        }

        if (!_channels.TryGetValue(pipelineId, out var channel))
            return false;

        _lastPush[pipelineId] = DateTime.UtcNow;
        return channel.Writer.TryWrite(new SseEvent(eventType, data));
    }
}
