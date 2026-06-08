namespace JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;

/// <summary>
/// DiffLog 收集器接口。
/// 在请求生命周期内收集 DataChange 事件，请求结束时由 IDiffLogPublisher 统一发布。
/// 阶段 1 注册为 DiffLogCollector（Scoped 列表实现）。
/// 阶段 5 通过 DiffLogPublishModule 将 Publisher 替换为 Outbox 版本。
/// </summary>
public interface IDiffLogCollector
{
    /// <summary>
    /// 收集一条 DiffLog 数据（由 OnDiffLog AOP 回调调用）
    /// </summary>
    void Collect(DiffLogData data);

    /// <summary>
    /// 获取已收集的数据并清空列表（由请求结束 ActionFilter 调用）
    /// </summary>
    IList<DiffLogData> GetAndClear();

    /// <summary>
    /// 是否有未发布的数据
    /// </summary>
    bool HasPendingData { get; }
}
