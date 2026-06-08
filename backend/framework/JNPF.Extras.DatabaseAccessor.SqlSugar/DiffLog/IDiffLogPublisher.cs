namespace JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;

/// <summary>
/// DiffLog 发布器接口。
/// 阶段 1-4：NoOpDiffLogPublisher（空操作，因为 Outbox 尚未就绪）。
/// 阶段 5：通过 DiffLogPublishModule 替换为 OutboxDiffLogPublisher。
/// </summary>
public interface IDiffLogPublisher
{
    Task PublishAsync(DiffLogData data);
}
