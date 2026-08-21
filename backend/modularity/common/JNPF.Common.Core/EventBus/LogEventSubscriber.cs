using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using JNPF.Systems.Entitys.System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Threading.Channels;

namespace JNPF.EventHandler;

/// <summary>
/// 日记事件订阅（Channel 批量缓冲版）.
/// 事件处理器写入 Channel，后台任务按租户分组批量写入数据库。
/// </summary>
public class LogEventSubscriber : IEventSubscriber, ISingleton, IHostedService
{
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly ITenantManager _tenantManager;
    private readonly ILogger<LogEventSubscriber> _logger;

    private readonly Channel<LogEventSource> _channel;
    private readonly CancellationTokenSource _cts = new();
    private Task? _flushTask;

    private const int ChannelCapacity = 1000;
    private const int BatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 构造函数.
    /// 战役 0.1.2：移除死注入 IUserManager（从未使用，且 Singleton 消费 Scoped 违规）.
    /// </summary>
    public LogEventSubscriber(
        ISqlSugarClient sqlSugarClient,
        ITenantManager tenantManager,
        ILogger<LogEventSubscriber> logger)
    {
        _sqlSugarClient = sqlSugarClient;
        _tenantManager = tenantManager;
        _logger = logger;

        _channel = Channel.CreateBounded<LogEventSource>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// 创建日记（写入 Channel，非阻塞）.
    /// </summary>
    [EventSubscribe("Log:CreateReLog")]
    [EventSubscribe("Log:CreateExLog")]
    [EventSubscribe("Log:CreateVisLog")]
    [EventSubscribe("Log:CreateOpLog")]
    public Task CreateLog(EventHandlerExecutingContext context)
    {
        var log = (LogEventSource)context.Source;

        if (!_channel.Writer.TryWrite(log))
        {
            _logger.LogWarning("Log buffer full, dropping log entry. TraceId={TraceId}", log.Entity.TraceId);
        }

        return Task.CompletedTask;
    }

    /// <summary>启动后台刷新任务.</summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _flushTask = Task.Run(() => FlushLoopAsync(_cts.Token), CancellationToken.None);
        _logger.LogInformation("LogEventSubscriber buffer started. Capacity={Capacity}, BatchSize={BatchSize}", ChannelCapacity, BatchSize);
        return Task.CompletedTask;
    }

    /// <summary>停止后台刷新任务，执行最终刷新.</summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();

        // 最终刷新
        await FlushRemainingAsync();

        if (_flushTask != null)
        {
            try { await _flushTask.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken); }
            catch { /* ignore on shutdown */ }
        }

        _logger.LogInformation("LogEventSubscriber buffer stopped.");
    }

    /// <summary>后台循环：每 5 秒或满 100 条触发刷新.</summary>
    private async Task FlushLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(FlushInterval, ct).ConfigureAwait(false);
                await DrainAndFlushAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in log flush loop.");
            }
        }
    }

    /// <summary>从 Channel 中读取所有可用消息并批量写入.</summary>
    private async Task DrainAndFlushAsync(CancellationToken ct)
    {
        var batch = new List<LogEventSource>(BatchSize);

        while (batch.Count < BatchSize && _channel.Reader.TryRead(out var item))
        {
            batch.Add(item);
        }

        if (batch.Count > 0)
        {
            await FlushBatchAsync(batch).ConfigureAwait(false);
        }
    }

    /// <summary>应用退出时刷新 Channel 中剩余的消息.</summary>
    private async Task FlushRemainingAsync()
    {
        var batch = new List<LogEventSource>();

        while (_channel.Reader.TryRead(out var item))
        {
            batch.Add(item);
        }

        if (batch.Count > 0)
        {
            _logger.LogInformation("Flushing {Count} remaining log entries on shutdown.", batch.Count);
            await FlushBatchAsync(batch).ConfigureAwait(false);
        }
    }

    /// <summary>按租户分组后批量写入数据库.</summary>
    private async Task FlushBatchAsync(List<LogEventSource> batch)
    {
        var groups = batch.GroupBy(x => x.TenantId ?? string.Empty);

        foreach (var group in groups)
        {
            try
            {
                var db = _sqlSugarClient.CopyNew();

                if (group.Key.IsNotEmptyOrNull())
                {
                    await _tenantManager.ChangTenant(db, group.Key);
                }

                var entities = group.Select(x => x.Entity).ToList();
                await db.Fastest<SysLogEntity>().BulkCopyAsync(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk insert failed for TenantId={TenantId}, Count={Count}", group.Key, group.Count());
            }
        }
    }
}
