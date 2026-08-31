namespace JNPF.Runtime.Capability.Modes;

/// <summary>
/// Section 9 (Layer 1) 内置 4 种 Mode 类型 (M1)。
///
/// 约束：
///   - Audit 默认开启 (M9)；
///   - Execute 需显式授权 (M10)；
///   - 排序值即严格递增的 Capability 强度序。
/// </summary>
public enum ModeType
{
    /// <summary>
    /// 最小特权 Mode：仅 Observe / Evaluate / Reflect / ReadEvidence。默认开启 (M9)。
    /// </summary>
    Audit = 1,

    /// <summary>
    /// 在 Audit 基础上增加 Build / Test。Capability 严格递增 (M11)。
    /// </summary>
    Verify = 2,

    /// <summary>
    /// 在 Verify 基础上增加 WriteEvidence / ApplyApprovedPatch / ModifyState。需显式授权 (M10)。
    /// </summary>
    Execute = 3,

    /// <summary>
    /// 由 Profile 注入额外 Capability 集合 (M2)。本阶段 Capability 等同 Execute，具体 Profile 扩展为 Section 10 职责。
    /// </summary>
    Assist = 4
}
