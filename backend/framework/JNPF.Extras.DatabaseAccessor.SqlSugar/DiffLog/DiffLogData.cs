namespace JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;

/// <summary>
/// DiffLog 数据载体。由 OnDiffLog AOP 回调填充，由 IDiffLogPublisher 发布。
/// </summary>
public class DiffLogData
{
    /// <summary>操作的表名</summary>
    public string TableName { get; set; }

    /// <summary>操作类型：Insert / Update / Delete</summary>
    public string OperationType { get; set; }

    /// <summary>变更前数据（Update/Delete 时有值）</summary>
    public Dictionary<string, object> BeforeData { get; set; }

    /// <summary>变更后数据（Insert/Update 时有值）</summary>
    public Dictionary<string, object> AfterData { get; set; }

    /// <summary>当前租户 ID</summary>
    public string TenantId { get; set; }

    /// <summary>分布式追踪 ID</summary>
    public string TraceId { get; set; }

    /// <summary>变更时间戳</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>操作人 ID（可选，从 HttpContext 或 TenantContext 获取）</summary>
    public string OperatorId { get; set; }
}
