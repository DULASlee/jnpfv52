namespace JNPF.Runtime.Core;

/// <summary>
/// Execution Hook 接口。
/// 
/// 约束：
///   - Hook 不得修改 RuntimeState
///   - Hook 失败不阻止 Execution（除非配置 fail-fast）
///   - Hook 按 Order 升序执行
/// </summary>
public interface IExecutionHook
{
    /// <summary>
    /// Hook 类型。
    /// </summary>
    ExecutionHookType HookType { get; }

    /// <summary>
    /// 执行顺序（升序，越小越先执行）。
    /// </summary>
    int Order { get; }

    /// <summary>
    /// 执行前回调。
    /// </summary>
    Task OnBeforeExecutionAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行后回调。
    /// </summary>
    Task OnAfterExecutionAsync(
        ExecutionContext context,
        ExecutionResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行失败回调。
    /// </summary>
    Task OnExecutionFailedAsync(
        ExecutionContext context,
        Exception exception,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行取消回调。
    /// </summary>
    Task OnExecutionCancelledAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default);
}
