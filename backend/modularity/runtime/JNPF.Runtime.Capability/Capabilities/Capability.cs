namespace JNPF.Runtime.Capability;

// Capability 枚举放置在根命名空间，避免与子命名空间 Capabilities 同名引起的编译歧义。

/// <summary>
/// Agent OS Layer 1 (Section 9) Capability 严格递增枚举。
///
/// 约束：
///   - 序号即为偏序关系：值越大 = 越强能力。
///   - 不允许引入 "Reasoning / Prompt / Plan / Think / Workflow / Step / DAG" 等 Intelligence / Workflow 概念 (M14 + LOCK-H02)。
///   - <see cref="ApplyUnapprovedChange"/> 仅供 Governance 路径使用，任何 IMode 不得声明 (M11 + Constraint-14)。
/// </summary>
public enum Capability
{
    Observe = 1,
    Evaluate = 2,
    Reflect = 3,
    ReadEvidence = 4,
    Build = 5,
    Test = 6,
    WriteEvidence = 7,
    ApplyApprovedPatch = 8,
    ModifyState = 9,

    /// <summary>
    /// 未审批变更 — 任何 IMode 必须显式不包含；仅 Governance 路径（Gate/Interceptor）允许。
    /// </summary>
    ApplyUnapprovedChange = 10
}
