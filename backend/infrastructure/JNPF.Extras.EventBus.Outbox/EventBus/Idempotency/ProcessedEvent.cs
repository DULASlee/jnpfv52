using SqlSugar;

namespace JNPF.Extras.EventBus.Idempotency;

/// <summary>
/// 已处理事件记录（幂等表）。
/// 表名：SYS_PROCESSED_EVENT
/// </summary>
[SugarTable("SYS_PROCESSED_EVENT")]
public class ProcessedEvent
{
    /// <summary>事件 ID</summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_EVENT_ID")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>处理器名称</summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_HANDLER_NAME")]
    public string HandlerName { get; set; } = string.Empty;

    /// <summary>处理完成时间</summary>
    [SugarColumn(ColumnName = "F_PROCESSED_AT")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
