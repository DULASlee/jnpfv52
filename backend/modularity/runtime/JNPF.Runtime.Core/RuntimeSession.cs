namespace JNPF.Runtime.Core;

/// <summary>
/// Section 8 Runtime 会话 (Session) — RuntimeContext 的运行时实例。
///
/// 约束：
///   - 持有 RuntimeContext 引用；
///   - 持有当前 RuntimeState；
///   - 不包含 Intelligence/Workflow 概念。
/// </summary>
public sealed class RuntimeSession
{
    /// <summary>
    /// 会话唯一标识。
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// 会话关联的运行时上下文。
    /// </summary>
    public RuntimeContext Context { get; }

    /// <summary>
    /// 当前状态。
    /// </summary>
    public RuntimeState State { get; private set; }

    /// <summary>
    /// 状态变更时间（UTC）。
    /// </summary>
    public DateTime StateChangedAtUtc { get; private set; }

    /// <summary>
    /// 状态变更原因（可选）。
    /// </summary>
    public string? StateReason { get; private set; }

    /// <summary>
    /// 当前 Mode 上下文（如果已设置）。
    /// </summary>
    public ModeContext? ModeContext { get; internal set; }

    internal RuntimeSession(RuntimeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SessionId = Guid.NewGuid();
        Context = context;
        State = RuntimeState.Created;
        StateChangedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// 内部方法：更新状态（由 RuntimeStateMachine 调用）。
    /// </summary>
    internal void TransitionTo(RuntimeState newState, string? reason = null)
    {
        State = newState;
        StateChangedAtUtc = DateTime.UtcNow;
        StateReason = reason;
    }
}
