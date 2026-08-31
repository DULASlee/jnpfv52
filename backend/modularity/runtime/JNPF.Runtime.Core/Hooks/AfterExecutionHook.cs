namespace JNPF.Runtime.Core;

/// <summary>
/// After Hook 基类。
/// </summary>
public abstract class AfterExecutionHook : IExecutionHook
{
    /// <inheritdoc />
    public abstract int Order { get; }

    /// <inheritdoc />
    public ExecutionHookType HookType => ExecutionHookType.After;

    /// <inheritdoc />
    public virtual Task OnBeforeExecutionAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnAfterExecutionAsync(
        ExecutionContext context,
        ExecutionResult result,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnExecutionFailedAsync(
        ExecutionContext context,
        Exception exception,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnExecutionCancelledAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
