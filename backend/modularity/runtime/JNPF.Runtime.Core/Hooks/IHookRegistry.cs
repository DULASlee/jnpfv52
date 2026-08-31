namespace JNPF.Runtime.Core;

/// <summary>
/// Hook 注册表接口。
/// 
/// 约束：
///   - 线程安全
///   - 按 HookType + Order 排序
/// </summary>
public interface IHookRegistry
{
    /// <summary>
    /// 注册 Hook。
    /// </summary>
    void Register(IExecutionHook hook);

    /// <summary>
    /// 注销 Hook。
    /// </summary>
    void Unregister(IExecutionHook hook);

    /// <summary>
    /// 获取指定类型的 Hook（已排序）。
    /// </summary>
    IReadOnlyList<IExecutionHook> GetHooks(ExecutionHookType type);

    /// <summary>
    /// 获取所有 Hook。
    /// </summary>
    IReadOnlyList<IExecutionHook> GetAllHooks();

    /// <summary>
    /// 是否为空。
    /// </summary>
    bool IsEmpty { get; }
}
