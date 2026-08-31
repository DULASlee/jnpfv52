namespace JNPF.Runtime.Core.Recovery;

/// <summary>
/// Execution 失败类型枚举。
/// 
/// 用于分类失败以便采取适当的恢复策略。
/// 禁止所有失败都使用相同的重试逻辑。
/// </summary>
public enum FailureType
{
    /// <summary>
    /// 未知类型。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 瞬时失败（网络、连接、超时等），可重试。
    /// </summary>
    Transient = 1,

    /// <summary>
    /// 永久性失败（逻辑错误、参数错误等），不可重试。
    /// </summary>
    Permanent = 2,

    /// <summary>
    /// 合同违规（接口契约被破坏）。
    /// </summary>
    ContractViolation = 3,

    /// <summary>
    /// 验证失败（测试未通过、断言失败等）。
    /// </summary>
    ValidationFailure = 4,

    /// <summary>
    /// 工具失败（外部工具调用失败）。
    /// </summary>
    ToolFailure = 5,

    /// <summary>
    /// 编译失败。
    /// </summary>
    CompilationFailure = 6,

    /// <summary>
    /// 测试失败。
    /// </summary>
    TestFailure = 7,

    /// <summary>
    /// 架构违规。
    /// </summary>
    ArchitectureViolation = 8,

    /// <summary>
    /// 资源不足。
    /// </summary>
    ResourceExhaustion = 9,

    /// <summary>
    /// 取消（用户主动取消或超时）。
    /// </summary>
    Cancelled = 10
}

/// <summary>
/// Execution 失败信息。
/// </summary>
public readonly struct FailureInfo
{
    /// <summary>
    /// 失败类型。
    /// </summary>
    public FailureType Type { get; }

    /// <summary>
    /// 失败原因。
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// 异常类型（如果有）。
    /// </summary>
    public string? ExceptionType { get; }

    /// <summary>
    /// 异常消息。
    /// </summary>
    public string? ExceptionMessage { get; }

    /// <summary>
    /// 是否可重试。
    /// </summary>
    public bool IsRetryable => Type switch
    {
        FailureType.Transient => true,
        FailureType.ToolFailure => true,
        FailureType.ResourceExhaustion => true,
        _ => false
    };

    /// <summary>
    /// 建议的最大重试次数。
    /// </summary>
    public int RecommendedMaxRetries => Type switch
    {
        FailureType.Transient => 3,
        FailureType.ToolFailure => 2,
        FailureType.ResourceExhaustion => 1,
        _ => 0
    };

    /// <summary>
    /// 建议的重试间隔（毫秒）。
    /// </summary>
    public int RecommendedRetryDelayMs => Type switch
    {
        FailureType.Transient => 1000,
        FailureType.ToolFailure => 2000,
        FailureType.ResourceExhaustion => 5000,
        _ => 0
    };

    public FailureInfo(FailureType type, string reason, Exception? ex = null)
    {
        Type = type;
        Reason = reason;
        ExceptionType = ex?.GetType().FullName;
        ExceptionMessage = ex?.Message;
    }

    /// <summary>
    /// 从异常推断失败类型。
    /// </summary>
    public static FailureInfo FromException(Exception ex)
    {
        var type = ex switch
        {
            TimeoutException => FailureType.Transient,
            OperationCanceledException => FailureType.Cancelled,
            InvalidOperationException => FailureType.ContractViolation,
            ArgumentException => FailureType.Permanent,
            DivideByZeroException => FailureType.Permanent,
            NullReferenceException => FailureType.Permanent,
            _ => FailureType.Unknown
        };

        return new FailureInfo(type, ex.Message, ex);
    }

    /// <summary>
    /// 从编译错误推断失败类型。
    /// </summary>
    public static FailureInfo FromCompilationError(string errorMessage)
    {
        return new FailureInfo(FailureType.CompilationFailure, errorMessage);
    }

    /// <summary>
    /// 从测试失败推断失败类型。
    /// </summary>
    public static FailureInfo FromTestFailure(string testName, string message)
    {
        return new FailureInfo(FailureType.TestFailure, $"Test '{testName}' failed: {message}");
    }
}
