using SqlSugar;

namespace JNPF.Extras.EventBus.Outbox;

/// <summary>
/// Outbox 消息实体。事件写入此表后由 Dispatcher 异步投递。
/// 表名：SYS_EVENT_OUTBOX_MESSAGE
/// </summary>
[SugarTable("SYS_EVENT_OUTBOX_MESSAGE")]
public class EventOutboxMessage
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_ID")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>事件名称（如 "Log:CreateReLog", "DiffLog:DataChanged"）</summary>
    [SugarColumn(ColumnName = "F_EVENT_NAME")]
    public string EventName { get; set; } = string.Empty;

    /// <summary>事件负载 JSON</summary>
    [SugarColumn(ColumnName = "F_EVENT_PAYLOAD", ColumnDataType = "text")]
    public string EventPayload { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    [SugarColumn(ColumnName = "F_CREATED_AT")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最后处理时间</summary>
    [SugarColumn(ColumnName = "F_PROCESSED_AT", IsNullable = true)]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>已重试次数</summary>
    [SugarColumn(ColumnName = "F_RETRY_COUNT")]
    public int RetryCount { get; set; }

    /// <summary>最大重试次数</summary>
    [SugarColumn(ColumnName = "F_MAX_RETRY_COUNT")]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>状态：0=Pending, 1=Processing, 2=Completed, 3=Failed, 4=DeadLetter</summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int Status { get; set; }

    /// <summary>最后一次错误信息</summary>
    [SugarColumn(ColumnName = "F_ERROR", ColumnDataType = "text", IsNullable = true)]
    public string? Error { get; set; }
}

/// <summary>
/// Outbox 消息状态枚举。
/// </summary>
public static class OutboxStatus
{
    public const int Pending = 0;
    public const int Processing = 1;
    public const int Completed = 2;
    public const int Failed = 3;
    public const int DeadLetter = 4;
}
