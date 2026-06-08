namespace JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;

/// <summary>
/// 事件 Outbox 存储接口。
/// 阶段 5 任务 5.3 创建完整实现（SqlSugarEventOutboxStore）。
/// 此接口定义在框架层，避免跨层依赖。
/// </summary>
public interface IEventOutboxStore
{
    /// <summary>
    /// 将事件写入 Outbox 表（原子操作，与业务数据在同一事务）。
    /// </summary>
    Task WriteAsync(string eventName, object payload);
}
