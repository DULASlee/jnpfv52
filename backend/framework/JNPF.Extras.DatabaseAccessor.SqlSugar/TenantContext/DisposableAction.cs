namespace JNPF.Extras.DatabaseAccessor.SqlSugar.TenantContext;

/// <summary>
/// IDisposable 辅助类 — 在 Dispose 时执行传入的 Action.
/// 用于 TenantContext.BeginScope 的返回值.
/// </summary>
internal sealed class DisposableAction : IDisposable
{
    private Action? _action;

    public DisposableAction(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Dispose()
    {
        var action = Interlocked.Exchange(ref _action, null);
        action?.Invoke();
    }
}
