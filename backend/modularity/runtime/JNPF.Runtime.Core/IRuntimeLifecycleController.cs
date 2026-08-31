namespace JNPF.Runtime.Core;

/// <summary>
/// Section 8 Runtime 生命周期控制器接口 (Layer 0 Kernel Contract)。
///
/// 约束：
///   - 不包含 Intelligence/Workflow/Prompt/Plan 概念；
///   - 不持有业务数据；
///   - 异常传播使用 InvalidOperationException。
/// </summary>
public interface IRuntimeLifecycleController
{
    /// <summary>
    /// 获取当前会话。
    /// </summary>
    RuntimeSession? CurrentSession { get; }

    /// <summary>
    /// 创建并初始化会话。
    /// </summary>
    /// <param name="context">运行时上下文（三元组）。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>新创建的会话。</returns>
    /// <exception cref="InvalidOperationException">会话已存在。</exception>
    Task<RuntimeSession> InitializeAsync(RuntimeContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动会话。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <exception cref="InvalidOperationException">会话不存在或状态不允许启动。</exception>
    Task StartAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 暂停会话。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消标记。</param>
    Task PauseAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复会话。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消标记。</param>
    Task ResumeAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记会话正常完成。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消标记。</param>
    Task CompleteAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记会话异常终止。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="reason">失败原因。</param>
    /// <param name="cancellationToken">取消标记。</param>
    Task FailAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// 释放会话资源。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消标记。</param>
    Task DisposeAsync(Guid sessionId, CancellationToken cancellationToken = default);

    // === Execution Boundary Extension ===

    /// <summary>
    /// 创建 ExecutionContext（无 Mode）。
    /// </summary>
    ExecutionContext CreateExecution(Guid sessionId);

    /// <summary>
    /// 创建带有自定义 HookRegistry 的 ExecutionContext（无 Mode）。
    /// </summary>
    ExecutionContext CreateExecution(Guid sessionId, IHookRegistry hookRegistry);

    /// <summary>
    /// 执行工作单元，自动管理 Hook 和 Event。
    /// </summary>
    Task<ExecutionResult> ExecuteAsync(
        ExecutionContext execution,
        Func<ExecutionContext, Task> work,
        CancellationToken cancellationToken = default);

    // === Mode Integration Extension ===

    /// <summary>
    /// 使用指定 Mode 类型 ID 创建 ExecutionContext。
    /// 自动解析 Mode 并创建 ExecutionPolicy。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="modeTypeId">Mode 类型 ID (0=Audit, 1=Verify, 2=Execute, 3=Assist)。</param>
    /// <param name="auth">可选的授权令牌。</param>
    /// <returns>带有 Mode 上下文的 ExecutionContext。</returns>
    ExecutionContext CreateExecution(Guid sessionId, int modeTypeId, AuthorizationToken? auth = null);

    /// <summary>
    /// 使用指定 ExecutionPolicy 创建 ExecutionContext。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="policy">执行策略。</param>
    /// <param name="hooks">可选的 Hook 注册表。</param>
    /// <returns>带有策略的 ExecutionContext。</returns>
    ExecutionContext CreateExecution(Guid sessionId, ExecutionPolicy policy, IHookRegistry? hooks = null);

    /// <summary>
    /// 获取会话的当前 Mode 上下文。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <returns>Mode 上下文（如果已设置）。</returns>
    ModeContext? GetCurrentModeContext(Guid sessionId);
}
