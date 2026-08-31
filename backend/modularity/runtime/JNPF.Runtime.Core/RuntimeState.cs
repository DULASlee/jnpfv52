namespace JNPF.Runtime.Core;

/// <summary>
/// Section 8 Runtime 生命周期状态枚举。
///
/// 约束：
///   - 严格状态机：只能按合法转换路径流转；
///   - 不允许跳状态（除 Disposed 可从任意非 Disposed 直接跳转）；
///   - 序号仅用于诊断，不表示偏序关系。
/// </summary>
public enum RuntimeState
{
    /// <summary>
    /// 已创建但未初始化。
    /// </summary>
    Created = 0,

    /// <summary>
    /// 初始化完成，等待启动。
    /// </summary>
    Initialized = 1,

    /// <summary>
    /// 运行中。
    /// </summary>
    Running = 2,

    /// <summary>
    /// 暂停（可恢复）。
    /// </summary>
    Paused = 3,

    /// <summary>
    /// 正常完成。
    /// </summary>
    Completed = 4,

    /// <summary>
    /// 异常终止。
    /// </summary>
    Failed = 5,

    /// <summary>
    /// 已释放资源，不可恢复。
    /// </summary>
    Disposed = 6
}
