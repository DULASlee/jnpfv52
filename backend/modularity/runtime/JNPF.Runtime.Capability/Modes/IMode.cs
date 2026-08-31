using JNPF.Runtime.Capability.Capabilities;
using JNPF.Runtime.Capability.Constraints;

namespace JNPF.Runtime.Capability.Modes;

/// <summary>
/// Section 9 Mode 纯 Contract 接口 (M16 Purity Boundary)。
///
/// 铁律：
///   - 不含任何 Runtime 引用；
///   - 不含 Think / Prompt / Plan / Step / DAG 等 Intelligence / Workflow 概念 (M14 + M16)；
///   - 不修改 Runtime 行为 (M8)；
///   - 不持有可变的运行时状态。
/// </summary>
public interface IMode
{
    /// <summary>
    /// Mode 类型标识（用于 Registry 检索与 Profile 注入）。
    /// </summary>
    ModeType Type { get; }

    /// <summary>
    /// 该 Mode 暴露的 Capability Whitelist (M5)。
    /// </summary>
    ModeCapabilitySet Capabilities { get; }

    /// <summary>
    /// 静态约束集合（如禁止声明某些 Capability、需显式授权）。
    /// </summary>
    ConstraintSet Constraints { get; }

    /// <summary>
    /// 人类可读名称（用于 Registry 与诊断）。允许本地化字符串，不引入 LLM 依赖。
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Mode 用途描述（非 Prompt）。
    /// </summary>
    string Description { get; }
}
