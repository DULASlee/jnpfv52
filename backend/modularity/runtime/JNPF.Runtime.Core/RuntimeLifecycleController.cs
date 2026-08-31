using JNPF.Runtime.Capability.Loading;
using JNPF.Runtime.Core.Events;

namespace JNPF.Runtime.Core;

/// <summary>
/// Section 8 Runtime 生命周期控制器默认实现。
///
/// 约束：
///   - 线程安全；
///   - 不包含 Intelligence/Workflow 概念；
///   - 会话以 SessionId 为键存储在内存中。
/// </summary>
public sealed class RuntimeLifecycleController : IRuntimeLifecycleController
{
    private readonly Dictionary<Guid, RuntimeSession> _sessions = new();
    private readonly IPolicyProvider _policyProvider;
    private readonly object _lock = new();

    /// <inheritdoc />
    public RuntimeSession? CurrentSession { get; private set; }

    /// <summary>
    /// Creates a new RuntimeLifecycleController with a Policy provider.
    /// </summary>
    /// <param name="policyProvider">Policy provider (required for Mode integration).</param>
    public RuntimeLifecycleController(IPolicyProvider policyProvider)
    {
        _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
    }

    /// <summary>
    /// Creates a new RuntimeLifecycleController with default Mode provider.
    /// For testing and backward compatibility.
    /// </summary>
    public RuntimeLifecycleController()
        : this(new JNPF.Runtime.Capability.Loading.DefaultModeProvider())
    {
    }

