namespace JNPF.Runtime.Core;

/// <summary>
/// ExecutionContext 工厂类。
/// 
/// 提供多种重载以适应不同场景：
/// - 基础创建（无 Mode）
/// - 带 ModeContext
/// - 带 ExecutionPolicy
/// - 完整上下文（TaskId + AgentId）
/// </summary>
public static class ExecutionContextFactory
{
    /// <summary>
    /// Creates an ExecutionContext without ModeContext (legacy/Phase 2-B compatible).
    /// </summary>
    public static ExecutionContext Create(Guid sessionId, IHookRegistry? hooks = null)
    {
        var id = ExecutionId.New();
        return new ExecutionContext(id, sessionId, hooks ?? new ExecutionHookRegistry(), null);
    }

    /// <summary>
    /// Creates an ExecutionContext with ModeContext.
    /// </summary>
    public static ExecutionContext Create(Guid sessionId, ModeContext modeContext, IHookRegistry? hooks = null)
    {
        ArgumentNullException.ThrowIfNull(modeContext);
        var id = ExecutionId.New();
        return new ExecutionContext(id, sessionId, hooks ?? new ExecutionHookRegistry(), modeContext);
    }

    /// <summary>
    /// Creates an ExecutionContext with ExecutionPolicy.
    /// </summary>
    public static ExecutionContext Create(Guid sessionId, ExecutionPolicy policy, IHookRegistry? hooks = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var id = ExecutionId.New();
        var modeContext = new ModeContext(0, "Unknown", policy);
        return new ExecutionContext(id, sessionId, hooks ?? new ExecutionHookRegistry(), modeContext);
    }

    /// <summary>
    /// Creates an ExecutionContext with full context including TaskId and AgentId.
    /// </summary>
    public static ExecutionContext CreateFull(
        Guid sessionId,
        ModeContext modeContext,
        string taskId,
        string agentId,
        IHookRegistry? hooks = null)
    {
        ArgumentNullException.ThrowIfNull(modeContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        var id = ExecutionId.New();
        return new ExecutionContext(id, sessionId, hooks ?? new ExecutionHookRegistry(), modeContext, taskId, agentId);
    }
}
