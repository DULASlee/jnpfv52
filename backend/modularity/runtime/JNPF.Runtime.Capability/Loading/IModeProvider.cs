using JNPF.Runtime.Capability.Modes;

namespace JNPF.Runtime.Capability.Loading;

/// <summary>
/// Section 9 Mode Provider Contract (M17)。
///
/// 职责：
///   - 根据 <see cref="ModeType"/> 解析出独立的 <see cref="IMode"/> 实例；
///   - 不缓存、不共享单例（§3.4 + Gate-9-5 + Iron Law-05 Lifetime Guard）；
///   - 不持有 Runtime 引用，不引入 Session 状态。
///
/// 调用方必须在使用后释放实例（实现 IDisposable 的 Mode）或确保实例无状态。
/// 本接口当前实现均不可变，调用方可按 Transient 处理。
/// </summary>
public interface IModeProvider
{
    /// <summary>
    /// 解析出独立的 IMode 实例（每次调用返回新实例）。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">未知 ModeType 时抛出。</exception>
    IMode Resolve(ModeType modeType);
}
