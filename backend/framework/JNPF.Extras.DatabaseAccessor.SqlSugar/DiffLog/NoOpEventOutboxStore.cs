namespace JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;

/// <summary>
/// 空操作 Outbox 存储。阶段 5 任务 5.3 完成前使用。
/// SqlSugarEventOutboxStore 上线后通过 DI 替换。
/// </summary>
public class NoOpEventOutboxStore : IEventOutboxStore
{
    public Task WriteAsync(string eventName, object payload)
    {
        // 空操作 — 事件被丢弃，等 Outbox 表和 Dispatcher 就绪后替换
        return Task.CompletedTask;
    }
}
