namespace JNPF.Runtime.Core;

/// <summary>
/// Execution 执行结果（不可变）。
/// </summary>
public sealed class ExecutionResult
{
    /// <summary>
    /// Execution ID。
    /// </summary>
    public ExecutionId ExecutionId { get; }

    /// <summary>
    /// 执行状态。
    /// </summary>
    public ExecutionState State { get; }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool IsSuccess => State == ExecutionState.Completed;

    /// <summary>
    /// 是否失败。
    /// </summary>
    public bool IsFailure => State == ExecutionState.Failed;

    /// <summary>
    /// 是否被取消。
    /// </summary>
    public bool IsCancelled => State == ExecutionState.Cancelled;

    /// <summary>
    /// 失败原因。
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// 异常信息。
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// 完成时间。
    /// </summary>
    public DateTime CompletedAtUtc { get; }

    /// <summary>
    /// 执行时长。
    /// </summary>
    public TimeSpan Duration { get; }

    private ExecutionResult(
        ExecutionId executionId,
        ExecutionState state,
        string? failureReason,
        Exception? exception,
        DateTime completedAtUtc,
        TimeSpan duration)
    {
        ExecutionId = executionId;
        State = state;
        FailureReason = failureReason;
        Exception = exception;
        CompletedAtUtc = completedAtUtc;
        Duration = duration;
    }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    public static ExecutionResult Success(ExecutionId id, TimeSpan duration) =>
        new(id, ExecutionState.Completed, null, null, DateTime.UtcNow, duration);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    public static ExecutionResult Failure(ExecutionId id, string reason, Exception? ex, TimeSpan duration) =>
        new(id, ExecutionState.Failed, reason, ex, DateTime.UtcNow, duration);

    /// <summary>
    /// 是否被拒绝。
    /// </summary>
    public bool IsRejected => State == ExecutionState.Rejected;

    /// <summary>
    /// 创建取消结果。
    /// </summary>
    public static ExecutionResult Cancelled(ExecutionId id, TimeSpan duration) =>
        new(id, ExecutionState.Cancelled, null, null, DateTime.UtcNow, duration);

    /// <summary>
    /// 创建拒绝结果（Admission 失败）。
    /// </summary>
    public static ExecutionResult Rejected(ExecutionId id, string reason) =>
        new(id, ExecutionState.Rejected, reason, null, DateTime.UtcNow, TimeSpan.Zero);
}
