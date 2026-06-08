namespace JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;

/// <summary>
/// Outbox 版 DiffLog 发布器。
/// 将 DiffLog 数据写入 Outbox 表，由 EventOutboxDispatcher 异步投递。
/// 阶段 5 DiffLogPublishModule 通过 DI 替换 NoOpDiffLogPublisher。
/// </summary>
public class OutboxDiffLogPublisher : IDiffLogPublisher
{
    private readonly IEventOutboxStore _outboxStore;

    public OutboxDiffLogPublisher(IEventOutboxStore outboxStore)
    {
        _outboxStore = outboxStore;
    }

    public async Task PublishAsync(DiffLogData data)
    {
        await _outboxStore.WriteAsync("DiffLog:DataChanged", data);
    }
}
