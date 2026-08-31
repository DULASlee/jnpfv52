using JNPF.Runtime.Capability.Modes;

namespace JNPF.Runtime.Capability.Registry;

/// <summary>
/// Section 9 Mode 注册表 Contract (M12)。
///
/// 职责（仅元数据，不负责 Resolve）：
///   - 注册 4 种 Default Mode 的元数据（Type / Name / Description / Capability Whitelist / Constraints）；
///   - 提供基于 <see cref="ModeType"/> 的只读查询；
///   - 不负责 Runtime lifecycle / State transition / Agent execution。
///
/// 与 <see cref="Loading.IModeProvider"/> 的关系：
///   - Registry = 元数据（只读、不可变、可序列化用于 Profile 注入）；
///   - Provider = 实例工厂（每次返回新实例）。
/// </summary>
public interface IModeRegistry
{
    /// <summary>
    /// 是否注册了指定 ModeType。
    /// </summary>
    bool Contains(ModeType modeType);

    /// <summary>
    /// 获取指定 ModeType 的元数据描述符。
    /// </summary>
    /// <exception cref="KeyNotFoundException">未知 ModeType 时抛出。</exception>
    ModeDescriptor GetDescriptor(ModeType modeType);

    /// <summary>
    /// 全部已注册的 Mode 元数据。
    /// </summary>
    IReadOnlyCollection<ModeDescriptor> All { get; }
}
