using JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;
using SqlSugar;
using System.Text.Json;

namespace JNPF.Extras.EventBus.Outbox;

/// <summary>
/// SqlSugar 实现的 Outbox 存储。
/// 使用 UPDLOCK+READPAST 行锁确保并发安全。
/// </summary>
public class SqlSugarEventOutboxStore : IEventOutboxStore
{
    private readonly ISqlSugarClient _db;

    public SqlSugarEventOutboxStore(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task WriteAsync(string eventName, object payload)
    {
        var message = new EventOutboxMessage
        {
            EventName = eventName,
            EventPayload = JsonSerializer.Serialize(payload),
            Status = OutboxStatus.Pending
        };
        await _db.Insertable(message).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取待处理消息（使用行锁防止并发消费）。
    /// </summary>
    public async Task<IList<EventOutboxMessage>> GetPendingAsync(int batchSize)
    {
        string sql;
        if (_db.CurrentConnectionConfig.DbType == SqlSugar.DbType.PostgreSQL)
        {
            // PostgreSQL: LIMIT + FOR UPDATE SKIP LOCKED 等价于 SQL Server 的 TOP + UPDLOCK/READPAST
            sql = @"SELECT * FROM SYS_EVENT_OUTBOX_MESSAGE
                     WHERE F_STATUS IN (0, 3) AND F_RETRY_COUNT < F_MAX_RETRY_COUNT
                     ORDER BY F_CREATED_AT
                     LIMIT @batchSize
                     FOR UPDATE SKIP LOCKED";
        }
        else
        {
            // SQL Server: 使用 UPDLOCK+READPAST 行锁确保并发安全
            sql = @"SELECT TOP(@batchSize) * FROM SYS_EVENT_OUTBOX_MESSAGE
                     WITH (UPDLOCK, READPAST)
                     WHERE F_STATUS IN (0, 3) AND F_RETRY_COUNT < F_MAX_RETRY_COUNT
                     ORDER BY F_CREATED_AT";
        }

        return await _db.Ado.SqlQueryAsync<EventOutboxMessage>(sql, new { batchSize });
    }

    public async Task MarkProcessingAsync(Guid id)
    {
        await _db.Updateable<EventOutboxMessage>()
            .SetColumns(it => it.Status == OutboxStatus.Processing)
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task MarkCompletedAsync(Guid id)
    {
        await _db.Updateable<EventOutboxMessage>()
            .SetColumns(it => new EventOutboxMessage
            {
                Status = OutboxStatus.Completed,
                ProcessedAt = DateTime.UtcNow
            })
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task MarkFailedAsync(Guid id, string error)
    {
        await _db.Updateable<EventOutboxMessage>()
            .SetColumns(it => new EventOutboxMessage
            {
                Status = OutboxStatus.Failed,
                Error = error,
                ProcessedAt = DateTime.UtcNow
            })
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();

        // 超过最大重试次数 → 标记死信
        var msg = await _db.Queryable<EventOutboxMessage>().InSingleAsync(id);
        if (msg != null && msg.RetryCount >= msg.MaxRetryCount)
        {
            await MarkDeadLetterAsync(id);
        }
    }

    public async Task IncrementRetryAsync(Guid id)
    {
        await _db.Updateable<EventOutboxMessage>()
            .SetColumns(it => new EventOutboxMessage
            {
                RetryCount = it.RetryCount + 1,
                Status = OutboxStatus.Pending
            })
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task MarkDeadLetterAsync(Guid id)
    {
        await _db.Updateable<EventOutboxMessage>()
            .SetColumns(it => it.Status == OutboxStatus.DeadLetter)
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task<IList<EventOutboxMessage>> GetDeadLettersAsync(int pageIndex = 1, int pageSize = 20)
    {
        return await _db.Queryable<EventOutboxMessage>()
            .Where(it => it.Status == OutboxStatus.DeadLetter)
            .OrderByDescending(it => it.CreatedAt)
            .ToPageListAsync(pageIndex, pageSize);
    }

    public async Task RetryDeadLetterAsync(Guid id)
    {
        await _db.Updateable<EventOutboxMessage>()
            .SetColumns(it => new EventOutboxMessage
            {
                Status = OutboxStatus.Pending,
                RetryCount = 0,
                Error = null
            })
            .Where(it => it.Id == id && it.Status == OutboxStatus.DeadLetter)
            .ExecuteCommandAsync();
    }

    public async Task<EventOutboxStats> GetStatsAsync()
    {
        var messages = await _db.Queryable<EventOutboxMessage>()
            .Where(it => it.Status != OutboxStatus.Completed)
            .ToListAsync();

        return new EventOutboxStats
        {
            PendingCount = messages.Count(it => it.Status == OutboxStatus.Pending),
            FailedCount = messages.Count(it => it.Status == OutboxStatus.Failed),
            DeadLetterCount = messages.Count(it => it.Status == OutboxStatus.DeadLetter)
        };
    }
}

/// <summary>
/// Outbox 统计数据。
/// </summary>
public class EventOutboxStats
{
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
}
