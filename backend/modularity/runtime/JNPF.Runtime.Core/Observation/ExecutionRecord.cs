namespace JNPF.Runtime.Core.Observation;

/// <summary>
/// Execution 执行记录。
/// 
/// 用于追踪一次 Execution 的完整生命周期：
/// - 动作
/// - 输入/输出
/// - 开始/结束时间
/// - 成功/失败状态
/// - 异常信息
/// - 验证结果
/// </summary>
public sealed class ExecutionRecord
{
    /// <summary>
    /// 执行 ID。
    /// </summary>
    public ExecutionId ExecutionId { get; }

    /// <summary>
    /// 关联的会话 ID。
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// 任务 ID（如果有）。
    /// </summary>
    public string? TaskId { get; }

    /// <summary>
    /// Agent ID（如果有）。
    /// </summary>
    public string? AgentId { get; }

    /// <summary>
    /// 动作描述。
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// 输入描述（JSON 序列化）。
    /// </summary>
    public string? Input { get; }

    /// <summary>
    /// 输出描述（JSON 序列化）。
    /// </summary>
    public string? Output { get; }

    /// <summary>
    /// 开始时间（UTC）。
    /// </summary>
    public DateTime StartedAtUtc { get; }

    /// <summary>
    /// 结束时间（UTC）。
    /// </summary>
    public DateTime? CompletedAtUtc { get; }

    /// <summary>
    /// 执行时长。
    /// </summary>
    public TimeSpan Duration => CompletedAtUtc.HasValue 
        ? CompletedAtUtc.Value - StartedAtUtc 
        : TimeSpan.Zero;

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 是否失败。
    /// </summary>
    public bool IsFailure { get; }

    /// <summary>
    /// 失败原因。
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// 异常类型。
    /// </summary>
    public string? ExceptionType { get; }

    /// <summary>
    /// 异常消息。
    /// </summary>
    public string? ExceptionMessage { get; }

    /// <summary>
    /// 验证结果（如果有）。
    /// </summary>
    public ValidationResult? ValidationResult { get; }

    /// <summary>
    /// 生成的产物路径（如果有）。
    /// </summary>
    public IReadOnlyList<string> Artifacts { get; }

    private ExecutionRecord(
        ExecutionId executionId,
        Guid sessionId,
        string? taskId,
        string? agentId,
        string action,
        string? input,
        string? output,
        DateTime startedAtUtc,
        DateTime? completedAtUtc,
        bool isSuccess,
        bool isFailure,
        string? failureReason,
        string? exceptionType,
        string? exceptionMessage,
        ValidationResult? validationResult,
        IReadOnlyList<string> artifacts)
    {
        ExecutionId = executionId;
        SessionId = sessionId;
        TaskId = taskId;
        AgentId = agentId;
        Action = action;
        Input = input;
        Output = output;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        IsSuccess = isSuccess;
        IsFailure = isFailure;
        FailureReason = failureReason;
        ExceptionType = exceptionType;
        ExceptionMessage = exceptionMessage;
        ValidationResult = validationResult;
        Artifacts = artifacts ?? Array.Empty<string>();
    }

    /// <summary>
    /// 创建开始记录。
    /// </summary>
    public static ExecutionRecord Started(
        ExecutionId executionId,
        Guid sessionId,
        string action,
        string? taskId = null,
        string? agentId = null,
        string? input = null)
    {
        return new ExecutionRecord(
            executionId,
            sessionId,
            taskId,
            agentId,
            action,
            input,
            null,
            DateTime.UtcNow,
            null,
            false,
            false,
            null,
            null,
            null,
            null,
            null);
    }

    /// <summary>
    /// 创建成功完成记录。
    /// </summary>
    public ExecutionRecord Completed(string? output = null, ValidationResult? validationResult = null, params string[] artifacts)
    {
        return new ExecutionRecord(
            ExecutionId,
            SessionId,
            TaskId,
            AgentId,
            Action,
            Input,
            output,
            StartedAtUtc,
            DateTime.UtcNow,
            true,
            false,
            null,
            null,
            null,
            validationResult,
            artifacts);
    }

    /// <summary>
    /// 创建失败记录。
    /// </summary>
    public ExecutionRecord Failed(string reason, Exception? ex = null)
    {
        return new ExecutionRecord(
            ExecutionId,
            SessionId,
            TaskId,
            AgentId,
            Action,
            Input,
            null,
            StartedAtUtc,
            DateTime.UtcNow,
            false,
            true,
            reason,
            ex?.GetType().FullName,
            ex?.Message,
            null,
            Artifacts);
    }
}

/// <summary>
/// 验证结果。
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// 是否通过。
    /// </summary>
    public bool IsPassed { get; }

    /// <summary>
    /// 验证消息。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 验证详情。
    /// </summary>
    public IReadOnlyList<string> Details { get; }

    private ValidationResult(bool isPassed, string message, IReadOnlyList<string> details)
    {
        IsPassed = isPassed;
        Message = message;
        Details = details ?? Array.Empty<string>();
    }

    /// <summary>
    /// 通过。
    /// </summary>
    public static ValidationResult Pass(string message = "Validation passed", params string[] details) =>
        new(true, message, details);

    /// <summary>
    /// 失败。
    /// </summary>
    public static ValidationResult Fail(string message, params string[] details) =>
        new(false, message, details);
}
