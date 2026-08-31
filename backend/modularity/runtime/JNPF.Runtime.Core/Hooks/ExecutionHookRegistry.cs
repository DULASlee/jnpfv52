using System.Collections.Concurrent;

namespace JNPF.Runtime.Core;

/// <summary>
/// Hook 注册表默认实现。
/// 
/// 线程安全：ConcurrentDictionary + ReaderWriterLockSlim
/// </summary>
public sealed class ExecutionHookRegistry : IHookRegistry, IDisposable
{
    private readonly ConcurrentDictionary<IExecutionHook, byte> _hooks = new();
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private bool _disposed;

    /// <inheritdoc />
    public void Register(IExecutionHook hook)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(hook);

        _hooks.TryAdd(hook, 0);
    }

    /// <inheritdoc />
    public void Unregister(IExecutionHook hook)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(hook);

        _hooks.TryRemove(hook, out _);
    }

    /// <inheritdoc />
    public IReadOnlyList<IExecutionHook> GetHooks(ExecutionHookType type)
    {
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            return _hooks.Keys
                .Where(h => h.HookType == type)
                .OrderBy(h => h.Order)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IExecutionHook> GetAllHooks()
    {
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            return _hooks.Keys
                .OrderBy(h => h.HookType)
                .ThenBy(h => h.Order)
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public bool IsEmpty => _hooks.IsEmpty;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ExecutionHookRegistry));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _hooks.Clear();
        _lock.Dispose();
    }
}
