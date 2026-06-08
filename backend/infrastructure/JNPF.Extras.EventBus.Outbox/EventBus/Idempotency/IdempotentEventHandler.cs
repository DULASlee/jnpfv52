using JNPF.EventBus;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.Extras.EventBus.Idempotency;

/// <summary>
/// 幂等事件执行器装饰器。
/// 执行前检查 ProcessedEvent 表，已处理则跳过；执行后写入记录。
/// </summary>
public class IdempotentEventHandler : IEventHandlerExecutor
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<IdempotentEventHandler> _logger;

    public IdempotentEventHandler(ISqlSugarClient db, ILogger<IdempotentEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler)
    {
        var eventId = context.Source?.EventId;
        var handlerName = context.HandlerMethod?.Name ?? "Unknown";

        if (string.IsNullOrEmpty(eventId))
        {
            // 没有 EventId，直接执行（无法做幂等）
            await handler(context);
            return;
        }

        // 检查是否已处理
        var existing = await _db.Queryable<ProcessedEvent>()
            .Where(it => it.EventId == eventId && it.HandlerName == handlerName)
            .FirstAsync();

        if (existing != null)
        {
            _logger.LogDebug("Event {EventId} already processed by {Handler}, skipping", eventId, handlerName);
            return;
        }

        // 执行处理器
        await handler(context);

        // 记录已处理
        try
        {
            await _db.Insertable(new ProcessedEvent
            {
                EventId = eventId,
                HandlerName = handlerName
            }).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            // 并发插入可能冲突（主键重复），忽略
            _logger.LogDebug(ex, "Failed to record processed event {EventId} (may be duplicate)", eventId);
        }
    }
}
