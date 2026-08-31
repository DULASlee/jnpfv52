namespace JNPF.Runtime.Core;

/// <summary>
/// Execution 的只读描述符。
/// </summary>
public sealed record ExecutionDescriptor(
    ExecutionId Id,
    Guid SessionId,
    DateTime CreatedAtUtc,
    ExecutionState State)
{
    /// <summary>
    /// 创建新的 ExecutionDescriptor。
    /// </summary>
    public static ExecutionDescriptor Create(ExecutionId id, Guid sessionId) =>
        new(id, sessionId, DateTime.UtcNow, ExecutionState.Pending);
}
