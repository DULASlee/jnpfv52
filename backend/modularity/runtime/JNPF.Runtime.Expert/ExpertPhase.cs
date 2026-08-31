namespace JNPF.Runtime.Expert;

/// <summary>
/// Expert 执行阶段。
/// 
/// Expert 生命周期必须显式存在：
/// - Created: 刚创建
/// - Analyzing: 分析中
/// - Planning: 规划中
/// - Executing: 执行中
/// - Validating: 验证中
/// - Repairing: 修复中
/// - Reviewing: 审查中
/// - Completed: 完成
/// - Failed: 失败
/// - Cancelled: 取消
/// - Rejected: 拒绝
/// </summary>
public enum ExpertPhase
{
    /// <summary>
    /// 刚创建。
    /// </summary>
    Created = 0,

    /// <summary>
    /// 分析中。
    /// </summary>
    Analyzing = 1,

    /// <summary>
    /// 规划中。
    /// </summary>
    Planning = 2,

    /// <summary>
    /// 执行中。
    /// </summary>
    Executing = 3,

    /// <summary>
    /// 验证中。
    /// </summary>
    Validating = 4,

    /// <summary>
    /// 修复中。
    /// </summary>
    Repairing = 5,

    /// <summary>
    /// 审查中。
    /// </summary>
    Reviewing = 6,

    /// <summary>
    /// 已完成。
    /// </summary>
    Completed = 7,

    /// <summary>
    /// 失败。
    /// </summary>
    Failed = 8,

    /// <summary>
    /// 取消。
    /// </summary>
    Cancelled = 9,

    /// <summary>
    /// 拒绝。
    /// </summary>
    Rejected = 10
}

/// <summary>
/// Expert 任务状态。
/// </summary>
public enum ExpertTaskStatus
{
    /// <summary>
    /// 等待中。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 运行中。
    /// </summary>
    Running = 1,

    /// <summary>
    /// 成功。
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// 失败。
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 取消。
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// 拒绝。
    /// </summary>
    Rejected = 5
}