    /// <inheritdoc />
    public Task<RuntimeSession> InitializeAsync(RuntimeContext context, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (CurrentSession != null)
                throw new InvalidOperationException("A session already exists. Dispose it before creating a new one.");

            var session = new RuntimeSession(context);
            _sessions[session.SessionId] = session;
            CurrentSession = session;

            RuntimeStateMachine.Transition(session, RuntimeState.Initialized, "InitializeAsync");
            return Task.FromResult(session);
        }
    }

    /// <inheritdoc />
    public Task StartAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = GetSession(sessionId);
        RuntimeStateMachine.Transition(session, RuntimeState.Running, "StartAsync");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PauseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = GetSession(sessionId);
        RuntimeStateMachine.Transition(session, RuntimeState.Paused, "PauseAsync");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResumeAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = GetSession(sessionId);
        RuntimeStateMachine.Transition(session, RuntimeState.Running, "ResumeAsync");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CompleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = GetSession(sessionId);
        RuntimeStateMachine.Transition(session, RuntimeState.Completed, "CompleteAsync");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task FailAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var session = GetSession(sessionId);
        RuntimeStateMachine.Transition(session, RuntimeState.Failed, reason);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        RuntimeSession? session;
        lock (_lock)
        {
            if (!_sessions.Remove(sessionId, out session))
                throw new InvalidOperationException($"Session '{sessionId}' not found.");

            if (CurrentSession?.SessionId == sessionId)
                CurrentSession = null;
        }

        RuntimeStateMachine.Transition(session, RuntimeState.Disposed, "DisposeAsync");
        return Task.CompletedTask;
    }

    private RuntimeSession GetSession(Guid sessionId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                throw new InvalidOperationException($"Session '{sessionId}' not found.");
            return session;
        }
    }

    // === Execution Boundary Extension ===

    /// <inheritdoc />
    public ExecutionContext CreateExecution(Guid sessionId)
    {
        var session = GetSession(sessionId);
        return ExecutionContextFactory.Create(session.SessionId);
    }

    /// <inheritdoc />
    public ExecutionContext CreateExecution(Guid sessionId, IHookRegistry hookRegistry)
    {
        ArgumentNullException.ThrowIfNull(hookRegistry);
        var session = GetSession(sessionId);
        return ExecutionContextFactory.Create(session.SessionId, hookRegistry);
    }

    // === Mode Integration Extension ===

    /// <inheritdoc />
    public ExecutionContext CreateExecution(Guid sessionId, int modeTypeId, AuthorizationToken? auth = null)
    {
        var session = GetSession(sessionId);
        
        // Resolve Mode type
        var modeType = (JNPF.Runtime.Capability.Modes.ModeType)modeTypeId;
        
        // Get Policy from provider
        var policyData = _policyProvider.ResolvePolicy(modeType, auth?.Value);
        
        // Create ModeContext
        var modeContext = ModeContext.FromPolicyData(policyData, auth);
        
        // Associate with session
        session.ModeContext = modeContext;
        
        // Create ExecutionContext
        return ExecutionContextFactory.Create(session.SessionId, modeContext);
    }

    /// <inheritdoc />
    public ExecutionContext CreateExecution(Guid sessionId, ExecutionPolicy policy, IHookRegistry? hooks = null)
    {
        var session = GetSession(sessionId);
        var execution = ExecutionContextFactory.Create(session.SessionId, policy, hooks);
        return execution;
    }

    /// <inheritdoc />
    public ModeContext? GetCurrentModeContext(Guid sessionId)
    {
        var session = GetSession(sessionId);
        return session.ModeContext;
    }

    /// <inheritdoc />
    public async Task<ExecutionResult> ExecuteAsync(
        ExecutionContext execution,
        Func<ExecutionContext, Task> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(work);

        var startTime = DateTime.UtcNow;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            execution.Token, cancellationToken);

        // === Mode Integration: Admission Check ===
        if (execution.ModeContext != null)
        {
            var authResult = execution.ModeContext.Policy.Authorize();
            if (!authResult.IsAuthorized)
            {
                // Rejected at admission - return immediately without executing
                return ExecutionResult.Rejected(execution.Id, authResult.Reason!);
            }
        }

        // Publish ExecutionStartedEvent
        PublishEvent(ExecutionStartedEvent.Create(execution.Id, execution.SessionId));

        try
        {
            // Execute Before Hooks
            await InvokeHooksAsync(
                execution,
                ExecutionHookType.Before,
                async hook =>
                    await hook.OnBeforeExecutionAsync(execution, linkedCts.Token),
                linkedCts.Token);

            if (linkedCts.Token.IsCancellationRequested)
            {
                return await HandleCancellationAsync(execution, startTime);
            }

            // Execute Work
            await work(execution);

            // Execute After Hooks
            var result = ExecutionResult.Success(execution.Id, DateTime.UtcNow - startTime);
            await InvokeHooksAsync(
                execution,
                ExecutionHookType.After,
                async hook =>
                    await hook.OnAfterExecutionAsync(execution, result, linkedCts.Token),
                linkedCts.Token);

            // Publish ExecutionCompletedEvent
            PublishEvent(ExecutionCompletedEvent.Create(execution.Id, result));

            return result;
        }
        catch (OperationCanceledException) when (execution.IsCancellationRequested)
        {
            return await HandleCancellationAsync(execution, startTime);
        }
        catch (OperationCanceledException)
        {
            return await HandleCancellationAsync(execution, startTime);
        }
        catch (Exception ex)
        {
            return await HandleFailureAsync(execution, ex, startTime);
        }
    }

    private async Task<ExecutionResult> HandleCancellationAsync(ExecutionContext execution, DateTime startTime)
    {
        var result = ExecutionResult.Cancelled(execution.Id, DateTime.UtcNow - startTime);

        await InvokeHooksAsync(
            execution,
            ExecutionHookType.OnCancelled,
            async hook =>
                await hook.OnExecutionCancelledAsync(execution, CancellationToken.None),
            CancellationToken.None);

        PublishEvent(ExecutionCancelledEvent.Create(execution.Id));

        return result;
    }

    private async Task<ExecutionResult> HandleFailureAsync(ExecutionContext execution, Exception ex, DateTime startTime)
    {
        var result = ExecutionResult.Failure(execution.Id, ex.Message, ex, DateTime.UtcNow - startTime);

        await InvokeHooksAsync(
            execution,
            ExecutionHookType.OnFailure,
            async hook =>
                await hook.OnExecutionFailedAsync(execution, ex, CancellationToken.None),
            CancellationToken.None);

        PublishEvent(ExecutionFailedEvent.Create(execution.Id, ex));

        return result;
    }

    private async Task InvokeHooksAsync(
        ExecutionContext execution,
        ExecutionHookType hookType,
        Func<IExecutionHook, Task> invoke,
        CancellationToken cancellationToken)
    {
        var hooks = execution.Hooks.GetHooks(hookType);
        foreach (var hook in hooks)
        {
            if (cancellationToken.IsCancellationRequested && hookType != ExecutionHookType.OnCancelled)
                break;

            try
            {
                await invoke(hook).ConfigureAwait(false);
            }
            catch
            {
                // Hook failures are swallowed by default
            }
        }
    }

    private void PublishEvent(IRuntimeEvent evt)
    {
        // Default implementation: no-op for v0.2
        // Runtime may inject IRuntimeEventPublisher in future
    }
}
