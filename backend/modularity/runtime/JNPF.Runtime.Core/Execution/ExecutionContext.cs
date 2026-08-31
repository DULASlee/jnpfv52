namespace JNPF.Runtime.Core;

/// <summary>
/// Section 8 Execution Context - 统一执行上下文。
/// 
/// 设计约束：
/// - 不可变核心：ExecutionId, SessionId, ModeContext 在创建后不可变
/// - 线程安全：公共成员均为不可变或线程安全
/// - M17 兼容：不得直接引用 Expert 实现
/// </summary>
public sealed class ExecutionContext : IDisposable
{
    /// <summary>
    /// 唯一执行标识。
    /// </summary>
    public ExecutionId Id { get; }

    /// <summary>
    /// 关联的会话 ID。
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// 任务标识（可选，用于关联任务系统）。
    /// </summary>
    public string? TaskId { get; }

    /// <summary>
    /// Agent 标识（可选，用于标识执行 Agent）。
    /// </summary>
    public string? AgentId { get; }

    /// <summary>
    /// Hook 注册表。
    /// </summary>
    public IHookRegistry Hooks { get; }

    /// <summary>
    /// Mode 上下文快照。
    /// </summary>
    public ModeContext? ModeContext { get; }

    internal CancellationTokenSource CancellationSource { get; }
    public bool IsCancellationRequested => CancellationSource.IsCancellationRequested;
    public CancellationToken Token => CancellationSource.Token;

    internal ExecutionContext(
        ExecutionId id, 
        Guid sessionId, 
        IHookRegistry hooks, 
        ModeContext? modeContext,
        string? taskId = null,
        string? agentId = null)
    {
        Id = id;
        SessionId = sessionId;
        Hooks = hooks ?? new ExecutionHookRegistry();
        ModeContext = modeContext;
        TaskId = taskId;
        AgentId = agentId;
        CancellationSource = new CancellationTokenSource();
    }

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
    public static ExecutionContext CreateWithMode(Guid sessionId, ModeContext modeContext, IHookRegistry? hooks = null)
    {
        ArgumentNullException.ThrowIfNull(modeContext);
        var id = ExecutionId.New();
        return new ExecutionContext(id, sessionId, hooks ?? new ExecutionHookRegistry(), modeContext);
    }

    /// <summary>
    /// Creates an ExecutionContext with ExecutionPolicy.
    /// </summary>
    public static ExecutionContext CreateWithPolicy(Guid sessionId, ExecutionPolicy policy, IHookRegistry? hooks = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var id = ExecutionId.New();
        var modeContext = new ModeContext(
            (int)JNPF.Runtime.Capability.Modes.ModeType.Audit,
            "Audit",
            policy);
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

    public void Cancel() => CancellationSource.Cancel();

    public void Dispose()
    {
        CancellationSource.Dispose();
        if (Hooks is IDisposable disposable) disposable.Dispose();
    }
}
