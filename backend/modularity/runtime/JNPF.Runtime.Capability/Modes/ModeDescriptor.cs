using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Constraints;

namespace JNPF.Runtime.Capability.Modes;

/// <summary>
/// Mode 注册表使用的不可变元数据记录。
///
/// 与 <see cref="IMode"/> 的区别：
///   - <see cref="IMode"/> 是运行时句柄；
///   - <see cref="ModeDescriptor"/> 是只读元数据（Name / Description / Capability Whitelist / Constraints）。
///
/// 注册表仅暴露 Descriptor；Provider 负责根据 Type 解析实例。
/// </summary>
public sealed record ModeDescriptor(
    ModeType Type,
    string Name,
    string Description,
    ModeCapabilitySet Capabilities,
    ConstraintSet Constraints);
