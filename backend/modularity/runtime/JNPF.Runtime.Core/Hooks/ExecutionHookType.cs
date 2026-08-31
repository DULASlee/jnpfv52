namespace JNPF.Runtime.Core;

/// <summary>
/// Hook 执行时机。
/// </summary>
public enum ExecutionHookType
{
    /// <summary>
    /// 执行前。
    /// </summary>
    Before = 0,

    /// <summary>
    /// 执行后（无论成功或失败）。
    /// </summary>
    After = 1,

    /// <summary>
    /// 执行失败时。
    /// </summary>
    OnFailure = 2,

    /// <summary>
    /// 执行取消时。
    /// </summary>
    OnCancelled = 3
}
