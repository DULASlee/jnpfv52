using JNPF.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace JNPF.Extras.EventBus.Outbox;

/// <summary>
/// Outbox 消息调度器（后台服务）。
/// 从 Outbox 表获取 Pending 消息，通过 EventBus 投递。
/// 支持 Channel 信号唤醒 + 30 秒兜底轮询。
/// </summary>
public class EventOutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventOutboxDispatcher> _logger;
    private readonly Channel<EventOutboxMessage> _channel;
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    public EventOutboxDispatcher(
        IServiceProvider serviceProvider,
        ILogger<EventOutboxDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateBounded<EventOutboxMessage>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    /// <summary>
    /// 通知有新消息写入 Outbox（由业务代码调用）。
    /// </summary>
    public void NotifyNewMessage()
    {
        _channel.Writer.TryWrite(new EventOutboxMessage());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventOutboxDispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 等待信号或超时
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(PollInterval);

                try
                {
                    await _channel.Reader.ReadAsync(cts.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // 超时，继续轮询
                }

                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventOutboxDispatcher error.");
                await Task.Delay(5000, stoppingToken);
            }
        }

        // 优雅停机：排空 Channel
        await DrainChannelAsync();
        _logger.LogInformation("EventOutboxDispatcher stopped.");
    }

    private async Task ProcessPendingAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<SqlSugarEventOutboxStore>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var messages = await store.GetPendingAsync(BatchSize);
        foreach (var msg in messages)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await store.MarkProcessingAsync(msg.Id);

                // 通过 EventBus 发布事件
                await publisher.PublishAsync(msg.EventName, msg.EventPayload);

                await store.MarkCompletedAsync(msg.Id);
                _logger.LogDebug("Outbox message {Id} delivered: {EventName}", msg.Id, msg.EventName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Outbox message {Id} delivery failed: {EventName}", msg.Id, msg.EventName);
                await store.IncrementRetryAsync(msg.Id);
                await store.MarkFailedAsync(msg.Id, ex.Message);
            }
        }
    }

    private async Task DrainChannelAsync()
    {
        _channel.Writer.TryComplete();
        while (await _channel.Reader.WaitToReadAsync())
        {
            while (_channel.Reader.TryRead(out _)) { }
        }
    }
}
