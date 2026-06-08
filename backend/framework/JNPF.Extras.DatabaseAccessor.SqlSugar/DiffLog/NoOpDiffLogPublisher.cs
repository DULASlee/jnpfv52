namespace JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;

/// <summary>
/// 空操作发布器。阶段 1-4 使用。
/// 阶段 5 上线后通过 DI 替换为 OutboxDiffLogPublisher。
/// </summary>
public class NoOpDiffLogPublisher : IDiffLogPublisher
{
    public Task PublishAsync(DiffLogData data)
    {
        // 空操作 — 数据在请求结束时被丢弃
        // 这是预期行为：阶段 1-4 Outbox 尚未就绪，无法可靠投递
        return Task.CompletedTask;
    }
}
