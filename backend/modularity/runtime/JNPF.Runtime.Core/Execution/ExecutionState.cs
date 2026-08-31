namespace JNPF.Runtime.Core;

/// <summary>
/// Execution 生命周期状态。
/// 
/// 注意：此枚举独立于 RuntimeState，两者正交。
/// </summary>
public enum ExecutionState
{
    /// <summary>
    /// 已创建，等待执行。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 执行中。
    /// </summary>
    Running = 1,

    /// <summary>
    /// 正常完成。
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 执行失败。
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 被取消。
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// 被拒绝（Admission 阶段拒绝）。
    /// </summary>
    Rejected = 5
}
