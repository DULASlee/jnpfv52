namespace JNPF.Runtime.Core;

/// <summary>
/// Section 8 Runtime 状态机 — 定义合法状态转换规则。
///
/// 状态转换图：
///   Created → Initialized → Running ↔ Paused → Completed
///                              ↘ Failed
///   (任意非 Disposed) ────────────────────────────→ Disposed
///
/// 约束：
///   - 单一职责：仅验证和执状态转换；
///   - 不包含 Intelligence/Workflow 概念；
///   - 非法转换抛出 InvalidOperationException。
/// </summary>
public static class RuntimeStateMachine
{
    private static readonly Dictionary<RuntimeState, HashSet<RuntimeState>> _transitions = new()
    {
        [RuntimeState.Created] = new HashSet<RuntimeState> { RuntimeState.Initialized, RuntimeState.Disposed },
        [RuntimeState.Initialized] = new HashSet<RuntimeState> { RuntimeState.Running, RuntimeState.Disposed },
        [RuntimeState.Running] = new HashSet<RuntimeState> { RuntimeState.Paused, RuntimeState.Completed, RuntimeState.Failed, RuntimeState.Disposed },
        [RuntimeState.Paused] = new HashSet<RuntimeState> { RuntimeState.Running, RuntimeState.Completed, RuntimeState.Failed, RuntimeState.Disposed },
        [RuntimeState.Completed] = new HashSet<RuntimeState> { RuntimeState.Disposed },
        [RuntimeState.Failed] = new HashSet<RuntimeState> { RuntimeState.Disposed },
        [RuntimeState.Disposed] = new HashSet<RuntimeState>(), // 终态，不可转换
    };

    /// <summary>
    /// 验证从当前状态到目标状态是否合法。
    /// </summary>
    /// <param name="current">当前状态。</param>
    /// <param name="target">目标状态。</param>
    /// <returns>是否合法。</returns>
    public static bool CanTransition(RuntimeState current, RuntimeState target)
    {
        if (_transitions.TryGetValue(current, out var allowed) && allowed.Contains(target))
            return true;

        // Disposed 可从任意非 Disposed 直接跳转（资源清理逃生口）
        return target == RuntimeState.Disposed && current != RuntimeState.Disposed;
    }

    /// <summary>
    /// 验证并执行状态转换。
    /// </summary>
    /// <param name="session">目标会话。</param>
    /// <param name="target">目标状态。</param>
    /// <param name="reason">变更原因。</param>
    /// <exception cref="InvalidOperationException">非法转换。</exception>
    public static void Transition(RuntimeSession session, RuntimeState target, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!CanTransition(session.State, target))
        {
            throw new InvalidOperationException(
                $"Invalid state transition from '{session.State}' to '{target}' for session '{session.SessionId}'.");
        }

        session.TransitionTo(target, reason);
    }
}
